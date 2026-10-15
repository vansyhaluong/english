using English.Authorization;
using English.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.ActiveAccount)]
[Authorize(Roles = "Student,Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ListeningAudioController(IListeningService service) : Controller
{
    [HttpGet("/listening/audio/{key}")]
    [HttpHead("/listening/audio/{key}")]
    public async Task<IActionResult> Audio(string key, CancellationToken cancellationToken)
    {
        var stream = await service.OpenAudioAsync(key, cancellationToken);
        if (stream is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, "audio/mpeg", enableRangeProcessing: true);
    }
}
