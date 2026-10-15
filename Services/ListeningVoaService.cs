using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using English.Data;
using English.Models;
using English.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

public sealed class ListeningVoaService(ApplicationDbContext context, ListeningDatabaseGuard databaseGuard,
    ListeningAudioStorage storage, SubtitleParser parser, IWebHostEnvironment environment)
{
    private static readonly string[] Titles = ["Welcome!", "Hello, I'm Anna!", "I'm Here!", "What Is It?", "Where Are You?"];
    private sealed record SourceLesson(int Number, string Title, string AudioUrl, string ExternalAudioUrl,
        string PageUrl, IReadOnlyList<SubtitleCue> Cues);

    public async Task<ListeningVoaReport> PreviewAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return new(false, "ListeningVoaDevelopmentOnly", []);
        var check = await databaseGuard.CheckAsync(cancellationToken);
        if (!check.IsSafe) return new(false, check.Message, []);
        try
        {
            var lessons = await ReadSourcesAsync(cancellationToken);
            var classification = await ClassificationAsync(cancellationToken);
            if (classification is null) return new(false, "ListeningVoaClassification", Summary(lessons));
            if (await HasDuplicatesAsync(lessons, cancellationToken)) return new(false, "ListeningVoaDuplicate", Summary(lessons));
            return new(true, "ListeningVoaReady", Summary(lessons));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException
            or DecoderFallbackException or KeyNotFoundException or InvalidOperationException or System.Data.Common.DbException)
        {
            return new(false, "ListeningVoaCheckFailed", []);
        }
    }

    public async Task<ListeningVoaReport> ImportAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return new(false, "ListeningVoaDevelopmentOnly", []);
        var check = await databaseGuard.CheckAsync(cancellationToken);
        if (!check.IsSafe) return new(false, check.Message, []);
        var commitAttempted = false;
        try
        {
            // POST repeats all checks; a successful preview is never used as authorization to skip validation.
            var lessons = await ReadSourcesAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            check = await databaseGuard.CheckAsync(cancellationToken);
            if (!check.IsSafe) return new(false, check.Message, []);
            var classification = await ClassificationAsync(cancellationToken);
            if (classification is null) return new(false, "ListeningVoaClassification", Summary(lessons));
            // Serializable holds the matching title/URL key ranges until commit, including for concurrent imports.
            if (await HasDuplicatesAsync(lessons, cancellationToken)) return new(false, "ListeningVoaDuplicate", Summary(lessons));
            var now = DateTimeOffset.UtcNow;
            var entities = lessons.Select(source => new ListeningLesson
            {
                AudioUrl = source.AudioUrl, SubtitleFileId = null, TranscriptRevision = 1,
                LearningItem = new LearningItem
                {
                    Kind = 3, Title = source.Title, LevelId = classification.Value.LevelId,
                    TopicId = classification.Value.TopicId, IsVisible = true, IsDeleted = false,
                    CreatedAt = now, UpdatedAt = now,
                    Description = "Source: VOA Learning English — " + source.PageUrl
                        + "\nACCEPTED_WITH_LIMITATIONS: full human listening and subtitle synchronization verification not performed."
                },
                TranscriptCues = source.Cues.Select(cue => new TranscriptCue
                {
                    Sequence = cue.Sequence, StartMs = cue.StartMs, EndMs = cue.EndMs, Text = cue.Text
                }).ToList()
            }).ToArray();
            // Initial transcripts come from the existing parser. One SaveChanges + one transaction for all five graphs.
            context.ListeningLessons.AddRange(entities);
            await context.SaveChangesAsync(cancellationToken);
            commitAttempted = true;
            await transaction.CommitAsync(cancellationToken);
            return new(false, "ListeningVoaImported", entities.Select((x, i) =>
                new ListeningVoaItem(i + 1, x.LearningItem.Title, x.AudioUrl, x.TranscriptCues.Count, x.TranscriptRevision, x.LearningItemId)).ToArray(), true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException
            or DecoderFallbackException or KeyNotFoundException or InvalidOperationException or System.Data.Common.DbException or DbUpdateException)
        {
            context.ChangeTracker.Clear();
            return new(false, commitAttempted ? "ListeningVoaCommitUnknown" : "ListeningVoaImportFailed", []);
        }
    }

    private async Task<(int LevelId, int TopicId)?> ClassificationAsync(CancellationToken cancellationToken)
    {
        var levels = await context.Levels.AsNoTracking().Where(x => x.Code == "A1").Select(x => x.Id).ToArrayAsync(cancellationToken);
        var topics = await context.Topics.AsNoTracking().Where(x => x.Name == "Daily Life").Select(x => x.Id).ToArrayAsync(cancellationToken);
        return levels.Length == 1 && topics.Length == 1 ? (levels[0], topics[0]) : null;
    }

    private Task<bool> HasDuplicatesAsync(IReadOnlyList<SourceLesson> lessons, CancellationToken cancellationToken)
    {
        var titles = lessons.Select(x => x.Title).ToArray();
        var urls = lessons.SelectMany(x => new[] { x.AudioUrl, x.ExternalAudioUrl }).ToArray();
        var pages = lessons.Select(x => x.PageUrl).ToArray();
        return context.ListeningLessons.AnyAsync(x => titles.Contains(x.LearningItem.Title)
            || urls.Contains(x.AudioUrl)
            || (x.LearningItem.Description != null && pages.Any(p => x.LearningItem.Description.Contains(p))), cancellationToken);
    }

    private async Task<IReadOnlyList<SourceLesson>> ReadSourcesAsync(CancellationToken cancellationToken)
    {
        var result = new List<SourceLesson>();
        for (var number = 1; number <= 5; number++)
        {
            var metadata = await ReadBoundedAsync(number, "source.json", 256 * 1024, cancellationToken);
            using var document = JsonDocument.Parse(metadata);
            var source = document.RootElement;
            if (source.GetProperty("lesson").GetInt32() != number
                || source.GetProperty("acceptance_status").GetString() != "ACCEPTED_WITH_LIMITATIONS"
                || source.GetProperty("audio_file").GetString() != "audio.mp3"
                || source.GetProperty("subtitle_file").GetString() != "subtitle.srt"
                || source.GetProperty("transcript_file").GetString() != "transcript.txt")
                throw new InvalidDataException("Source metadata mismatch.");
            var audio = await ReadBoundedAsync(number, "audio.mp3", ListeningAudioLimits.MaxBytes, cancellationToken);
            var subtitles = await ReadBoundedAsync(number, "subtitle.srt", ListeningSubtitleLimits.MaxSizeBytes, cancellationToken);
            var transcript = await ReadBoundedAsync(number, "transcript.txt", ListeningSubtitleLimits.MaxSizeBytes, cancellationToken);
            foreach (var (bytes, property) in new[] { (audio, "audio_sha256"), (subtitles, "subtitle_sha256"), (transcript, "transcript_sha256") })
                if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), source.GetProperty(property).GetString(), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Source hash mismatch.");
            if (string.IsNullOrWhiteSpace(new UTF8Encoding(false, true).GetString(transcript)))
                throw new InvalidDataException("Empty transcript.");
            using var audioStream = new MemoryStream(audio, false);
            if (await ListeningMp3Validator.ReadAsync(audioStream, cancellationToken) is null)
                throw new InvalidDataException("Invalid MP3.");
            using var subtitleStream = new MemoryStream(subtitles, false);
            var parsed = await parser.ParseAsync(subtitleStream, "subtitle.srt", subtitles.Length, cancellationToken);
            if (!parsed.IsValid) throw new InvalidDataException("Invalid SRT.");
            var page = source.GetProperty("page_url").GetString();
            var externalAudio = source.GetProperty("audio_url").GetString();
            if (!Uri.TryCreate(page, UriKind.Absolute, out var pageUri) || pageUri.Scheme != "https"
                || pageUri.Host != "learningenglish.voanews.com"
                || !Uri.TryCreate(externalAudio, UriKind.Absolute, out var audioUri) || audioUri.Scheme != "https")
                throw new InvalidDataException("Invalid source URL.");
            result.Add(new(number, Titles[number - 1], ListeningAudioStorage.UrlPrefix + "voa-" + number,
                externalAudio!, page!, parsed.Cues));
        }
        return result;
    }

    private async Task<byte[]> ReadBoundedAsync(int number, string name, int limit, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(storage.SourceFile(number, name), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        if (stream.Length is <= 0 || stream.Length > limit) throw new InvalidDataException("Source size invalid.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        return bytes;
    }

    private static IReadOnlyList<ListeningVoaItem> Summary(IReadOnlyList<SourceLesson> lessons) =>
        lessons.Select(x => new ListeningVoaItem(x.Number, x.Title, x.AudioUrl, x.Cues.Count, 1)).ToArray();
}
