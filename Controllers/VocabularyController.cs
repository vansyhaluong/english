using System.Globalization;
using System.Security.Claims;
using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.Vocabulary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.StudentOnly)]
public sealed class VocabularyController(IVocabularyService service, IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet("/vocabulary")]
    [HttpGet("/vi/vocabulary")]
    public async Task<IActionResult> Index(VocabularyQuery query, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        return View("List", await service.ListAsync(query, false, userId, cancellationToken));
    }
    [HttpGet("/vocabulary/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        var item = await service.GetAsync(id, false, userId, cancellationToken);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost("/vocabulary/{id:int}/learned"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Learned(int id, VocabularyProgressInputModel input, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        if (!ModelState.IsValid || !input.IsLearned.HasValue) return BadRequest();
        var result = await service.SetLearnedAsync(id, userId, input.IsLearned.Value, cancellationToken);
        if (result == VocabularyResult.NotFound) return NotFound();
        if (result == VocabularyResult.Invalid) return Forbid();
        TempData["VocabularyMessage"] = localizer[result == VocabularyResult.Success ? "ClassificationSaved" : "VocabConflict"].Value;
        return RedirectToAction(nameof(Details), new { id, culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName });
    }
}
