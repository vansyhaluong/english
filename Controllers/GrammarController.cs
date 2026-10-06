
using System.Globalization;
using System.Security.Claims;
using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.Grammar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.StudentOnly)]
public sealed class GrammarController(IGrammarService service, IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet("/grammar")]
    [HttpGet("/vi/grammar")]
    public async Task<IActionResult> Index(GrammarQuery query, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        return View("List", await service.ListAsync(query, false, userId, cancellationToken));
    }
    [HttpGet("/grammar/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        var item = await service.GetAsync(id, false, userId, cancellationToken);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost("/grammar/{id:int}/learned"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Learned(int id, GrammarProgressInputModel input, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        if (!ModelState.IsValid || !input.IsLearned.HasValue) return BadRequest();
        var result = await service.SetLearnedAsync(id, userId, input.IsLearned.Value, cancellationToken);
        if (result == GrammarResult.NotFound) return NotFound();
        if (result == GrammarResult.Invalid) return Forbid();
        TempData["GrammarMessage"] = localizer[result == GrammarResult.Success ? "ClassificationSaved" : "GrammarConflict"].Value;
        return RedirectToAction(nameof(Details), new { id, culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName });
    }
}
