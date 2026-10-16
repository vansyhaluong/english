using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using English.Data;
using English.Interfaces;
using English.Models;
using English.Models.Entities;
using English.Models.ViewModels.Books;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

public sealed class BooksService(ApplicationDbContext context, BooksDatabaseGuard guard,
    IFileStorage fileStorage, ILogger<BooksService> logger) : IBooksService
{
    private const int PageSize = 20;
    private IQueryable<Book> AvailableBooks() => context.Books.AsNoTracking().Where(b => b.IsVisible && !b.IsDeleted);
    private IQueryable<BookChapter> AvailableChapters(int bookId) => context.BookChapters.AsNoTracking()
        .Where(c => c.BookId == bookId && c.IsVisible && !c.IsDeleted && c.Book.IsVisible && !c.Book.IsDeleted);

    private static IQueryable<AdminBookDto> AdminBooks(IQueryable<Book> source) => source.Select(b => new AdminBookDto
    {
        Id = b.Id, TopicId = b.TopicId, Topic = b.Topic.Name, Title = b.Title, Author = b.Author,
        Description = b.Description, CoverUrl = b.CoverUrl, IsVisible = b.IsVisible, IsDeleted = b.IsDeleted,
        DeletedAt = b.DeletedAt, RowVersion = b.RowVersion
    });

    private static IQueryable<AdminChapterDto> AdminChapters(IQueryable<BookChapter> source) => source.Select(c => new AdminChapterDto
    {
        Id = c.Id, BookId = c.BookId, DisplayOrder = c.DisplayOrder, Title = c.Title, ContentEn = c.ContentEn,
        TranslationVi = c.TranslationVi, VocabularyNotes = c.VocabularyNotes, IsVisible = c.IsVisible,
        IsDeleted = c.IsDeleted, DeletedAt = c.DeletedAt, RowVersion = c.RowVersion
    });

    private static IQueryable<StudentBookDto> StudentBooks(IQueryable<Book> source, Guid userId) => source.Select(b => new StudentBookDto
    {
        Id = b.Id, TopicId = b.TopicId, Topic = b.Topic.Name, Title = b.Title, Author = b.Author,
        Description = b.Description, CoverUrl = b.CoverUrl,
        AvailableChapterCount = b.BookChapters.Count(c => c.IsVisible && !c.IsDeleted),
        ReadChapterCount = b.BookChapters.Count(c => c.IsVisible && !c.IsDeleted
            && c.UserChapterProgresses.Any(p => p.UserId == userId && p.IsRead)),
        ContinueChapterId = b.BookReadingPositions
            .Where(p => p.UserId == userId && p.BookChapter.IsVisible && !p.BookChapter.IsDeleted)
            .Select(p => (int?)p.LastChapterId).FirstOrDefault()
            ?? b.BookChapters.Where(c => c.IsVisible && !c.IsDeleted).OrderBy(c => c.DisplayOrder)
                .Select(c => (int?)c.Id).FirstOrDefault()
    });

    public async Task<IReadOnlyList<BookTopicOption>> GetTopicsAsync(CancellationToken cancellationToken)
    {
        guard.EnsureTarget();
        return await context.Topics.AsNoTracking().OrderBy(t => t.Name).ThenBy(t => t.Id)
            .Select(t => new BookTopicOption(t.Id, t.Name)).ToArrayAsync(cancellationToken);
    }

    public async Task<BooksPage<AdminBookDto>> ListAdminAsync(AdminBooksQuery query, CancellationToken cancellationToken)
    {
        guard.EnsureTarget();
        var source = context.Books.AsNoTracking();
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search)) source = source.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
        if (query.TopicId.HasValue) source = source.Where(b => b.TopicId == query.TopicId);
        if (query.IsVisible.HasValue) source = source.Where(b => b.IsVisible == query.IsVisible);
        if (query.IsDeleted.HasValue) source = source.Where(b => b.IsDeleted == query.IsDeleted);
        var count = await source.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        var page = Math.Clamp(query.Page, 1, pages);
        var items = await AdminBooks(source.OrderBy(b => b.Title).ThenBy(b => b.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)).ToArrayAsync(cancellationToken);
        return new(items, count, page, pages, PageSize);
    }

    public async Task<AdminBookDetailsDto?> GetAdminAsync(int bookId, CancellationToken cancellationToken)
    {
        guard.EnsureTarget();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var book = await AdminBooks(context.Books.AsNoTracking().Where(b => b.Id == bookId)).SingleOrDefaultAsync(cancellationToken);
        if (book is null) return null;
        var chapters = await context.BookChapters.AsNoTracking().Where(c => c.BookId == bookId)
            .OrderBy(c => c.DisplayOrder).Select(c => new AdminChapterSummary(c.Id, c.BookId, c.DisplayOrder,
                c.Title, c.IsVisible, c.IsDeleted, c.DeletedAt, c.RowVersion)).ToArrayAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(book, chapters);
    }

    public async Task<AdminChapterDto?> GetAdminChapterAsync(int bookId, int chapterId, CancellationToken cancellationToken)
    {
        guard.EnsureTarget();
        return await AdminChapters(context.BookChapters.AsNoTracking()
            .Where(c => c.BookId == bookId && c.Id == chapterId)).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<BooksMutationResult> SaveBookAsync(int? bookId, BookInput input, CancellationToken cancellationToken) =>
        SaveBookCoreAsync(bookId, input, false, null, cancellationToken);

    private Task<BooksMutationResult> SaveBookCoreAsync(int? bookId, BookInput input, bool preserveCover,
        string? uploadedCoverUrl, CancellationToken cancellationToken) => MutateAsync(async () =>
    {
        var errors = Validate(input);
        var cover = preserveCover ? uploadedCoverUrl : Optional(input.CoverUrl)?.Trim();
        if (!preserveCover && cover is not null && (!Uri.TryCreate(cover, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || uri.UserInfo.Length != 0 || cover.Any(char.IsControl) || cover.Any(char.IsWhiteSpace)))
            errors.Add(new(nameof(input.CoverUrl), "BooksInvalidCoverUrl"));
        if (errors.Count != 0) return new(BooksResult.Invalid, Errors: errors);
        if (!await context.Topics.AnyAsync(t => t.Id == input.TopicId, cancellationToken))
            return Invalid(nameof(input.TopicId), "BooksTopicNotFound");
        Book book;
        if (bookId.HasValue)
        {
            var existing = await EditableBookAsync(bookId.Value, cancellationToken);
            if (existing is null) return new(BooksResult.NotFound);
            if (!Matches(existing.RowVersion, input.RowVersion)) return new(BooksResult.Conflict);
            book = existing;
        }
        else
        {
            book = new Book { CreatedAt = DateTimeOffset.UtcNow };
            context.Books.Add(book);
        }
        book.Title = input.Title.Trim(); book.Author = input.Author.Trim(); book.TopicId = input.TopicId;
        book.Description = Optional(input.Description); book.IsVisible = input.IsVisible;
        if (!preserveCover || cover is not null) book.CoverUrl = cover;
        book.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new(BooksResult.Success, book.Id, book.RowVersion);
    }, cancellationToken);

    public async Task<BooksMutationResult> SaveBookWithCoverAsync(int? bookId, BookInput input,
        BookCoverUpload? cover, CancellationToken cancellationToken)
    {
        guard.EnsureTarget();
        if (cover is null) return await SaveBookCoreAsync(bookId, input, true, null, cancellationToken);
        if (cover.SizeBytes <= 0 || cover.SizeBytes > BookCoverImages.MaxSizeBytes)
            return Invalid(nameof(AdminBookUploadInput.CoverImage), "BooksCoverTooLarge");
        var errors = Validate(input);
        if (errors.Count != 0) return new(BooksResult.Invalid, Errors: errors);

        string? storageKey = null;
        var saved = false;
        try
        {
            var bytes = await BookCoverImages.ReadBoundedAsync(cover.Content, cancellationToken);
            // Reuse the existing full-image decoder. Browser MIME, extension and filename are never used.
            if (!AvatarImageContent.TryDecode(bytes, out _, out var extension))
                return Invalid(nameof(AdminBookUploadInput.CoverImage), "BooksCoverInvalidContent");
            await using var stream = new MemoryStream(bytes, writable: false);
            storageKey = await fileStorage.SaveAsync(stream, extension, cancellationToken);
            var result = await SaveBookCoreAsync(bookId, input, true, BookCoverPath.Prefix + storageKey, cancellationToken);
            saved = result.Status == BooksResult.Success;
            return result;
        }
        catch (InvalidDataException)
        {
            return Invalid(nameof(AdminBookUploadInput.CoverImage), "BooksCoverTooLarge");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DbException or DbUpdateException)
        {
            return Invalid(nameof(AdminBookUploadInput.CoverImage), "BooksCoverSaveFailed");
        }
        finally
        {
            if (!saved && storageKey is not null)
            {
                try { await fileStorage.DeleteAsync(storageKey, CancellationToken.None); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    logger.LogWarning("Unable to clean up an unsaved book cover.");
                }
            }
        }
    }

    public async Task<BookCoverFile?> GetCoverAsync(string storageKey, bool isAdmin, CancellationToken cancellationToken)
    {
        guard.EnsureTarget();
        if (!BookCoverPath.IsStorageKey(storageKey)) return null;
        var coverUrl = BookCoverPath.Prefix + storageKey;
        // Resolve only keys referenced by an authorized book, never arbitrary files in shared storage.
        if (!await context.Books.AsNoTracking().AnyAsync(b => b.CoverUrl == coverUrl
            && (isAdmin || (b.IsVisible && !b.IsDeleted)), cancellationToken)) return null;
        try
        {
            await using var stream = await fileStorage.OpenReadAsync(storageKey, cancellationToken);
            if (stream is null) return null;
            var bytes = await BookCoverImages.ReadBoundedAsync(stream, cancellationToken);
            if (!AvatarImageContent.TryDecode(bytes, out var contentType, out var extension)
                || !storageKey.EndsWith(extension, StringComparison.Ordinal)) return null;
            return new(bytes, contentType);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public Task<BooksMutationResult> SetBookVisibilityAsync(int bookId, BookVisibilityInput input, CancellationToken cancellationToken) =>
        ChangeBookAsync(bookId, input, input.IsVisible, false, cancellationToken);

    public Task<BooksMutationResult> DeleteBookAsync(int bookId, BookVersionInput input, CancellationToken cancellationToken) =>
        ChangeBookAsync(bookId, input, null, true, cancellationToken);

    private Task<BooksMutationResult> ChangeBookAsync(int bookId, BookVersionInput input, bool? visible, bool delete,
        CancellationToken cancellationToken) => MutateAsync(async () =>
    {
        var errors = Validate(input);
        if (errors.Count != 0) return new(BooksResult.Invalid, Errors: errors);
        if (!delete && !visible.HasValue) return Invalid(nameof(BookVisibilityInput.IsVisible), "BooksRequired");
        var book = await EditableBookAsync(bookId, cancellationToken);
        if (book is null) return new(BooksResult.NotFound);
        if (!Matches(book.RowVersion, input.RowVersion)) return new(BooksResult.Conflict);
        book.UpdatedAt = DateTimeOffset.UtcNow;
        if (delete) { book.IsDeleted = true; book.DeletedAt = book.UpdatedAt; }
        else book.IsVisible = visible!.Value;
        // Keep chapters, read marks and reading positions intact.
        await context.SaveChangesAsync(cancellationToken);
        return new(BooksResult.Success, book.Id, book.RowVersion);
    }, cancellationToken);

    public Task<BooksMutationResult> SaveChapterAsync(int bookId, int? chapterId, ChapterInput input,
        CancellationToken cancellationToken) => MutateAsync(async () =>
    {
        var errors = Validate(input);
        if (errors.Count != 0) return new(BooksResult.Invalid, Errors: errors);
        var book = await EditableBookAsync(bookId, cancellationToken);
        if (book is null) return new(BooksResult.NotFound);
        BookChapter chapter;
        if (chapterId.HasValue)
        {
            var existing = await context.BookChapters.SingleOrDefaultAsync(c => c.Id == chapterId && c.BookId == bookId && !c.IsDeleted, cancellationToken);
            if (existing is null) return new(BooksResult.NotFound);
            if (!Matches(existing.RowVersion, input.RowVersion)) return new(BooksResult.Conflict);
            chapter = existing;
        }
        else
        {
            chapter = new BookChapter { BookId = bookId, CreatedAt = DateTimeOffset.UtcNow };
            context.BookChapters.Add(chapter);
        }
        // Deleted chapters still reserve their order; swaps use ReorderChaptersAsync.
        if (await context.BookChapters.AnyAsync(c => c.BookId == bookId && c.DisplayOrder == input.DisplayOrder
            && (!chapterId.HasValue || c.Id != chapterId), cancellationToken))
            return Invalid(nameof(input.DisplayOrder), "BooksOrderInUse");
        chapter.Title = input.Title.Trim(); chapter.ContentEn = input.ContentEn;
        chapter.TranslationVi = Optional(input.TranslationVi); chapter.VocabularyNotes = Optional(input.VocabularyNotes);
        chapter.DisplayOrder = input.DisplayOrder; chapter.IsVisible = input.IsVisible;
        chapter.UpdatedAt = book.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new(BooksResult.Success, chapter.Id, chapter.RowVersion);
    }, cancellationToken);

    public Task<BooksMutationResult> SaveVocabularyNotesAsync(int bookId, int chapterId, ChapterVocabularyInput input,
        CancellationToken cancellationToken) => MutateAsync(async () =>
    {
        var errors = Validate(input);
        if (errors.Count != 0) return new(BooksResult.Invalid, Errors: errors);
        var book = await EditableBookAsync(bookId, cancellationToken);
        if (book is null) return new(BooksResult.NotFound);
        var chapter = await context.BookChapters.SingleOrDefaultAsync(
            c => c.BookId == bookId && c.Id == chapterId && !c.IsDeleted, cancellationToken);
        if (chapter is null) return new(BooksResult.NotFound);
        if (!Matches(chapter.RowVersion, input.RowVersion)) return new(BooksResult.Conflict);
        chapter.VocabularyNotes = BookVocabularyNotes.Write(input.Entries, chapter.VocabularyNotes);
        chapter.UpdatedAt = book.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new(BooksResult.Success, chapter.Id, chapter.RowVersion);
    }, cancellationToken);

    public Task<BooksMutationResult> SetChapterVisibilityAsync(int bookId, int chapterId, BookVisibilityInput input,
        CancellationToken cancellationToken) => ChangeChapterAsync(bookId, chapterId, input, input.IsVisible, false, cancellationToken);

    public Task<BooksMutationResult> DeleteChapterAsync(int bookId, int chapterId, BookVersionInput input,
        CancellationToken cancellationToken) => ChangeChapterAsync(bookId, chapterId, input, null, true, cancellationToken);

    private Task<BooksMutationResult> ChangeChapterAsync(int bookId, int chapterId, BookVersionInput input, bool? visible,
        bool delete, CancellationToken cancellationToken) => MutateAsync(async () =>
    {
        var errors = Validate(input);
        if (errors.Count != 0) return new(BooksResult.Invalid, Errors: errors);
        if (!delete && !visible.HasValue) return Invalid(nameof(BookVisibilityInput.IsVisible), "BooksRequired");
        var book = await EditableBookAsync(bookId, cancellationToken);
        if (book is null) return new(BooksResult.NotFound);
        var chapter = await context.BookChapters.SingleOrDefaultAsync(c => c.Id == chapterId && c.BookId == bookId && !c.IsDeleted, cancellationToken);
        if (chapter is null) return new(BooksResult.NotFound);
        if (!Matches(chapter.RowVersion, input.RowVersion)) return new(BooksResult.Conflict);
        chapter.UpdatedAt = book.UpdatedAt = DateTimeOffset.UtcNow;
        if (delete) { chapter.IsDeleted = true; chapter.DeletedAt = chapter.UpdatedAt; }
        else chapter.IsVisible = visible!.Value;
        await context.SaveChangesAsync(cancellationToken);
        return new(BooksResult.Success, chapter.Id, chapter.RowVersion);
    }, cancellationToken);

    public Task<BooksMutationResult> ReorderChaptersAsync(int bookId, ChaptersOrderInput input,
        CancellationToken cancellationToken) => MutateAsync(async () =>
    {
        var errors = Validate(input);
        if (errors.Count != 0) return new(BooksResult.Invalid, Errors: errors);
        if (input.Chapters.Any(c => c is null)) return Invalid(nameof(input.Chapters), "BooksRequired");
        foreach (var entry in input.Chapters) errors.AddRange(Validate(entry));
        if (errors.Count != 0) return new(BooksResult.Invalid, Errors: errors);
        if (input.Chapters.Select(c => c.ChapterId).Distinct().Count() != input.Chapters.Count
            || input.Chapters.Select(c => c.DisplayOrder).Distinct().Count() != input.Chapters.Count)
            return Invalid(nameof(input.Chapters), "BooksDuplicateOrderOrChapter");
        var book = await EditableBookAsync(bookId, cancellationToken);
        if (book is null) return new(BooksResult.NotFound);
        var all = await context.BookChapters.Where(c => c.BookId == bookId).OrderBy(c => c.Id).ToArrayAsync(cancellationToken);
        var byId = all.ToDictionary(c => c.Id);
        var requested = input.Chapters.ToDictionary(c => c.ChapterId);
        foreach (var entry in input.Chapters)
        {
            if (!byId.TryGetValue(entry.ChapterId, out var chapter) || chapter.IsDeleted) return new(BooksResult.NotFound);
            if (!Matches(chapter.RowVersion, entry.RowVersion)) return new(BooksResult.Conflict);
        }
        var finalOrders = all.Select(c => requested.TryGetValue(c.Id, out var entry) ? entry.DisplayOrder : c.DisplayOrder).ToArray();
        if (finalOrders.Distinct().Count() != finalOrders.Length) return Invalid(nameof(input.Chapters), "BooksOrderInUse");
        var changed = input.Chapters.Where(e => byId[e.ChapterId].DisplayOrder != e.DisplayOrder).ToArray();
        if (changed.Length == 0) return new(BooksResult.Success, book.Id, book.RowVersion);

        // Allocate unused POSITIVE slots, excluding both current and final orders.
        // This satisfies CHECK DisplayOrder > 0 and avoids transient unique collisions in swaps/cycles.
        var occupied = all.Select(c => c.DisplayOrder).Concat(finalOrders).ToHashSet();
        long temporary = 1;
        foreach (var entry in changed)
        {
            while (temporary <= int.MaxValue && occupied.Contains((int)temporary)) temporary++;
            if (temporary > int.MaxValue) return Invalid(nameof(input.Chapters), "BooksNoFreeOrder");
            byId[entry.ChapterId].DisplayOrder = (int)temporary;
            occupied.Add((int)temporary++);
        }
        await context.SaveChangesAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in changed)
        {
            byId[entry.ChapterId].DisplayOrder = entry.DisplayOrder;
            byId[entry.ChapterId].UpdatedAt = now;
        }
        book.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);
        return new(BooksResult.Success, book.Id, book.RowVersion);
    }, cancellationToken);

    public async Task<BooksPage<StudentBookDto>> ListStudentAsync(BooksQuery query, Guid userId, CancellationToken cancellationToken)
    {
        guard.EnsureTarget(); EnsureUser(userId);
        var source = AvailableBooks();
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search)) source = source.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
        if (query.TopicId.HasValue) source = source.Where(b => b.TopicId == query.TopicId);
        var count = await source.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        var page = Math.Clamp(query.Page, 1, pages);
        var items = await StudentBooks(source.OrderBy(b => b.Title).ThenBy(b => b.Id)
            .Skip((page - 1) * PageSize).Take(PageSize), userId).ToArrayAsync(cancellationToken);
        return new(items, count, page, pages, PageSize);
    }

    public async Task<StudentBookDetailsDto?> GetStudentAsync(int bookId, Guid userId, CancellationToken cancellationToken)
    {
        guard.EnsureTarget(); EnsureUser(userId);
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var book = await StudentBooks(AvailableBooks().Where(b => b.Id == bookId), userId).SingleOrDefaultAsync(cancellationToken);
        if (book is null) return null;
        var chapters = await AvailableChapters(bookId).OrderBy(c => c.DisplayOrder).Select(c => new StudentChapterSummary(
            c.Id, c.DisplayOrder, c.Title, c.UserChapterProgresses.Any(p => p.UserId == userId && p.IsRead),
            c.UserChapterProgresses.Where(p => p.UserId == userId && p.IsRead).Select(p => p.ReadAt).FirstOrDefault()))
            .ToArrayAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(book, chapters);
    }

    public async Task<StudentChapterDto?> GetChapterAsync(int bookId, int chapterId, Guid userId, CancellationToken cancellationToken)
    {
        guard.EnsureTarget(); EnsureUser(userId);
        // Reading is side-effect free. The client records an actual open with a separate protected POST.
        return await AvailableChapters(bookId).Where(c => c.Id == chapterId).Select(c => new StudentChapterDto(
            c.Id, c.BookId, c.DisplayOrder, c.Title, c.ContentEn, c.TranslationVi, c.VocabularyNotes,
            c.UserChapterProgresses.Any(p => p.UserId == userId && p.IsRead),
            c.UserChapterProgresses.Where(p => p.UserId == userId && p.IsRead).Select(p => p.ReadAt).FirstOrDefault(),
            c.Book.BookChapters.Where(other => other.IsVisible && !other.IsDeleted && other.DisplayOrder < c.DisplayOrder)
                .OrderByDescending(other => other.DisplayOrder).Select(other => (int?)other.Id).FirstOrDefault(),
            c.Book.BookChapters.Where(other => other.IsVisible && !other.IsDeleted && other.DisplayOrder > c.DisplayOrder)
                .OrderBy(other => other.DisplayOrder).Select(other => (int?)other.Id).FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<BooksMutationResult> SetReadAsync(int bookId, int chapterId, Guid userId, bool isRead,
        CancellationToken cancellationToken) => MutateAsync(async () =>
    {
        if (userId == Guid.Empty) return Invalid("UserId", "BooksInvalidUser");
        if (!await AvailableChapters(bookId).AnyAsync(c => c.Id == chapterId, cancellationToken)) return new(BooksResult.NotFound);
        var progress = await context.UserChapterProgresses.SingleOrDefaultAsync(p => p.UserId == userId && p.ChapterId == chapterId, cancellationToken);
        if (progress is null)
        {
            if (!isRead) return new(BooksResult.Success, chapterId);
            progress = new UserChapterProgress { UserId = userId, ChapterId = chapterId };
            context.UserChapterProgresses.Add(progress);
        }
        var now = DateTimeOffset.UtcNow;
        progress.ReadAt = isRead ? (progress.IsRead && progress.ReadAt.HasValue ? progress.ReadAt : now) : null;
        progress.IsRead = isRead; progress.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);
        return new(BooksResult.Success, chapterId);
    }, cancellationToken);

    public Task<BooksMutationResult> RecordOpenedAsync(int bookId, int chapterId, Guid userId,
        CancellationToken cancellationToken) => MutateAsync(async () =>
    {
        if (userId == Guid.Empty) return Invalid("UserId", "BooksInvalidUser");
        if (!await AvailableChapters(bookId).AnyAsync(c => c.Id == chapterId, cancellationToken)) return new(BooksResult.NotFound);
        var position = await context.BookReadingPositions.SingleOrDefaultAsync(p => p.UserId == userId && p.BookId == bookId, cancellationToken);
        if (position is null)
        {
            position = new BookReadingPosition { UserId = userId, BookId = bookId };
            context.BookReadingPositions.Add(position);
        }
        position.LastChapterId = chapterId; position.LastOpenedAt = DateTimeOffset.UtcNow;
        // Do not modify read marks, or rewrite hidden/deleted positions merely during a GET fallback.
        await context.SaveChangesAsync(cancellationToken);
        return new(BooksResult.Success, chapterId);
    }, cancellationToken);

    private Task<Book?> EditableBookAsync(int bookId, CancellationToken cancellationToken) =>
        context.Books.SingleOrDefaultAsync(b => b.Id == bookId && !b.IsDeleted, cancellationToken);

    private async Task<BooksMutationResult> MutateAsync(Func<Task<BooksMutationResult>> action, CancellationToken cancellationToken)
    {
        guard.EnsureTarget();
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var result = await action();
            if (result.Status == BooksResult.Success) await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException) { return new(BooksResult.Conflict); }
        catch (Exception exception) when (IsIntegrityConflict(exception)) { return new(BooksResult.Conflict); }
        // Failed transactions must not leave temporary orders or newly tracked entities in a reused scope.
        finally { context.ChangeTracker.Clear(); }
    }

    private static List<BooksError> Validate(object input)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), errors, true);
        return errors.SelectMany(e => e.MemberNames.DefaultIfEmpty(string.Empty)
            .Select(field => new BooksError(field, "BooksInvalidInput"))).ToList();
    }

    private static BooksMutationResult Invalid(string field, string code) => new(BooksResult.Invalid, Errors: [new(field, code)]);
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static bool Matches(byte[] current, string? encoded)
    {
        Span<byte> version = stackalloc byte[8];
        return encoded is { Length: 12 } && Convert.TryFromBase64String(encoded, version, out var count)
            && count == 8 && current.AsSpan().SequenceEqual(version);
    }
    private static void EnsureUser(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("BooksInvalidUser", nameof(userId));
    }
    private static bool IsIntegrityConflict(Exception exception)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            if (cause is SqlException { Number: 547 or 2601 or 2627 or 1205 }) return true;
        return false;
    }
}
