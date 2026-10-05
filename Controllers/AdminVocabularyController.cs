using System.Globalization;
using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.Vocabulary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminVocabularyController(IVocabularyService service, IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(VocabularyQuery query, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        return View("~/Views/Vocabulary/List.cshtml", await service.ListAsync(query, true, Guid.Empty, cancellationToken));
    }
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) => View("Form",
        new VocabularyFormViewModel(null, new() { Meanings = [new()] }, await service.GetChoicesAsync(cancellationToken)));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([Bind(Prefix = "Input")] VocabularyInputModel input, CancellationToken cancellationToken) => Save(null, input, cancellationToken);
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, true, Guid.Empty, cancellationToken);
        if (item is null || item.IsDeleted) return NotFound();
        return View("Form", new VocabularyFormViewModel(id, new()
        {
            Title = item.Title, Word = item.Word, Pronunciation = item.Pronunciation,
            PartOfSpeech = item.PartOfSpeech, Example = item.Example, Description = item.Description,
            LevelId = item.LevelId, TopicId = item.TopicId, ImageUrl = item.ImageUrl, AudioUrl = item.AudioUrl,
            IsVisible = item.IsVisible, RowVersion = Convert.ToBase64String(item.RowVersion),
            Meanings = item.Meanings.Select(m => new VocabularyMeaningInputModel { Id = m.Id, MeaningVi = m.MeaningVi }).ToList()
        }, await service.GetChoicesAsync(cancellationToken)));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, [Bind(Prefix = "Input")] VocabularyInputModel input, CancellationToken cancellationToken) => Save(id, input, cancellationToken);
    private async Task<IActionResult> Save(int? id, VocabularyInputModel input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await service.SaveAsync(id, input, cancellationToken);
            if (result.Status == VocabularyResult.NotFound) return NotFound();
            if (result.Status == VocabularyResult.Success) return Saved();
            if (result.Errors is not null)
                foreach (var error in result.Errors) ModelState.AddModelError("Input." + error.Field, localizer[error.Resource]);
            else ModelState.AddModelError(string.Empty, localizer["VocabConflict"]);
        }
        input.Meanings ??= [];
        return View("Form", new VocabularyFormViewModel(id, input, await service.GetChoicesAsync(cancellationToken)));
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, true, Guid.Empty, cancellationToken);
        if (item is null || item.IsDeleted) return NotFound();
        return View(new VocabularyDeleteViewModel(item, new() { RowVersion = Convert.ToBase64String(item.RowVersion) }));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, [Bind(Prefix = "Input")] VocabularyDeleteInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var result = await service.DeleteAsync(id, input.RowVersion, cancellationToken);
        if (result == VocabularyResult.NotFound) return NotFound();
        if (result == VocabularyResult.Success) return Saved();
        ModelState.AddModelError(string.Empty, localizer["VocabConflict"]);
        var item = await service.GetAsync(id, true, Guid.Empty, cancellationToken);
        if (item is null) return NotFound();
        return View(new VocabularyDeleteViewModel(item, input));
    }
    private IActionResult Saved()
    {
        TempData["VocabularyMessage"] = localizer["ClassificationSaved"].Value;
        return RedirectToAction(nameof(Index), new { culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName });
    }
}
