using English.Authorization;
using English.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.ActiveAccount)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BookCoversController(IBooksService service) : Controller
{
    [HttpGet("/books/covers/{storageKey}")]
    public async Task<IActionResult> Cover(string storageKey, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && !User.IsInRole("Student")) return Forbid();
        var cover = await service.GetCoverAsync(storageKey, isAdmin, cancellationToken);
        if (cover is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(cover.Content, cover.ContentType);
    }
}
