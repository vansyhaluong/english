using System.ComponentModel.DataAnnotations;
using System.Data;
using English.Data;
using English.Interfaces;
using English.Models.Entities;
using English.Models.ViewModels.Vocabulary;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

public sealed class VocabularyService(ApplicationDbContext context, IMediaUrlValidator mediaValidator) : IVocabularyService
{
    // Persisted value verified against CK_LearningItem_Classification in the existing database.
    private const byte VocabularyKind = 1;
    private const int PageSize = 20;

    public async Task<VocabularyChoices> GetChoicesAsync(CancellationToken cancellationToken) => new(
        await context.Levels.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => new VocabularyOption(x.Id, x.Code)).ToArrayAsync(cancellationToken),
        await context.Topics.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new VocabularyOption(x.Id, x.Name)).ToArrayAsync(cancellationToken));

    private IQueryable<Vocabulary> Source(bool admin) => context.Vocabularies.AsNoTracking()
        .Where(x => x.LearningItem.Kind == VocabularyKind && (admin || (!x.LearningItem.IsDeleted && x.LearningItem.IsVisible)));

    private static IQueryable<VocabularyDetailsViewModel> Project(IQueryable<Vocabulary> source, Guid userId, bool details = true) => source.Select(x => new VocabularyDetailsViewModel
    {
        Id = x.LearningItemId, Title = x.LearningItem.Title, Word = x.Word,
        Pronunciation = x.Pronunciation, PartOfSpeech = x.PartOfSpeech, Example = details ? x.Example : string.Empty,
        Description = details ? x.LearningItem.Description : null, LevelId = x.LearningItem.LevelId, Level = x.LearningItem.Level.Code,
        TopicId = x.LearningItem.TopicId!.Value, Topic = x.LearningItem.Topic!.Name,
        IsVisible = x.LearningItem.IsVisible, IsDeleted = x.LearningItem.IsDeleted,
        IsLearned = x.LearningItem.UserLearningProgresses.Any(p => p.UserId == userId && p.IsLearned),
        ImageUrl = details ? x.ImageUrl : null, AudioUrl = details ? x.AudioUrl : null, RowVersion = details ? x.LearningItem.RowVersion : new byte[0],
        Meanings = x.VocabularyMeanings.OrderBy(m => m.DisplayOrder)
            .Select(m => new VocabularyMeaningViewModel(m.Id, m.MeaningVi, m.DisplayOrder)).ToList()
    });

    public async Task<VocabularyListViewModel> ListAsync(VocabularyQuery query, bool admin, Guid userId, CancellationToken cancellationToken)
    {
        query.Search = query.Search?.Trim();
        var source = Source(admin);
        if (!string.IsNullOrEmpty(query.Search))
        {
            var search = query.Search;
            source = source.Where(x => x.Word.Contains(search) || x.LearningItem.Title.Contains(search)
                || x.VocabularyMeanings.Any(m => m.MeaningVi.Contains(search)));
        }
        if (query.LevelId.HasValue) source = source.Where(x => x.LearningItem.LevelId == query.LevelId);
        if (query.TopicId.HasValue) source = source.Where(x => x.LearningItem.TopicId == query.TopicId);
        if (!admin && query.Learned.HasValue)
            source = source.Where(x => x.LearningItem.UserLearningProgresses.Any(p => p.UserId == userId && p.IsLearned) == query.Learned.Value);
        var count = await source.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        query.Page = Math.Clamp(query.Page, 1, pages);
        var items = await Project(source.OrderBy(x => x.Word).ThenBy(x => x.LearningItemId)
            .Skip((query.Page - 1) * PageSize).Take(PageSize), userId, details: false).ToArrayAsync(cancellationToken);
        return new(query, await GetChoicesAsync(cancellationToken), items, pages, admin);
    }

    public async Task<VocabularyDetailsViewModel?> GetAsync(int id, bool admin, Guid userId, CancellationToken cancellationToken)
    {
        var item = await Project(Source(admin).Where(x => x.LearningItemId == id), userId).SingleOrDefaultAsync(cancellationToken);
        // Admin must see invalid legacy URLs to correct them; Student never receives those URLs.
        if (item is not null && !admin) SanitizeMedia(item);
        return item;
    }

    private void SanitizeMedia(VocabularyDetailsViewModel item)
    {
        if (!mediaValidator.Validate(item.ImageUrl).IsValid) item.ImageUrl = null;
        if (!mediaValidator.Validate(item.AudioUrl).IsValid) item.AudioUrl = null;
    }

    public async Task<VocabularySaveResult> SaveAsync(int? id, VocabularyInputModel input, CancellationToken cancellationToken)
    {
        var errors = Validate(input, id.HasValue);
        if (errors.Count != 0) return new(VocabularyResult.Invalid, errors);
        if (!await context.Levels.AnyAsync(x => x.Id == input.LevelId, cancellationToken))
            errors.Add(new(nameof(input.LevelId), "VocabClassificationRequired"));
        if (!await context.Topics.AnyAsync(x => x.Id == input.TopicId, cancellationToken))
            errors.Add(new(nameof(input.TopicId), "VocabClassificationRequired"));
        if (errors.Count != 0) return new(VocabularyResult.Invalid, errors);

        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            Vocabulary vocabulary;
            if (id.HasValue)
            {
                var existing = await context.Vocabularies.Include(x => x.LearningItem).Include(x => x.VocabularyMeanings)
                    .SingleOrDefaultAsync(x => x.LearningItemId == id && x.LearningItem.Kind == VocabularyKind && !x.LearningItem.IsDeleted, cancellationToken);
                if (existing is null) return new(VocabularyResult.NotFound);
                vocabulary = existing;
                var version = ReadVersion(input.RowVersion);
                if (version is null || !vocabulary.LearningItem.RowVersion.SequenceEqual(version)) return new(VocabularyResult.Conflict);
                context.Entry(vocabulary.LearningItem).Property(x => x.RowVersion).OriginalValue = version;
                var ownedIds = vocabulary.VocabularyMeanings.Select(x => x.Id).ToHashSet();
                if (input.Meanings.Any(m => m.Id != 0 && !ownedIds.Contains(m.Id)))
                    return new(VocabularyResult.Invalid, [new("Meanings", "VocabInvalidMeaning")]);
            }
            else
            {
                vocabulary = new Vocabulary { LearningItem = new LearningItem { Kind = VocabularyKind, CreatedAt = DateTimeOffset.UtcNow } };
                context.Vocabularies.Add(vocabulary);
            }

            var item = vocabulary.LearningItem;
            item.Title = input.Title; item.Description = NullIfBlank(input.Description);
            item.LevelId = input.LevelId; item.TopicId = input.TopicId;
            item.IsVisible = input.IsVisible; item.UpdatedAt = DateTimeOffset.UtcNow;
            vocabulary.Word = input.Word; vocabulary.Pronunciation = input.Pronunciation;
            vocabulary.PartOfSpeech = input.PartOfSpeech; vocabulary.Example = input.Example;
            vocabulary.ImageUrl = NullIfBlank(input.ImageUrl); vocabulary.AudioUrl = NullIfBlank(input.AudioUrl);

            if (id.HasValue)
            {
                // SQL Server enforces positive, unique order immediately. Move ALL old rows
                // above both existing and final ranges, then assign final order in this transaction.
                var oldMeanings = vocabulary.VocabularyMeanings.OrderBy(m => m.DisplayOrder).ToArray();
                var maximum = Math.Max(oldMeanings.Select(m => m.DisplayOrder).DefaultIfEmpty().Max(), input.Meanings.Count);
                if ((long)maximum + oldMeanings.Length > int.MaxValue) return new(VocabularyResult.Conflict);
                for (var i = 0; i < oldMeanings.Length; i++) oldMeanings[i].DisplayOrder = maximum + i + 1;
                await context.SaveChangesAsync(cancellationToken);
                var retained = input.Meanings.Where(m => m.Id != 0).Select(m => m.Id).ToHashSet();
                context.VocabularyMeanings.RemoveRange(oldMeanings.Where(m => !retained.Contains(m.Id)));
            }

            for (var i = 0; i < input.Meanings.Count; i++)
            {
                var submitted = input.Meanings[i];
                var meaning = submitted.Id == 0 ? new VocabularyMeaning() : vocabulary.VocabularyMeanings.Single(m => m.Id == submitted.Id);
                meaning.MeaningVi = submitted.MeaningVi;
                meaning.DisplayOrder = i + 1;
                if (submitted.Id == 0) vocabulary.VocabularyMeanings.Add(meaning);
            }
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(VocabularyResult.Success);
        }
        catch (DbUpdateConcurrencyException) { return new(VocabularyResult.Conflict); }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return new(VocabularyResult.Conflict); }
    }

    private List<VocabularyError> Validate(VocabularyInputModel input, bool editing)
    {
        input.Title = input.Title?.Trim() ?? string.Empty;
        input.Word = input.Word?.Trim() ?? string.Empty;
        input.Pronunciation = input.Pronunciation?.Trim() ?? string.Empty;
        input.PartOfSpeech = input.PartOfSpeech?.Trim() ?? string.Empty;
        input.Example = input.Example?.Trim() ?? string.Empty;
        input.ImageUrl = NullIfBlank(input.ImageUrl); input.AudioUrl = NullIfBlank(input.AudioUrl);
        var errors = new List<VocabularyError>();
        AddAnnotationErrors(input, "", errors);
        if (!VocabularyPartsOfSpeech.Values.Contains(input.PartOfSpeech)) errors.Add(new("PartOfSpeech", "VocabInvalidPos"));
        foreach (var (field, url) in new[] { ("ImageUrl", input.ImageUrl), ("AudioUrl", input.AudioUrl) })
            if (url is not null && !mediaValidator.Validate(url).IsValid) errors.Add(new(field, "VocabInvalidMedia"));
        if (input.Meanings is null || input.Meanings.Count == 0) errors.Add(new("Meanings", "VocabMeaningRequired"));
        else
        {
            for (var i = 0; i < input.Meanings.Count; i++)
            {
                var meaning = input.Meanings[i];
                if (meaning is null) { errors.Add(new("Meanings", "VocabInvalidMeaning")); continue; }
                meaning.MeaningVi = meaning.MeaningVi?.Trim() ?? string.Empty;
                AddAnnotationErrors(meaning, $"Meanings[{i}].", errors);
            }
            var ids = input.Meanings.Where(m => m is not null && m.Id != 0).Select(m => m.Id).ToArray();
            if ((!editing && ids.Length != 0) || ids.Distinct().Count() != ids.Length) errors.Add(new("Meanings", "VocabInvalidMeaning"));
        }
        if (editing && ReadVersion(input.RowVersion) is null) errors.Add(new("RowVersion", "VocabConflict"));
        return errors;
    }

    private static void AddAnnotationErrors(object value, string prefix, List<VocabularyError> errors)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, true);
        foreach (var result in results)
            foreach (var member in result.MemberNames.DefaultIfEmpty(string.Empty))
                errors.Add(new(prefix + member, result.ErrorMessage ?? "VocabInvalid"));
    }

    public async Task<VocabularyResult> DeleteAsync(int id, string? rowVersion, CancellationToken cancellationToken)
    {
        var version = ReadVersion(rowVersion);
        if (version is null) return VocabularyResult.Conflict;
        var item = await context.LearningItems.SingleOrDefaultAsync(x => x.Id == id && x.Kind == VocabularyKind && !x.IsDeleted, cancellationToken);
        if (item is null) return VocabularyResult.NotFound;
        if (!item.RowVersion.SequenceEqual(version)) return VocabularyResult.Conflict;
        context.Entry(item).Property(x => x.RowVersion).OriginalValue = version;
        item.IsDeleted = true; item.DeletedAt = DateTimeOffset.UtcNow; item.UpdatedAt = item.DeletedAt.Value;
        // Preserve visibility, child meanings and every learning-progress row.
        try { await context.SaveChangesAsync(cancellationToken); return VocabularyResult.Success; }
        catch (DbUpdateConcurrencyException) { return VocabularyResult.Conflict; }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return VocabularyResult.Conflict; }
    }

    public async Task<VocabularyResult> SetLearnedAsync(int id, Guid userId, bool learned, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) return VocabularyResult.Invalid;
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            if (!await Source(false).AnyAsync(x => x.LearningItemId == id, cancellationToken)) return VocabularyResult.NotFound;
            var progress = await context.UserLearningProgresses.SingleOrDefaultAsync(x => x.UserId == userId && x.LearningItemId == id, cancellationToken);
            if (progress is null)
            {
                if (!learned) return VocabularyResult.Success;
                progress = new UserLearningProgress { UserId = userId, LearningItemId = id };
                context.UserLearningProgresses.Add(progress);
            }
            progress.IsLearned = learned;
            if (learned) progress.LearnedAt ??= DateTimeOffset.UtcNow;
            progress.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return VocabularyResult.Success;
        }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return VocabularyResult.Conflict; }
    }

    private static byte[]? ReadVersion(string? value)
    {
        Span<byte> bytes = stackalloc byte[8];
        return value is { Length: 12 } && Convert.TryFromBase64String(value, bytes, out var length) && length == 8 ? bytes.ToArray() : null;
    }
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsIntegrityConflict(Exception exception)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            if (cause is SqlException { Number: 547 or 2601 or 2627 or 1205 }) return true;
        return false;
    }
}
