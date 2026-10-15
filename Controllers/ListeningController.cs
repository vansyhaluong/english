using System.Security.Claims;
using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.Listening;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.StudentOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ListeningController(IListeningService service) : Controller
{
    [HttpGet("/listening")]
    [HttpGet("/vi/listening")]
    public IActionResult Index() => View();

    [HttpGet("/listening/lessons")]
    public async Task<IActionResult> Lessons(StudentListeningQuery query, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        return Json(await service.ListStudentAsync(query, userId, cancellationToken));
    }

    [HttpGet("/listening/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty) return Forbid();
        if (!ModelState.IsValid || id <= 0) return BadRequest();
        var lesson = await service.GetStudentAsync(id, userId, cancellationToken);
        return lesson is null ? NotFound() : Json(lesson);
    }

    [HttpGet("/listening/antiforgery")]
    public IActionResult Antiforgery([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Json(new { tokens.RequestToken, tokens.HeaderName, tokens.FormFieldName });
    }

    [HttpPost("/listening/{id:int}/learned"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Learned(int id, ListeningProgressInputModel input, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty) return Forbid();
        if (!ModelState.IsValid || id <= 0 || !input.IsLearned.HasValue) return BadRequest();
        var result = await service.SetLearnedAsync(id, userId, input.IsLearned.Value, cancellationToken);
        return result switch
        {
            ListeningResult.Success => NoContent(),
            ListeningResult.NotFound => NotFound(),
            ListeningResult.Invalid => Forbid(),
            _ => Conflict()
        };
    }
}
