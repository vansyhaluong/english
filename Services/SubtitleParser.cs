using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using English.Models;

namespace English.Services;

// Strict, basic SRT/WebVTT. This service reads only memory/streams; it never writes files.
public sealed partial class SubtitleParser
{
    public async Task<SubtitleParseResult> ParseAsync(Stream content, string fileName, long length,
        CancellationToken cancellationToken = default)
    {
        SubtitleParseResult Fail(string resource, int line = 0) => new([], new(resource, line));
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(['/', '\\']) >= 0)
            return Fail("ListeningSubtitleExtension");
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".srt" or ".vtt")) return Fail("ListeningSubtitleExtension");
        if (length < 0 || length > ListeningSubtitleLimits.MaxSizeBytes) return Fail("ListeningSubtitleSize");
        using var memory = new MemoryStream();
        var buffer = new byte[8192];
        try
        {
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) != 0)
            {
                if (memory.Length + read > ListeningSubtitleLimits.MaxSizeBytes) return Fail("ListeningSubtitleSize");
                await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            return Fail("ListeningSubtitleReadFailed");
        }
        if (memory.Length == 0) return Fail("ListeningSubtitleEmpty");
        string text;
        try { text = new UTF8Encoding(false, true).GetString(memory.GetBuffer(), 0, (int)memory.Length); }
        catch (DecoderFallbackException) { return Fail("ListeningSubtitleEncoding"); }
        if (text[0] == '\uFEFF') text = text[1..];
        if (text.Any(c => c == '\uFEFF' || (char.IsControl(c) && c is not ('\r' or '\n' or '\t'))))
            return Fail("ListeningSubtitleEncoding");
        // CRLF, CR and LF express the same line boundaries. Cue words/spaces are preserved.
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var vtt = extension == ".vtt";
        var index = 0;
        if (vtt)
        {
            if (lines[0] != "WEBVTT") return Fail("ListeningSubtitleVttHeader", 1);
            if (lines.Length < 2 || lines[1].Length != 0) return Fail("ListeningSubtitleStructure", 2);
            index = 2;
        }
        var cues = new List<SubtitleCue>();
        while (index < lines.Length)
        {
            if (lines[index].Length == 0) { index++; continue; }
            var firstLine = index + 1;
            if (!vtt)
            {
                if (!int.TryParse(lines[index], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
                    || sequence != cues.Count + 1) return Fail("ListeningSubtitleSequence", firstLine);
                index++;
            }
            else
            {
                if (UnsupportedBlock(lines[index])) return Fail("ListeningSubtitleVttUnsupported", firstLine);
                // A basic identifier is optional. It is not cue text or a database Sequence.
                if (!lines[index].Contains("-->"))
                {
                    if (lines[index].Contains('<') || lines[index].Contains('>')) return Fail("ListeningSubtitleVttUnsupported", firstLine);
                    index++;
                }
            }
            if (index >= lines.Length) return Fail("ListeningSubtitleStructure", firstLine);
            var timingLine = index + 1;
            var timing = (vtt ? VttTiming() : SrtTiming()).Match(lines[index]);
            if (!timing.Success)
            {
                if (vtt && VttSettings().IsMatch(lines[index])) return Fail("ListeningSubtitleVttUnsupported", timingLine);
                return Fail("ListeningSubtitleTimestamp", timingLine);
            }
            if (!TryMilliseconds(timing.Groups["start"].Value, vtt, out var start)
                || !TryMilliseconds(timing.Groups["end"].Value, vtt, out var end) || end <= start)
                return Fail("ListeningSubtitleTimestamp", timingLine);
            if (cues.Count > 0 && start < cues[^1].EndMs) return Fail("ListeningSubtitleOverlap", timingLine);
            index++;
            var textStart = index;
            while (index < lines.Length && lines[index].Length != 0) index++;
            var cueText = string.Join('\n', lines[textStart..index]);
            if (string.IsNullOrWhiteSpace(cueText)) return Fail("ListeningSubtitleCueEmpty", timingLine + 1);
            if (vtt && (cueText.Contains('<') || cueText.Contains('>')))
                return Fail("ListeningSubtitleVttUnsupported", textStart + 1);
            // A missing block separator must not silently swallow another cue's timing line.
            if (lines[textStart..index].Any(line => line.Contains("-->"))) return Fail("ListeningSubtitleStructure", textStart + 1);
            cues.Add(new(cues.Count + 1, start, end, cueText));
        }
        return cues.Count == 0 ? Fail("ListeningSubtitleEmpty") : new(cues);
    }

    private static bool UnsupportedBlock(string line) => line is "NOTE" or "STYLE" or "REGION"
        || line.StartsWith("NOTE ", StringComparison.Ordinal) || line.StartsWith("NOTE\t", StringComparison.Ordinal);

    private static bool TryMilliseconds(string value, bool vtt, out int milliseconds)
    {
        milliseconds = 0;
        var parts = value.Replace(vtt ? '.' : ',', ':').Split(':');
        var hasHours = parts.Length == 4;
        if (!hasHours && (!vtt || parts.Length != 3)) return false;
        var offset = hasHours ? 1 : 0;
        if (!long.TryParse(hasHours ? parts[0] : "0", NumberStyles.None, CultureInfo.InvariantCulture, out var hours)) return false;
        var minutes = int.Parse(parts[offset], CultureInfo.InvariantCulture);
        var seconds = int.Parse(parts[offset + 1], CultureInfo.InvariantCulture);
        var fraction = int.Parse(parts[offset + 2], CultureInfo.InvariantCulture);
        // Check before arithmetic so hostile hour fields cannot overflow.
        if (hours > int.MaxValue / 3_600_000L) return false;
        var result = hours * 3_600_000L + minutes * 60_000L + seconds * 1000L + fraction;
        if (result > int.MaxValue) return false;
        milliseconds = (int)result;
        return true;
    }

    [GeneratedRegex(@"^[ \t]*(?<start>[0-9]{2,}:[0-5][0-9]:[0-5][0-9],[0-9]{3})[ \t]+-->[ \t]+(?<end>[0-9]{2,}:[0-5][0-9]:[0-5][0-9],[0-9]{3})[ \t]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SrtTiming();
    [GeneratedRegex(@"^[ \t]*(?<start>(?:[0-9]{2,}:)?[0-5][0-9]:[0-5][0-9]\.[0-9]{3})[ \t]+-->[ \t]+(?<end>(?:[0-9]{2,}:)?[0-5][0-9]:[0-5][0-9]\.[0-9]{3})[ \t]*$", RegexOptions.CultureInvariant)]
    private static partial Regex VttTiming();
    [GeneratedRegex(@"^[ \t]*(?:[0-9]{2,}:)?[0-5][0-9]:[0-5][0-9]\.[0-9]{3}[ \t]+-->[ \t]+(?:[0-9]{2,}:)?[0-5][0-9]:[0-5][0-9]\.[0-9]{3}[ \t]+\S", RegexOptions.CultureInvariant)]
    private static partial Regex VttSettings();
}
