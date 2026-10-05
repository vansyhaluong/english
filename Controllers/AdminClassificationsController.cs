using System.Globalization;
using English.Authorization;
using English.Interfaces;
using English.Models.ViewModels.AdminClassifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminClassificationsController(IClassificationService service, IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(ClassificationKind kind, CancellationToken cancellationToken)
    {
        if (!ValidKind(kind)) return BadRequest();
        return View(new ClassificationIndexViewModel(kind, await service.GetAllAsync(kind, cancellationToken)));
    }

    [HttpGet]
    public async Task<IActionResult> Levels(CancellationToken cancellationToken) =>
        View(new LevelsViewModel(await service.GetLevelsAsync(cancellationToken)));

    [HttpGet]
    public IActionResult Create(ClassificationKind kind)
    {
        if (!ValidKind(kind)) return BadRequest();
        return View("Form", new ClassificationFormViewModel(kind, null, new ClassificationInputModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassificationKind kind, [Bind(Prefix = "Input")] ClassificationInputModel input, CancellationToken cancellationToken)
    {
        if (!ValidKind(kind)) return BadRequest();
        if (ModelState.IsValid)
        {
            var result = await service.CreateAsync(kind, input.Name, input.Description, cancellationToken);
            if (result == ClassificationResult.Success) return Saved(kind);
            AddError(result);
        }
        return View("Form", new ClassificationFormViewModel(kind, null, input));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(ClassificationKind kind, int id, CancellationToken cancellationToken)
    {
        if (!ValidKind(kind) || id <= 0) return BadRequest();
        var item = await service.GetAsync(kind, id, cancellationToken);
        if (item is null) return NotFound();
        return View("Form", new ClassificationFormViewModel(kind, id, new ClassificationInputModel
        {
            Name = item.Name, Description = item.Description, RowVersion = Convert.ToBase64String(item.RowVersion)
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClassificationKind kind, int id, [Bind(Prefix = "Input")] ClassificationInputModel input, CancellationToken cancellationToken)
    {
        if (!ValidKind(kind) || id <= 0) return BadRequest();
        var rowVersion = ReadRowVersion(input.RowVersion);
        if (ModelState.IsValid)
        {
            var result = await service.EditAsync(kind, id, input.Name, input.Description, rowVersion!, cancellationToken);
            if (result == ClassificationResult.NotFound) return NotFound();
            if (result == ClassificationResult.Success) return Saved(kind);
            AddError(result);
        }
        if (await service.GetAsync(kind, id, cancellationToken) is null) return NotFound();
        return View("Form", new ClassificationFormViewModel(kind, id, input));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(ClassificationKind kind, int id, CancellationToken cancellationToken)
    {
        if (!ValidKind(kind) || id <= 0) return BadRequest();
        var item = await service.GetAsync(kind, id, cancellationToken);
        if (item is null) return NotFound();
        return View(new ClassificationDeleteViewModel(kind, item, new ClassificationDeleteInputModel
        {
            RowVersion = Convert.ToBase64String(item.RowVersion)
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ClassificationKind kind, int id, [Bind(Prefix = "Input")] ClassificationDeleteInputModel input, CancellationToken cancellationToken)
    {
        if (!ValidKind(kind) || id <= 0) return BadRequest();
        var rowVersion = ReadRowVersion(input.RowVersion);
        if (ModelState.IsValid)
        {
            var result = await service.DeleteAsync(kind, id, rowVersion!, cancellationToken);
            if (result == ClassificationResult.NotFound) return NotFound();
            if (result == ClassificationResult.Success) return Saved(kind);
            AddError(result);
        }
        var item = await service.GetAsync(kind, id, cancellationToken);
        if (item is null) return NotFound();
        return View(new ClassificationDeleteViewModel(kind, item, input));
    }

    private bool ValidKind(ClassificationKind kind) =>
        (!ModelState.TryGetValue("kind", out var state) || state.Errors.Count == 0) &&
        kind is ClassificationKind.Topic or ClassificationKind.GrammarGroup;

    private byte[]? ReadRowVersion(string? value)
    {
        if (value is not null && value.Length == 12)
        {
            Span<byte> bytes = stackalloc byte[8];
            if (Convert.TryFromBase64String(value, bytes, out var length) && length == 8) return bytes.ToArray();
        }
        ModelState.AddModelError(string.Empty, localizer["ClassificationInvalidVersion"]);
        return null;
    }

    private void AddError(ClassificationResult result) => ModelState.AddModelError(string.Empty, localizer[result switch
    {
        ClassificationResult.Conflict => "ClassificationConflict",
        ClassificationResult.Referenced => "ClassificationReferenced",
        _ => "ClassificationInvalidInput"
    }]);

    private IActionResult Saved(ClassificationKind kind)
    {
        TempData["ClassificationMessage"] = localizer["ClassificationSaved"].Value;
        return RedirectToAction(nameof(Index), new { kind, culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName });
    }
}
