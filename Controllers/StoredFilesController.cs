using System.Security.Claims;
using English.Authorization;
using English.Data;
using English.Interfaces;
using English.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.ActiveAccount)]
public sealed class StoredFilesController(
    ApplicationDbContext context,
    IFileStorage fileStorage) : Controller
{
    [HttpGet("/files/avatar/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Avatar(
        int id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                out var currentUserId))
        {
            return Forbid();
        }

        var file = await context.StoredFiles
            .AsNoTracking()
            .Where(storedFile =>
                storedFile.Id == id &&
                storedFile.Kind == (byte)StoredFileKind.Avatar)
            .Select(storedFile => new
            {
                storedFile.StorageKey,
                storedFile.ContentType,
                storedFile.UploadedByUserId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (file is null)
        {
            return NotFound();
        }

        if (file.UploadedByUserId != currentUserId ||
            !await context.AspNetUsers.AsNoTracking().AnyAsync(
                user => user.Id == currentUserId && user.AvatarFileId == id,
                cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (file.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            return NotFound();
        }

        Stream? content;
        try
        {
            content = await fileStorage.OpenReadAsync(file.StorageKey, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return NotFound();
        }

        if (content is null)
        {
            return NotFound();
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        return File(content, file.ContentType);
    }
}
