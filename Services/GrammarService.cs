using System.ComponentModel.DataAnnotations;
using System.Data;
using English.Data;
using English.Interfaces;
using English.Models.Entities;
using English.Models.ViewModels.Grammar;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

public sealed class GrammarService(ApplicationDbContext context, GrammarHtmlSanitizer html) : IGrammarService
{
    // Confirmed by the user from enabled/trusted CK_LearningItem_Classification.
    private const byte GrammarKind = 2;
    private const int PageSize = 20;
    public async Task<GrammarChoices> GetChoicesAsync(CancellationToken cancellationToken) => new(
        await context.Levels.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => new GrammarOption(x.Id, x.Code)).ToArrayAsync(cancellationToken),
        await context.GrammarGroups.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new GrammarOption(x.Id, x.Name)).ToArrayAsync(cancellationToken));

    private IQueryable<GrammarLesson> Source(bool admin) => context.GrammarLessons.AsNoTracking()
        .Where(x => x.LearningItem.Kind == GrammarKind && (admin || (!x.LearningItem.IsDeleted && x.LearningItem.IsVisible)));
    private static IQueryable<GrammarDetailsViewModel> Project(IQueryable<GrammarLesson> source, Guid userId, bool details) => source.Select(x => new GrammarDetailsViewModel
    {
        Id = x.LearningItemId, Title = x.LearningItem.Title, Description = x.LearningItem.Description,
        LevelId = x.LearningItem.LevelId, Level = x.LearningItem.Level.Code,
        GrammarGroupId = x.LearningItem.GrammarGroupId!.Value, GrammarGroup = x.LearningItem.GrammarGroup!.Name,
        Formula = details ? x.Formula : string.Empty, Usage = details ? x.Usage : string.Empty,
        Examples = details ? x.Examples : string.Empty, Notes = details ? x.Notes : null,
        IsVisible = x.LearningItem.IsVisible, IsDeleted = x.LearningItem.IsDeleted,
        IsLearned = x.LearningItem.UserLearningProgresses.Any(p => p.UserId == userId && p.IsLearned),
        RowVersion = details ? x.LearningItem.RowVersion : new byte[0]
    });
    public async Task<GrammarListViewModel> ListAsync(GrammarQuery query, bool admin, Guid userId, CancellationToken cancellationToken)
    {
        query.Search = query.Search?.Trim();
        var source = Source(admin);
        if (!string.IsNullOrEmpty(query.Search)) source = source.Where(x => x.LearningItem.Title.Contains(query.Search));
        if (query.LevelId.HasValue) source = source.Where(x => x.LearningItem.LevelId == query.LevelId);
        if (query.GrammarGroupId.HasValue) source = source.Where(x => x.LearningItem.GrammarGroupId == query.GrammarGroupId);
        var count = await source.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        query.Page = Math.Clamp(query.Page, 1, pages);
        var items = await Project(source.OrderBy(x => x.LearningItem.Title).ThenBy(x => x.LearningItemId)
            .Skip((query.Page - 1) * PageSize).Take(PageSize), userId, false).ToArrayAsync(cancellationToken);
        return new(query, await GetChoicesAsync(cancellationToken), items, pages, admin);
    }
    public async Task<GrammarWorkspaceViewModel> GetWorkspaceAsync(GrammarQuery query, int? id, Guid userId, CancellationToken cancellationToken)
    {
        query.Search = query.Search?.Trim();
        var source = Source(false);
        if (!string.IsNullOrEmpty(query.Search)) source = source.Where(x => x.LearningItem.Title.Contains(query.Search));
        if (query.LevelId.HasValue) source = source.Where(x => x.LearningItem.LevelId == query.LevelId);
        if (query.GrammarGroupId.HasValue) source = source.Where(x => x.LearningItem.GrammarGroupId == query.GrammarGroupId);
        // The sidebar needs the full matching catalog for accurate group counts, without lesson HTML.
        var items = await Project(source.OrderBy(x => x.LearningItem.Level.SortOrder)
            .ThenBy(x => x.LearningItem.GrammarGroup!.Name).ThenBy(x => x.LearningItem.Title)
            .ThenBy(x => x.LearningItemId), userId, false).ToArrayAsync(cancellationToken);
        var selectedId = id ?? items.FirstOrDefault()?.Id;
        var lesson = selectedId.HasValue ? await GetAsync(selectedId.Value, false, userId, cancellationToken) : null;
        return new(query, await GetChoicesAsync(cancellationToken), items, lesson);
    }
    public async Task<GrammarDetailsViewModel?> GetAsync(int id, bool admin, Guid userId, CancellationToken cancellationToken)
    {
        var item = await Project(Source(admin).Where(x => x.LearningItemId == id), userId, true).SingleOrDefaultAsync(cancellationToken);
        // Sanitize on read as well: legacy or externally written content is never trusted.
        if (item is not null)
        {
            item.Formula = html.Sanitize(item.Formula); item.Usage = html.Sanitize(item.Usage);
            item.Examples = html.Sanitize(item.Examples); item.Notes = html.Sanitize(item.Notes);
        }
        return item;
    }
    public async Task<GrammarSaveResult> SaveAsync(int? id, GrammarInputModel input, CancellationToken cancellationToken)
    {
        input.Title = input.Title?.Trim() ?? string.Empty;
        input.Formula = html.Sanitize(input.Formula); input.Usage = html.Sanitize(input.Usage);
        input.Examples = html.Sanitize(input.Examples); input.Notes = html.Sanitize(input.Notes);
        var errors = new List<GrammarError>();
        var validation = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), validation, true);
        foreach (var error in validation)
            foreach (var member in error.MemberNames.DefaultIfEmpty(string.Empty)) errors.Add(new(member, error.ErrorMessage ?? "GrammarRequired"));
        foreach (var (field, value) in new[] { (nameof(input.Formula), input.Formula), (nameof(input.Usage), input.Usage), (nameof(input.Examples), input.Examples) })
            if (!html.HasContent(value)) errors.Add(new(field, "GrammarRequired"));
        if (id.HasValue && ReadVersion(input.RowVersion) is null) errors.Add(new(nameof(input.RowVersion), "GrammarConflict"));
        if (errors.Count != 0) return new(GrammarResult.Invalid, errors);
        if (!await context.Levels.AnyAsync(x => x.Id == input.LevelId, cancellationToken)) errors.Add(new(nameof(input.LevelId), "GrammarClassificationRequired"));
        if (!await context.GrammarGroups.AnyAsync(x => x.Id == input.GrammarGroupId, cancellationToken)) errors.Add(new(nameof(input.GrammarGroupId), "GrammarClassificationRequired"));
        if (errors.Count != 0) return new(GrammarResult.Invalid, errors);
        try
        {
            GrammarLesson lesson;
            if (id.HasValue)
            {
                var existing = await context.GrammarLessons.Include(x => x.LearningItem)
                    .SingleOrDefaultAsync(x => x.LearningItemId == id && x.LearningItem.Kind == GrammarKind && !x.LearningItem.IsDeleted, cancellationToken);
                if (existing is null) return new(GrammarResult.NotFound);
                lesson = existing;
                var version = ReadVersion(input.RowVersion)!;
                if (!lesson.LearningItem.RowVersion.SequenceEqual(version)) return new(GrammarResult.Conflict);
                context.Entry(lesson.LearningItem).Property(x => x.RowVersion).OriginalValue = version;
            }
            else
            {
                lesson = new GrammarLesson { LearningItem = new LearningItem { Kind = GrammarKind, CreatedAt = DateTimeOffset.UtcNow } };
                context.GrammarLessons.Add(lesson);
            }
            lesson.LearningItem.Title = input.Title;
            lesson.LearningItem.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
            lesson.LearningItem.LevelId = input.LevelId; lesson.LearningItem.GrammarGroupId = input.GrammarGroupId;
            lesson.LearningItem.TopicId = null;
            lesson.LearningItem.IsVisible = input.IsVisible; lesson.LearningItem.UpdatedAt = DateTimeOffset.UtcNow;
            lesson.Formula = input.Formula; lesson.Usage = input.Usage; lesson.Examples = input.Examples;
            lesson.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes;
            // EF commits the parent and shared-PK subtype atomically in one SaveChanges.
            await context.SaveChangesAsync(cancellationToken);
            return new(GrammarResult.Success);
        }
        catch (DbUpdateConcurrencyException) { return new(GrammarResult.Conflict); }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return new(GrammarResult.Conflict); }
    }
    public async Task<GrammarResult> DeleteAsync(int id, string? rowVersion, CancellationToken cancellationToken)
    {
        var version = ReadVersion(rowVersion);
        if (version is null) return GrammarResult.Conflict;
        var item = await context.LearningItems.SingleOrDefaultAsync(x => x.Id == id && x.Kind == GrammarKind && !x.IsDeleted, cancellationToken);
        if (item is null) return GrammarResult.NotFound;
        if (!item.RowVersion.SequenceEqual(version)) return GrammarResult.Conflict;
        context.Entry(item).Property(x => x.RowVersion).OriginalValue = version;
        item.IsDeleted = true; item.DeletedAt = DateTimeOffset.UtcNow; item.UpdatedAt = item.DeletedAt.Value;
        // Match Vocabulary: retain visibility, subtype content and all progress/history.
        try { await context.SaveChangesAsync(cancellationToken); return GrammarResult.Success; }
        catch (DbUpdateConcurrencyException) { return GrammarResult.Conflict; }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return GrammarResult.Conflict; }
    }
    public async Task<GrammarResult> SetLearnedAsync(int id, Guid userId, bool learned, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) return GrammarResult.Invalid;
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            if (!await Source(false).AnyAsync(x => x.LearningItemId == id, cancellationToken)) return GrammarResult.NotFound;
            var progress = await context.UserLearningProgresses.SingleOrDefaultAsync(x => x.UserId == userId && x.LearningItemId == id, cancellationToken);
            if (progress is null)
            {
                if (!learned) return GrammarResult.Success;
                progress = new UserLearningProgress { UserId = userId, LearningItemId = id };
                context.UserLearningProgresses.Add(progress);
            }
            var now = DateTimeOffset.UtcNow;
            progress.IsLearned = learned; progress.LearnedAt = learned ? now : null; progress.UpdatedAt = now;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return GrammarResult.Success;
        }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return GrammarResult.Conflict; }
    }
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
