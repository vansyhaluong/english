using System.Text.RegularExpressions;
using English.Models;

namespace English.Services;

public sealed partial class ListeningAudioStorage(IWebHostEnvironment environment)
{
    public const string UrlPrefix = "/listening/audio/";
    private string Root => Path.Combine(environment.ContentRootPath, "Data", "Listening");
    public static bool IsKey(string key) => KeyPattern().IsMatch(key);
    public static bool IsInternalUrl(string? url) => url is not null && url.StartsWith(UrlPrefix, StringComparison.Ordinal)
        && IsKey(url[UrlPrefix.Length..]);

    private string Resolve(string key)
    {
        if (!IsKey(key)) throw new InvalidDataException("Invalid audio key.");
        var relative = key.StartsWith("voa-", StringComparison.Ordinal)
            ? Path.Combine("lesson-" + key[4..], "audio.mp3") : Path.Combine("uploads", key + ".mp3");
        var path = Path.GetFullPath(Path.Combine(Root, relative));
        // Reject symlinks/junctions at every existing path component under ContentRoot.
        CheckPath(path);
        return path;
    }

    public string SourceFile(int lesson, string name)
    {
        if (lesson is < 1 or > 5 || name is not ("audio.mp3" or "subtitle.srt" or "transcript.txt" or "source.json"))
            throw new InvalidDataException("Invalid source.");
        var path = Path.Combine(Root, "lesson-" + lesson, name);
        CheckPath(path);
        return path;
    }

    private void CheckPath(string path)
    {
        var contentRoot = Path.GetFullPath(environment.ContentRootPath).TrimEnd(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(contentRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Invalid storage path.");
        var current = contentRoot;
        foreach (var part in Path.GetRelativePath(contentRoot, fullPath).Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Linked storage paths are not supported.");
        }
    }

    public async Task<string> WriteNewAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        var key = Guid.NewGuid().ToString("N");
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        try { await stream.WriteAsync(bytes, cancellationToken); }
        catch
        {
            await stream.DisposeAsync();
            File.Delete(path); // Only this newly created file, never an existing/source file.
            throw;
        }
        return UrlPrefix + key;
    }

    public void DeleteNew(string url)
    {
        if (!IsInternalUrl(url)) throw new InvalidDataException("Invalid new audio URL.");
        var key = url[UrlPrefix.Length..];
        if (key.StartsWith("voa-", StringComparison.Ordinal)) throw new InvalidDataException("Source deletion is forbidden.");
        File.Delete(Resolve(key));
    }

    public async Task<FileStream?> OpenAsync(string key, CancellationToken cancellationToken)
    {
        if (!IsKey(key)) return null;
        var path = Resolve(key);
        if (!File.Exists(path)) return null;
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        try
        {
            if (stream.Length > ListeningAudioLimits.MaxBytes || await ListeningMp3Validator.ReadAsync(stream, cancellationToken) is null)
            { await stream.DisposeAsync(); return null; }
            stream.Position = 0;
            return stream;
        }
        catch { await stream.DisposeAsync(); throw; }
    }

    [GeneratedRegex(@"\A(?:voa-[1-5]|[a-f0-9]{32})\z", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}
