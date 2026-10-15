using System.Globalization;
using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.Listening;
using English.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminListeningController(IListeningService service, IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(ListeningQuery query, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        return View(await service.ListAsync(query, cancellationToken));
    }
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : View(item);
    }
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) => View("Form",
        new ListeningFormViewModel(null, new(), await service.GetChoicesAsync(cancellationToken)));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([Bind(Prefix = "Input")] ListeningInputModel input, CancellationToken cancellationToken) => Save(null, input, cancellationToken);
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        if (item is null || item.IsDeleted) return NotFound();
        return View("Form", new ListeningFormViewModel(id, new()
        {
            Title = item.Title, Description = item.Description, LevelId = item.LevelId, TopicId = item.TopicId,
            AudioUrl = item.YouTubeVideoId is null ? string.Empty : item.AudioUrl,
            IsVisible = item.IsVisible, RowVersion = Convert.ToBase64String(item.RowVersion)
        }, await service.GetChoicesAsync(cancellationToken), item.AudioLocked, item.AudioUrl));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, [Bind(Prefix = "Input")] ListeningInputModel input, CancellationToken cancellationToken) => Save(id, input, cancellationToken);
    private async Task<IActionResult> Save(int? id, ListeningInputModel input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await service.SaveAsync(id, input, cancellationToken);
            if (result.Status == ListeningResult.NotFound) return NotFound();
            if (result.Status == ListeningResult.Success) return Saved();
            if (result.Errors is not null)
                foreach (var error in result.Errors) ModelState.AddModelError("Input." + error.Field, localizer[error.Resource]);
            else ModelState.AddModelError(string.Empty, localizer["ListeningConflict"]);
        }
        var item = id.HasValue ? await service.GetAsync(id.Value, cancellationToken) : null;
        if (id.HasValue && (item is null || item.IsDeleted)) return NotFound();
        return View("Form", new ListeningFormViewModel(id, input, await service.GetChoicesAsync(cancellationToken),
            item?.AudioLocked ?? false, item?.AudioUrl));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Visibility(int id, [Bind(Prefix = "Input")] ListeningVisibilityInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var result = await service.SetVisibilityAsync(id, input, cancellationToken);
        if (result == ListeningResult.NotFound) return NotFound();
        if (result == ListeningResult.Invalid) return BadRequest();
        if (result == ListeningResult.Success) return Saved();
        ModelState.AddModelError(string.Empty, localizer["ListeningConflict"]);
        var item = await service.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : View("Details", item);
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        if (item is null || item.IsDeleted) return NotFound();
        return View(new ListeningDeleteViewModel(item, new() { RowVersion = Convert.ToBase64String(item.RowVersion) }));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, [Bind(Prefix = "Input")] ListeningDeleteInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var result = await service.DeleteAsync(id, input.RowVersion, cancellationToken);
        if (result == ListeningResult.NotFound) return NotFound();
        if (result == ListeningResult.Success) return Saved();
        ModelState.AddModelError(string.Empty, localizer["ListeningConflict"]);
        var item = await service.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : View(new ListeningDeleteViewModel(item, input));
    }
    [HttpGet]
    public async Task<IActionResult> ImportSubtitle(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        if (item is null || item.IsDeleted) return NotFound();
        return View(new ListeningSubtitleViewModel(item, new()
        {
            RowVersion = Convert.ToBase64String(item.RowVersion), ExpectedRevision = item.TranscriptRevision
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(ListeningSubtitleLimits.MaxRequestBytes)]
    [RequestFormLimits(MemoryBufferThreshold = ListeningSubtitleLimits.MaxRequestBytes,
        MultipartBodyLengthLimit = ListeningSubtitleLimits.MaxRequestBytes)]
    public async Task<IActionResult> ImportSubtitle(int id, [Bind(Prefix = "Input")] ListeningSubtitleInputModel input, CancellationToken cancellationToken)
    {
        // The request limit is below the memory threshold: multipart uploads never spill to disk.
        var item = await service.GetAsync(id, cancellationToken);
        if (item is null || item.IsDeleted) return NotFound();
        if (ModelState.IsValid && input.File is not null)
        {
            await using var content = input.File.OpenReadStream();
            var result = await service.ImportSubtitleAsync(id, new(content, input.File.FileName, input.File.Length,
                input.RowVersion, input.ExpectedRevision), cancellationToken);
            if (result.Status == ListeningResult.NotFound) return NotFound();
            if (result.Status == ListeningResult.Success)
            {
                TempData["ListeningMessage"] = localizer[result.Unchanged ? "ListeningSubtitleUnchanged" : "ListeningSubtitleImported"].Value;
                return RedirectToAction(nameof(Index), new { culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName });
            }
            var error = result.Error ?? new SubtitleError("ListeningConflict");
            ModelState.AddModelError("Input.File", localizer[error.Resource, error.Line]);
        }
        return View(new ListeningSubtitleViewModel(item, input));
    }

    private IActionResult Saved()
    {
        TempData["ListeningMessage"] = localizer["ClassificationSaved"].Value;
        return RedirectToAction(nameof(Index), new { culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName });
    }
}
