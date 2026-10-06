using System.Globalization;
using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.Grammar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminGrammarController(IGrammarService service, IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(GrammarQuery query, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        return View("~/Views/Grammar/List.cshtml", await service.ListAsync(query, true, Guid.Empty, cancellationToken));
    }
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) => View("Form",
        new GrammarFormViewModel(null, new(), await service.GetChoicesAsync(cancellationToken)));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([Bind(Prefix = "Input")] GrammarInputModel input, CancellationToken cancellationToken) => Save(null, input, cancellationToken);
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, true, Guid.Empty, cancellationToken);
        if (item is null || item.IsDeleted) return NotFound();
        return View("Form", new GrammarFormViewModel(id, new()
        {
            Title = item.Title, Formula = item.Formula, Usage = item.Usage, Examples = item.Examples, Notes = item.Notes,
            LevelId = item.LevelId, GrammarGroupId = item.GrammarGroupId, IsVisible = item.IsVisible,
            RowVersion = Convert.ToBase64String(item.RowVersion)
        }, await service.GetChoicesAsync(cancellationToken)));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, [Bind(Prefix = "Input")] GrammarInputModel input, CancellationToken cancellationToken) => Save(id, input, cancellationToken);
    private async Task<IActionResult> Save(int? id, GrammarInputModel input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await service.SaveAsync(id, input, cancellationToken);
            if (result.Status == GrammarResult.NotFound) return NotFound();
            if (result.Status == GrammarResult.Success) return Saved();
            if (result.Errors is not null)
                foreach (var error in result.Errors) ModelState.AddModelError("Input." + error.Field, localizer[error.Resource]);
            else ModelState.AddModelError(string.Empty, localizer["GrammarConflict"]);
        }
        return View("Form", new GrammarFormViewModel(id, input, await service.GetChoicesAsync(cancellationToken)));
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, true, Guid.Empty, cancellationToken);
        if (item is null || item.IsDeleted) return NotFound();
        return View(new GrammarDeleteViewModel(item, new() { RowVersion = Convert.ToBase64String(item.RowVersion) }));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, [Bind(Prefix = "Input")] GrammarDeleteInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var result = await service.DeleteAsync(id, input.RowVersion, cancellationToken);
        if (result == GrammarResult.NotFound) return NotFound();
        if (result == GrammarResult.Success) return Saved();
        ModelState.AddModelError(string.Empty, localizer["GrammarConflict"]);
        var item = await service.GetAsync(id, true, Guid.Empty, cancellationToken);
        return item is null ? NotFound() : View(new GrammarDeleteViewModel(item, input));
    }
    private IActionResult Saved()
    {
        TempData["GrammarMessage"] = localizer["ClassificationSaved"].Value;
        return RedirectToAction(nameof(Index), new { culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName });
    }
}
