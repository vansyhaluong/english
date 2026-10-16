using System.Globalization;
using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace English.Controllers;

// Razor/form adapters over the existing service; the AdminBooks JSON contract is unchanged.
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("/AdminBooks/manage")]
public sealed class AdminBooksManageController(IBooksService service, IStringLocalizer<SharedResource> localizer) : Controller
{
    private const string ViewRoot = "~/Views/AdminBooks/";
    private string Culture => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    [HttpGet]
    public async Task<IActionResult> Index(AdminBooksQuery query, CancellationToken cancellationToken)
    {
        LocalizeModelErrors();
        var books = ModelState.IsValid ? await service.ListAdminAsync(query, cancellationToken)
            : new BooksPage<AdminBookDto>([], 0, 1, 1, 20);
        return View(ViewRoot + "Index.cshtml", new AdminBooksIndexPage(query, books, await service.GetTopicsAsync(cancellationToken)));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(ViewRoot + "BookForm.cshtml", new AdminBookFormPage(null, new(), await service.GetTopicsAsync(cancellationToken)));

    [HttpGet("{bookId:int}/edit")]
    public async Task<IActionResult> Edit(int bookId, CancellationToken cancellationToken)
    {
        var details = await service.GetAdminAsync(bookId, cancellationToken);
        if (details is null || details.Book.IsDeleted) return NotFound();
        var book = details.Book;
        return View(ViewRoot + "BookForm.cshtml", new AdminBookFormPage(bookId, new()
        {
            Title = book.Title, Author = book.Author, TopicId = book.TopicId, Description = book.Description,
            IsVisible = book.IsVisible, RowVersion = Convert.ToBase64String(book.RowVersion)
        }, await service.GetTopicsAsync(cancellationToken), book.CoverUrl));
    }

    [HttpPost("create"), ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public Task<IActionResult> Create([Bind(Prefix = "Input")] AdminBookUploadInput input, CancellationToken cancellationToken) =>
        SaveBook(null, input, cancellationToken);

    [HttpPost("{bookId:int}/edit"), ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public Task<IActionResult> Edit(int bookId, [Bind(Prefix = "Input")] AdminBookUploadInput input, CancellationToken cancellationToken) =>
        SaveBook(bookId, input, cancellationToken);

    private async Task<IActionResult> SaveBook(int? bookId, AdminBookUploadInput input, CancellationToken cancellationToken)
    {
        LocalizeModelErrors();
        if (input.CoverImage is { } image && (image.Length <= 0 || image.Length > 2 * 1024 * 1024))
            ModelState.AddModelError("Input.CoverImage", localizer["BooksCoverTooLarge"]);
        if (ModelState.IsValid)
        {
            await using var content = input.CoverImage?.OpenReadStream();
            var upload = content is null ? null : new BookCoverUpload(content, input.CoverImage!.Length);
            var result = await service.SaveBookWithCoverAsync(bookId, input.ToBookInput(), upload, cancellationToken);
            if (result.Status == BooksResult.NotFound) return NotFound();
            if (result.Status == BooksResult.Success)
            {
                TempData["BooksSuccess"] = localizer["BooksSaved"].Value;
                return RedirectToAction(nameof(Chapters), new { bookId = result.Id, culture = Culture });
            }
            AddErrors(result);
        }
        var current = bookId.HasValue ? await service.GetAdminAsync(bookId.Value, cancellationToken) : null;
        return View(ViewRoot + "BookForm.cshtml", new AdminBookFormPage(bookId, input,
            await service.GetTopicsAsync(cancellationToken), current?.Book.CoverUrl));
    }

    [HttpGet("{bookId:int}/chapters")]
    public async Task<IActionResult> Chapters(int bookId, CancellationToken cancellationToken)
    {
        var book = await service.GetAdminAsync(bookId, cancellationToken);
        return book is null ? NotFound() : View(ViewRoot + "Chapters.cshtml", new AdminChaptersPage(book.Book, book.Chapters));
    }

    [HttpGet("{bookId:int}/chapters/create")]
    public async Task<IActionResult> CreateChapter(int bookId, CancellationToken cancellationToken)
    {
        var details = await service.GetAdminAsync(bookId, cancellationToken);
        if (details is null || details.Book.IsDeleted) return NotFound();
        // Suggest the first free positive order, including reservations by soft-deleted chapters.
        var used = details.Chapters.Select(c => c.DisplayOrder).ToHashSet();
        var order = 1;
        while (order < int.MaxValue && used.Contains(order)) order++;
        return View(ViewRoot + "ChapterForm.cshtml", new AdminChapterFormPage(bookId, details.Book.Title, null, new() { DisplayOrder = order }));
    }

    [HttpGet("{bookId:int}/chapters/{chapterId:int}/edit")]
    public async Task<IActionResult> EditChapter(int bookId, int chapterId, CancellationToken cancellationToken)
    {
        var book = await service.GetAdminAsync(bookId, cancellationToken);
        if (book is null || book.Book.IsDeleted) return NotFound();
        var chapter = await service.GetAdminChapterAsync(bookId, chapterId, cancellationToken);
        if (chapter is null || chapter.IsDeleted) return NotFound();
        return View(ViewRoot + "ChapterForm.cshtml", new AdminChapterFormPage(bookId, book.Book.Title, chapterId, new()
        {
            Title = chapter.Title, DisplayOrder = chapter.DisplayOrder, ContentEn = chapter.ContentEn,
            TranslationVi = chapter.TranslationVi, VocabularyNotes = chapter.VocabularyNotes,
            IsVisible = chapter.IsVisible, RowVersion = Convert.ToBase64String(chapter.RowVersion)
        }));
    }

    [HttpGet("{bookId:int}/chapters/{chapterId:int}/vocabulary")]
    public async Task<IActionResult> Vocabulary(int bookId, int chapterId, CancellationToken cancellationToken)
    {
        var book = await service.GetAdminAsync(bookId, cancellationToken);
        var chapter = await service.GetAdminChapterAsync(bookId, chapterId, cancellationToken);
        if (book is null || book.Book.IsDeleted || chapter is null || chapter.IsDeleted) return NotFound();
        var notes = BookVocabularyNotes.Read(chapter.VocabularyNotes);
        return View(ViewRoot + "Vocabulary.cshtml", new AdminChapterVocabularyPage(bookId, chapterId,
            book.Book.Title, chapter.Title, new() { Entries = notes.Entries, RowVersion = Convert.ToBase64String(chapter.RowVersion) }, notes.LegacyText));
    }

    [HttpPost("{bookId:int}/chapters/{chapterId:int}/vocabulary"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Vocabulary(int bookId, int chapterId, [Bind(Prefix = "Input")] ChapterVocabularyInput input,
        CancellationToken cancellationToken)
    {
        var book = await service.GetAdminAsync(bookId, cancellationToken);
        var chapter = await service.GetAdminChapterAsync(bookId, chapterId, cancellationToken);
        if (book is null || book.Book.IsDeleted || chapter is null || chapter.IsDeleted) return NotFound();
        if (ModelState.IsValid)
        {
            var result = await service.SaveVocabularyNotesAsync(bookId, chapterId, input, cancellationToken);
            if (result.Status == BooksResult.NotFound) return NotFound();
            if (result.Status == BooksResult.Success)
            {
                TempData["BooksSuccess"] = localizer["BooksSaved"].Value;
                return RedirectToAction(nameof(Vocabulary), new { bookId, chapterId, culture = Culture });
            }
            AddErrors(result);
        }
        return View(ViewRoot + "Vocabulary.cshtml", new AdminChapterVocabularyPage(bookId, chapterId,
            book.Book.Title, chapter.Title, input, BookVocabularyNotes.Read(chapter.VocabularyNotes).LegacyText));
    }

    [HttpPost("{bookId:int}/chapters/create"), ValidateAntiForgeryToken]
    public Task<IActionResult> CreateChapter(int bookId, [Bind(Prefix = "Input")] ChapterInput input, CancellationToken cancellationToken) =>
        SaveChapter(bookId, null, input, cancellationToken);

    [HttpPost("{bookId:int}/chapters/{chapterId:int}/edit"), ValidateAntiForgeryToken]
    public Task<IActionResult> EditChapter(int bookId, int chapterId, [Bind(Prefix = "Input")] ChapterInput input, CancellationToken cancellationToken) =>
        SaveChapter(bookId, chapterId, input, cancellationToken);

    private async Task<IActionResult> SaveChapter(int bookId, int? chapterId, ChapterInput input, CancellationToken cancellationToken)
    {
        var book = await service.GetAdminAsync(bookId, cancellationToken);
        if (book is null || book.Book.IsDeleted) return NotFound();
        LocalizeModelErrors();
        if (ModelState.IsValid)
        {
            var result = await service.SaveChapterAsync(bookId, chapterId, input, cancellationToken);
            if (result.Status == BooksResult.NotFound) return NotFound();
            if (result.Status == BooksResult.Success) return Success(bookId);
            AddErrors(result);
        }
        return View(ViewRoot + "ChapterForm.cshtml", new AdminChapterFormPage(bookId, book.Book.Title, chapterId, input));
    }

    [HttpPost("{bookId:int}/visibility"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Visibility(int bookId, [Bind(Prefix = "Input")] BookVisibilityInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Failure(nameof(Index), null, "BooksInvalidInput");
        var result = await service.SetBookVisibilityAsync(bookId, input, cancellationToken);
        if (result.Status == BooksResult.NotFound) return NotFound();
        if (result.Status != BooksResult.Success) return Failure(nameof(Index), null, ErrorCode(result));
        TempData["BooksSuccess"] = localizer["BooksSaved"].Value;
        return RedirectToAction(nameof(Index), new { culture = Culture });
    }

    [HttpPost("{bookId:int}/chapters/{chapterId:int}/visibility"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ChapterVisibility(int bookId, int chapterId, [Bind(Prefix = "Input")] BookVisibilityInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Failure(nameof(Chapters), bookId, "BooksInvalidInput");
        var result = await service.SetChapterVisibilityAsync(bookId, chapterId, input, cancellationToken);
        if (result.Status == BooksResult.NotFound) return NotFound();
        return result.Status == BooksResult.Success ? Success(bookId) : Failure(nameof(Chapters), bookId, ErrorCode(result));
    }

    [HttpPost("{bookId:int}/chapters/order"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(int bookId, [Bind(Prefix = "Input")] ChaptersOrderInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Failure(nameof(Chapters), bookId, "BooksOrderInvalid");
        var result = await service.ReorderChaptersAsync(bookId, input, cancellationToken);
        if (result.Status == BooksResult.NotFound) return NotFound();
        // Reload current IDs/order/versions after a conflict; never silently retry stale reorders.
        return result.Status == BooksResult.Success ? Success(bookId) : Failure(nameof(Chapters), bookId, ErrorCode(result));
    }

    [HttpGet("{bookId:int}/delete")]
    public Task<IActionResult> Delete(int bookId, CancellationToken cancellationToken) => ConfirmDelete(bookId, null, cancellationToken);

    [HttpGet("{bookId:int}/chapters/{chapterId:int}/delete")]
    public Task<IActionResult> DeleteChapter(int bookId, int chapterId, CancellationToken cancellationToken) => ConfirmDelete(bookId, chapterId, cancellationToken);

    private async Task<IActionResult> ConfirmDelete(int bookId, int? chapterId, CancellationToken cancellationToken)
    {
        var details = await service.GetAdminAsync(bookId, cancellationToken);
        if (details is null || details.Book.IsDeleted) return NotFound();
        var chapter = chapterId.HasValue ? details.Chapters.SingleOrDefault(c => c.Id == chapterId) : null;
        if (chapterId.HasValue && (chapter is null || chapter.IsDeleted)) return NotFound();
        return View(ViewRoot + "Delete.cshtml", new AdminBookDeletePage(bookId, chapterId, chapter?.Title ?? details.Book.Title,
            new() { RowVersion = Convert.ToBase64String(chapter?.RowVersion ?? details.Book.RowVersion) }));
    }

    [HttpPost("{bookId:int}/delete"), ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(int bookId, [Bind(Prefix = "Input")] BookVersionInput input, CancellationToken cancellationToken) =>
        ApplyDelete(bookId, null, input, cancellationToken);

    [HttpPost("{bookId:int}/chapters/{chapterId:int}/delete"), ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteChapter(int bookId, int chapterId, [Bind(Prefix = "Input")] BookVersionInput input, CancellationToken cancellationToken) =>
        ApplyDelete(bookId, chapterId, input, cancellationToken);

    private async Task<IActionResult> ApplyDelete(int bookId, int? chapterId, BookVersionInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Failure(chapterId.HasValue ? nameof(Chapters) : nameof(Index), chapterId.HasValue ? bookId : null, "BooksConflict");
        var result = chapterId.HasValue ? await service.DeleteChapterAsync(bookId, chapterId.Value, input, cancellationToken)
            : await service.DeleteBookAsync(bookId, input, cancellationToken);
        if (result.Status == BooksResult.NotFound) return NotFound();
        if (result.Status != BooksResult.Success) return Failure(chapterId.HasValue ? nameof(Chapters) : nameof(Index), chapterId.HasValue ? bookId : null, ErrorCode(result));
        TempData["BooksSuccess"] = localizer["BooksDeleted"].Value;
        return RedirectToAction(chapterId.HasValue ? nameof(Chapters) : nameof(Index), new { bookId = chapterId.HasValue ? (int?)bookId : null, culture = Culture });
    }

    private IActionResult Success(int bookId)
    {
        TempData["BooksSuccess"] = localizer["BooksSaved"].Value;
        return RedirectToAction(nameof(Chapters), new { bookId, culture = Culture });
    }
    private IActionResult Failure(string action, int? bookId, string code)
    {
        TempData["BooksError"] = localizer[code].Value;
        return RedirectToAction(action, new { bookId, culture = Culture });
    }
    private static string ErrorCode(BooksMutationResult result) => result.Status == BooksResult.Conflict
        ? "BooksConflict" : result.Errors?.FirstOrDefault()?.Code ?? "BooksInvalidInput";
    private void AddErrors(BooksMutationResult result)
    {
        if (result.Status == BooksResult.Conflict) ModelState.AddModelError(string.Empty, localizer["BooksConflict"]);
        else if (result.Errors is { Count: > 0 })
            foreach (var error in result.Errors) ModelState.AddModelError("Input." + error.Field, localizer[error.Code]);
        else ModelState.AddModelError(string.Empty, localizer["BooksInvalidInput"]);
    }
    private void LocalizeModelErrors()
    {
        foreach (var entry in ModelState.Values.Where(v => v.Errors.Count != 0))
        {
            entry.Errors.Clear();
            entry.Errors.Add(localizer["BooksFieldInvalid"].Value);
        }
    }
}
