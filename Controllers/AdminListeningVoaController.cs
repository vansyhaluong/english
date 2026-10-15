using English.Authorization;
using English.Models;
using English.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminListeningVoaController(ListeningVoaService service, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        return View(await service.PreviewAsync(cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        ListeningVoaReport report = await service.ImportAsync(cancellationToken);
        return View("Index", report);
    }
}
