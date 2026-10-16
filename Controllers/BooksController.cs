using System.Security.Claims;
using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.Books;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.StudentOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("/books")]
public sealed class BooksController(IBooksService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(BooksQuery query, CancellationToken cancellationToken)
    {
        if (!TryUser(out var userId)) return Forbid();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Json(await service.ListStudentAsync(query, userId, cancellationToken));
    }

    [HttpGet("topics")]
    public async Task<IActionResult> Topics(CancellationToken cancellationToken) =>
        Json(await service.GetTopicsAsync(cancellationToken));

    [HttpGet("{bookId:int}")]
    public async Task<IActionResult> Details(int bookId, CancellationToken cancellationToken)
    {
        if (!TryUser(out var userId)) return Forbid();
        if (bookId <= 0) return BadRequest();
        var book = await service.GetStudentAsync(bookId, userId, cancellationToken);
        return book is null ? NotFound() : Json(book);
    }

    [HttpGet("{bookId:int}/chapters/{chapterId:int}")]
    public async Task<IActionResult> Chapter(int bookId, int chapterId, CancellationToken cancellationToken)
    {
        if (!TryUser(out var userId)) return Forbid();
        if (bookId <= 0 || chapterId <= 0) return BadRequest();
        var chapter = await service.GetChapterAsync(bookId, chapterId, userId, cancellationToken);
        return chapter is null ? NotFound() : Json(chapter);
    }

    [HttpGet("antiforgery")]
    public IActionResult Antiforgery([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Json(new { tokens.RequestToken, tokens.HeaderName, tokens.FormFieldName });
    }

    [HttpPost("{bookId:int}/chapters/{chapterId:int}/read"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(int bookId, int chapterId, [FromBody] ChapterReadInput input, CancellationToken cancellationToken)
    {
        if (!TryUser(out var userId)) return Forbid();
        if (bookId <= 0 || chapterId <= 0) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!input.IsRead.HasValue) return BadRequest();
        return Respond(await service.SetReadAsync(bookId, chapterId, userId, input.IsRead.Value, cancellationToken));
    }

    [HttpPost("{bookId:int}/chapters/{chapterId:int}/opened"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Opened(int bookId, int chapterId, CancellationToken cancellationToken)
    {
        if (!TryUser(out var userId)) return Forbid();
        if (bookId <= 0 || chapterId <= 0) return BadRequest();
        return Respond(await service.RecordOpenedAsync(bookId, chapterId, userId, cancellationToken));
    }

    private bool TryUser(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId != Guid.Empty;

    private IActionResult Respond(BooksMutationResult result) => result.Status switch
    {
        BooksResult.Success => NoContent(),
        BooksResult.NotFound => NotFound(),
        BooksResult.Conflict => Conflict(new { code = "BooksConflict" }),
        _ => BadRequest(new { result.Errors })
    };
}
