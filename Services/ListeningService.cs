using System.ComponentModel.DataAnnotations;
using System.Data;
using English.Data;
using English.Interfaces;
using English.Models.Entities;
using English.Models;
using English.Models.ViewModels.Listening;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

public sealed class ListeningService(ApplicationDbContext context, SubtitleParser subtitleParser, ListeningAudioStorage audioStorage, ListeningDatabaseGuard databaseGuard) : IListeningService
{
    private const byte ListeningKind = 3;
    private const int PageSize = 20;

    private IQueryable<ListeningLesson> StudentSource() => Source()
        .Where(x => x.LearningItem.IsVisible && !x.LearningItem.IsDeleted);

    private static IQueryable<StudentListeningLessonViewModel> ProjectStudent(IQueryable<ListeningLesson> source, Guid userId) =>
        source.Select(x => new StudentListeningLessonViewModel
        {
            Id = x.LearningItemId, Title = x.LearningItem.Title, Description = x.LearningItem.Description,
            LevelId = x.LearningItem.LevelId, Level = x.LearningItem.Level.Code,
            TopicId = x.LearningItem.TopicId!.Value, Topic = x.LearningItem.Topic!.Name,
            AudioUrl = x.AudioUrl, TranscriptRevision = x.TranscriptRevision, CueCount = x.TranscriptCues.Count,
            IsLearned = x.LearningItem.UserLearningProgresses.Any(p => p.UserId == userId && p.IsLearned),
            LearnedAt = x.LearningItem.UserLearningProgresses.Where(p => p.UserId == userId && p.IsLearned)
                .Select(p => p.LearnedAt).FirstOrDefault()
        });

    public async Task<StudentListeningListViewModel> ListStudentAsync(StudentListeningQuery query, Guid userId, CancellationToken cancellationToken)
    {
        query.Search = query.Search?.Trim();
        var source = StudentSource();
        if (!string.IsNullOrEmpty(query.Search)) source = source.Where(x => x.LearningItem.Title.Contains(query.Search));
        if (query.LevelId.HasValue) source = source.Where(x => x.LearningItem.LevelId == query.LevelId);
        if (query.TopicId.HasValue) source = source.Where(x => x.LearningItem.TopicId == query.TopicId);
        var count = await source.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        query.Page = Math.Clamp(query.Page, 1, pages);
        var items = await ProjectStudent(source.OrderBy(x => x.LearningItem.Title).ThenBy(x => x.LearningItemId)
            .Skip((query.Page - 1) * PageSize).Take(PageSize), userId).ToArrayAsync(cancellationToken);
        return new(query, await GetChoicesAsync(cancellationToken), items, count, pages, PageSize);
    }

    public async Task<StudentListeningDetailsViewModel?> GetStudentAsync(int id, Guid userId, CancellationToken cancellationToken)
    {
        // Keep visibility, revision and cues consistent while a concurrent import replaces the transcript.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var lesson = await ProjectStudent(StudentSource().Where(x => x.LearningItemId == id), userId)
            .SingleOrDefaultAsync(cancellationToken);
        if (lesson is null) return null;
        var cues = await context.TranscriptCues.AsNoTracking().Where(x => x.ListeningLessonId == id)
            .OrderBy(x => x.Sequence).Select(x => new SubtitleCue(x.Sequence, x.StartMs, x.EndMs, x.Text))
            .ToArrayAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(lesson, cues);
    }

