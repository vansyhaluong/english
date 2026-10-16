using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.Books;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

// Backend JSON endpoints only; Admin views are a separate implementation step.
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("/AdminBooks")]
public sealed class AdminBooksController(IBooksService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(AdminBooksQuery query, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Json(await service.ListAdminAsync(query, cancellationToken));
    }

    [HttpGet("topics")]
    public async Task<IActionResult> Topics(CancellationToken cancellationToken) =>
        Json(await service.GetTopicsAsync(cancellationToken));

    [HttpGet("{bookId:int}")]
    public async Task<IActionResult> Details(int bookId, CancellationToken cancellationToken)
    {
        if (bookId <= 0) return BadRequest();
        var book = await service.GetAdminAsync(bookId, cancellationToken);
        return book is null ? NotFound() : Json(book);
    }

    [HttpGet("antiforgery")]
    public IActionResult Antiforgery([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Json(new { tokens.RequestToken, tokens.HeaderName, tokens.FormFieldName });
    }

    [HttpGet("{bookId:int}/chapters/{chapterId:int}")]
    public async Task<IActionResult> Chapter(int bookId, int chapterId, CancellationToken cancellationToken)
    {
        if (bookId <= 0 || chapterId <= 0) return BadRequest();
        var chapter = await service.GetAdminChapterAsync(bookId, chapterId, cancellationToken);
        return chapter is null ? NotFound() : Json(chapter);
    }

    // JSON mutations use the antiforgery header obtained from GET antiforgery, plus the session cookie.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] BookInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Respond(await service.SaveBookAsync(null, input, cancellationToken));
    }

    [HttpPost("{bookId:int}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int bookId, [FromBody] BookInput input, CancellationToken cancellationToken)
    {
        if (bookId <= 0) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Respond(await service.SaveBookAsync(bookId, input, cancellationToken));
    }

    [HttpPost("{bookId:int}/visibility"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Visibility(int bookId, [FromBody] BookVisibilityInput input, CancellationToken cancellationToken)
    {
        if (bookId <= 0) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Respond(await service.SetBookVisibilityAsync(bookId, input, cancellationToken));
    }

    [HttpPost("{bookId:int}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int bookId, [FromBody] BookVersionInput input, CancellationToken cancellationToken)
    {
        if (bookId <= 0) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Respond(await service.DeleteBookAsync(bookId, input, cancellationToken));
    }

    [HttpPost("{bookId:int}/chapters"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateChapter(int bookId, [FromBody] ChapterInput input, CancellationToken cancellationToken)
    {
        if (bookId <= 0) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Respond(await service.SaveChapterAsync(bookId, null, input, cancellationToken));
    }

    [HttpPost("{bookId:int}/chapters/{chapterId:int}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditChapter(int bookId, int chapterId, [FromBody] ChapterInput input, CancellationToken cancellationToken)
    {
        if (bookId <= 0 || chapterId <= 0) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Respond(await service.SaveChapterAsync(bookId, chapterId, input, cancellationToken));
    }

    [HttpPost("{bookId:int}/chapters/{chapterId:int}/visibility"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ChapterVisibility(int bookId, int chapterId, [FromBody] BookVisibilityInput input, CancellationToken cancellationToken)
    {
        if (bookId <= 0 || chapterId <= 0) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Respond(await service.SetChapterVisibilityAsync(bookId, chapterId, input, cancellationToken));
    }

    [HttpPost("{bookId:int}/chapters/{chapterId:int}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChapter(int bookId, int chapterId, [FromBody] BookVersionInput input, CancellationToken cancellationToken)
    {
        if (bookId <= 0 || chapterId <= 0) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Respond(await service.DeleteChapterAsync(bookId, chapterId, input, cancellationToken));
    }

    [HttpPost("{bookId:int}/chapters/order"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(int bookId, [FromBody] ChaptersOrderInput input, CancellationToken cancellationToken)
    {
        if (bookId <= 0) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Respond(await service.ReorderChaptersAsync(bookId, input, cancellationToken));
    }

    private IActionResult Respond(BooksMutationResult result) => result.Status switch
    {
        BooksResult.Success => Ok(new { result.Id, result.RowVersion }),
        BooksResult.NotFound => NotFound(),
        BooksResult.Conflict => Conflict(new { code = "BooksConflict" }),
        _ => BadRequest(new { result.Errors })
    };
}