    public async Task<ListeningResult> SetLearnedAsync(int id, Guid userId, bool learned, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) return ListeningResult.Invalid;
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            if (!await StudentSource().AnyAsync(x => x.LearningItemId == id, cancellationToken)) return ListeningResult.NotFound;
            var progress = await context.UserLearningProgresses
                .SingleOrDefaultAsync(x => x.UserId == userId && x.LearningItemId == id, cancellationToken);
            if (progress is null)
            {
                if (!learned) return ListeningResult.Success;
                progress = new UserLearningProgress { UserId = userId, LearningItemId = id };
                context.UserLearningProgresses.Add(progress);
            }
            var now = DateTimeOffset.UtcNow;
            progress.IsLearned = learned;
            progress.LearnedAt = learned ? now : null;
            progress.UpdatedAt = now;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ListeningResult.Success;
        }
        catch (DbUpdateConcurrencyException) { return ListeningResult.Conflict; }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return ListeningResult.Conflict; }
    }

    public async Task<ListeningChoices> GetChoicesAsync(CancellationToken cancellationToken) => new(
        await context.Levels.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => new ListeningOption(x.Id, x.Code)).ToArrayAsync(cancellationToken),
        await context.Topics.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new ListeningOption(x.Id, x.Name)).ToArrayAsync(cancellationToken));

    private IQueryable<ListeningLesson> Source() => context.ListeningLessons.AsNoTracking().Where(x => x.LearningItem.Kind == ListeningKind);
    private static IQueryable<ListeningDetailsViewModel> Project(IQueryable<ListeningLesson> source) => source.Select(x => new ListeningDetailsViewModel
    {
        Id = x.LearningItemId, Title = x.LearningItem.Title, Description = x.LearningItem.Description,
        LevelId = x.LearningItem.LevelId, Level = x.LearningItem.Level.Code,
        TopicId = x.LearningItem.TopicId!.Value, Topic = x.LearningItem.Topic!.Name,
        AudioUrl = x.AudioUrl,
        AudioLocked = x.SubtitleFileId != null || x.TranscriptRevision > 0 || x.TranscriptCues.Any(),
        IsVisible = x.LearningItem.IsVisible, IsDeleted = x.LearningItem.IsDeleted, RowVersion = x.LearningItem.RowVersion,
        TranscriptRevision = x.TranscriptRevision, CueCount = x.TranscriptCues.Count
    });
    public async Task<ListeningListViewModel> ListAsync(ListeningQuery query, CancellationToken cancellationToken)
    {
        query.Search = query.Search?.Trim();
        var source = Source();
        if (!string.IsNullOrEmpty(query.Search)) source = source.Where(x => x.LearningItem.Title.Contains(query.Search));
        if (query.LevelId.HasValue) source = source.Where(x => x.LearningItem.LevelId == query.LevelId);
        if (query.TopicId.HasValue) source = source.Where(x => x.LearningItem.TopicId == query.TopicId);
        if (query.IsVisible.HasValue) source = source.Where(x => x.LearningItem.IsVisible == query.IsVisible);
        if (query.IsDeleted.HasValue) source = source.Where(x => x.LearningItem.IsDeleted == query.IsDeleted);
        var count = await source.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        query.Page = Math.Clamp(query.Page, 1, pages);
        var items = await Project(source.OrderBy(x => x.LearningItem.Title).ThenBy(x => x.LearningItemId)
            .Skip((query.Page - 1) * PageSize).Take(PageSize)).ToArrayAsync(cancellationToken);
        return new(query, await GetChoicesAsync(cancellationToken), items, pages);
    }
    public Task<ListeningDetailsViewModel?> GetAsync(int id, CancellationToken cancellationToken) =>
        Project(Source().Where(x => x.LearningItemId == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<ListeningSaveResult> SaveAsync(int? id, ListeningInputModel input, CancellationToken cancellationToken)
    {
        var validation = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), validation, true);
        var errors = validation.SelectMany(e => e.MemberNames.DefaultIfEmpty(string.Empty)
            .Select(field => new ListeningError(field, e.ErrorMessage ?? "ListeningRequired"))).ToList();
        var version = ReadVersion(input.RowVersion);
        if (id.HasValue && version is null) errors.Add(new(nameof(input.RowVersion), "ListeningConflict"));
        var videoId = ListeningYouTubeUrl.GetVideoId(input.AudioUrl);
        if ((!id.HasValue || !string.IsNullOrEmpty(input.AudioUrl)) && videoId is null)
            errors.Add(new(nameof(input.AudioUrl), "ListeningInvalidYouTubeUrl"));
        if (errors.Count != 0) return new(ListeningResult.Invalid, errors);
        var database = await databaseGuard.CheckAsync(cancellationToken);
        if (!database.IsSafe) return new(ListeningResult.Invalid, [new(string.Empty, database.Message)]);
        if (!await context.Levels.AnyAsync(x => x.Id == input.LevelId, cancellationToken)) errors.Add(new(nameof(input.LevelId), "ListeningClassificationRequired"));
        if (!await context.Topics.AnyAsync(x => x.Id == input.TopicId, cancellationToken)) errors.Add(new(nameof(input.TopicId), "ListeningClassificationRequired"));
        if (errors.Count != 0) return new(ListeningResult.Invalid, errors);

        var commitAttempted = false;
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            ListeningLesson lesson;
            if (id.HasValue)
            {
                var existing = await context.ListeningLessons.Include(x => x.LearningItem)
                    .SingleOrDefaultAsync(x => x.LearningItemId == id && x.LearningItem.Kind == ListeningKind && !x.LearningItem.IsDeleted, cancellationToken);
                if (existing is null) return new(ListeningResult.NotFound);
                lesson = existing;
                if (!lesson.LearningItem.RowVersion.SequenceEqual(version!)) return new(ListeningResult.Conflict);
                var hasCues = await context.TranscriptCues.AnyAsync(x => x.ListeningLessonId == id, cancellationToken);
                var currentVideoId = ListeningYouTubeUrl.GetVideoId(lesson.AudioUrl);
                if (videoId is null && currentVideoId is not null)
                    return new(ListeningResult.Invalid, [new(nameof(input.AudioUrl), "ListeningInvalidYouTubeUrl")]);
                if (videoId is not null && videoId != currentVideoId
                    && (lesson.SubtitleFileId != null || hasCues || lesson.TranscriptRevision > 0))
                    return new(ListeningResult.Invalid, [new(nameof(input.AudioUrl), "ListeningVideoLocked")]);
                context.Entry(lesson.LearningItem).Property(x => x.RowVersion).OriginalValue = version!;
            }
            else
            {
                lesson = new ListeningLesson { LearningItem = new LearningItem { Kind = ListeningKind, CreatedAt = DateTimeOffset.UtcNow } };
                context.ListeningLessons.Add(lesson);
            }
            // Blank input on a legacy lesson retains its existing source, including locked media.
            // Only a validated Video ID can select a new source; no media files are uploaded.
            if (videoId is not null) lesson.AudioUrl = ListeningYouTubeUrl.Normalize(videoId);
            var item = lesson.LearningItem;
            item.Title = input.Title;
            item.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
            item.LevelId = input.LevelId; item.TopicId = input.TopicId; item.GrammarGroupId = null;
            item.IsVisible = input.IsVisible; item.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            commitAttempted = true;
            await transaction.CommitAsync(cancellationToken);
            return new(ListeningResult.Success);
        }
        catch (DbUpdateConcurrencyException) { return new(ListeningResult.Conflict); }
        catch (Exception exception) when (exception is DbUpdateException or System.Data.Common.DbException)
        {
            return new(ListeningResult.Invalid, [new(string.Empty, commitAttempted ? "ListeningVideoCommitUnknown" : "ListeningVideoSaveFailed")]);
        }
    }

    public async Task<FileStream?> OpenAudioAsync(string key, CancellationToken cancellationToken)
    {
        if (!ListeningAudioStorage.IsKey(key)) return null;
        var url = ListeningAudioStorage.UrlPrefix + key;
        if (!await StudentSource().AnyAsync(x => x.AudioUrl == url, cancellationToken)) return null;
        try { return await audioStorage.OpenAsync(key, cancellationToken); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return null; }
    }
    public async Task<ListeningResult> SetVisibilityAsync(int id, ListeningVisibilityInputModel input, CancellationToken cancellationToken)
    {
        if (!input.IsVisible.HasValue) return ListeningResult.Invalid;
        var version = ReadVersion(input.RowVersion);
        if (version is null) return ListeningResult.Conflict;
        var item = await context.LearningItems.SingleOrDefaultAsync(x => x.Id == id && x.Kind == ListeningKind && !x.IsDeleted && x.ListeningLesson != null, cancellationToken);
        if (item is null) return ListeningResult.NotFound;
        if (!item.RowVersion.SequenceEqual(version)) return ListeningResult.Conflict;
        context.Entry(item).Property(x => x.RowVersion).OriginalValue = version;
        item.IsVisible = input.IsVisible.Value; item.UpdatedAt = DateTimeOffset.UtcNow;
        try { await context.SaveChangesAsync(cancellationToken); return ListeningResult.Success; }
        catch (DbUpdateConcurrencyException) { return ListeningResult.Conflict; }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return ListeningResult.Conflict; }
    }
    public async Task<ListeningResult> DeleteAsync(int id, string? rowVersion, CancellationToken cancellationToken)
    {
        var version = ReadVersion(rowVersion);
        if (version is null) return ListeningResult.Conflict;
        var item = await context.LearningItems.SingleOrDefaultAsync(x => x.Id == id && x.Kind == ListeningKind && !x.IsDeleted && x.ListeningLesson != null, cancellationToken);
        if (item is null) return ListeningResult.NotFound;
        if (!item.RowVersion.SequenceEqual(version)) return ListeningResult.Conflict;
        context.Entry(item).Property(x => x.RowVersion).OriginalValue = version;
        item.IsDeleted = true; item.DeletedAt = DateTimeOffset.UtcNow; item.UpdatedAt = item.DeletedAt.Value;
        try { await context.SaveChangesAsync(cancellationToken); return ListeningResult.Success; }
        catch (DbUpdateConcurrencyException) { return ListeningResult.Conflict; }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return ListeningResult.Conflict; }
    }
    public async Task<ListeningSubtitleImportResult> ImportSubtitleAsync(int id, ListeningSubtitleUpload upload, CancellationToken cancellationToken)
    {
        var version = ReadVersion(upload.RowVersion);
        if (version is null || upload.ExpectedRevision is null or < 0)
            return new(ListeningResult.Conflict, Error: new("ListeningConflict"));
        var parsed = await subtitleParser.ParseAsync(upload.Content, upload.FileName, upload.Length, cancellationToken);
        if (!parsed.IsValid) return new(ListeningResult.Invalid, Error: parsed.Error);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var lesson = await context.ListeningLessons.Include(x => x.LearningItem)
                .SingleOrDefaultAsync(x => x.LearningItemId == id && x.LearningItem.Kind == ListeningKind && !x.LearningItem.IsDeleted, cancellationToken);
            if (lesson is null) return new(ListeningResult.NotFound);
            if (!lesson.LearningItem.RowVersion.SequenceEqual(version) || lesson.TranscriptRevision != upload.ExpectedRevision)
                return new(ListeningResult.Conflict, Error: new("ListeningConflict"));
            var current = await context.TranscriptCues.Where(x => x.ListeningLessonId == id)
                .OrderBy(x => x.Sequence).ToListAsync(cancellationToken);
            if (CuesEqual(current, parsed.Cues)) return new(ListeningResult.Success, Unchanged: true);
            if (lesson.TranscriptRevision == int.MaxValue)
                return new(ListeningResult.Conflict, Error: new("ListeningSubtitleRevisionLimit"));

            // Delete old sequences before inserting new ones to avoid the unique lesson/sequence index.
            // Both saves remain inside this transaction: failure rolls back the entire replacement.
            context.TranscriptCues.RemoveRange(current);
            await context.SaveChangesAsync(cancellationToken);
            context.TranscriptCues.AddRange(parsed.Cues.Select(cue => new TranscriptCue
            {
                ListeningLessonId = id, Sequence = cue.Sequence, StartMs = cue.StartMs, EndMs = cue.EndMs, Text = cue.Text
            }));
            lesson.SubtitleFileId = null;
            lesson.TranscriptRevision++;
            // No LearningItem metadata/audio assignments; old StoredFile rows and physical files are retained.
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(ListeningResult.Success);
        }
        catch (DbUpdateConcurrencyException) { return new(ListeningResult.Conflict, Error: new("ListeningConflict")); }
        catch (Exception exception) when (exception is DbUpdateException or System.Data.Common.DbException)
        {
            return new(ListeningResult.Conflict, Error: new("ListeningSubtitleSaveFailed"));
        }
    }

    private static bool CuesEqual(IReadOnlyList<TranscriptCue> current, IReadOnlyList<SubtitleCue> incoming) =>
        current.Count == incoming.Count && current.Select(cue => new SubtitleCue(cue.Sequence, cue.StartMs, cue.EndMs, cue.Text)).SequenceEqual(incoming);

    private static byte[]? ReadVersion(string? value)
    {
        Span<byte> bytes = stackalloc byte[8];
        return value is { Length: 12 } && Convert.TryFromBase64String(value, bytes, out var length) && length == 8 ? bytes.ToArray() : null;
    }
    private static bool IsIntegrityConflict(Exception exception)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            if (cause is SqlException { Number: 547 or 2601 or 2627 or 1205 }) return true;
        return false;
    }
}
