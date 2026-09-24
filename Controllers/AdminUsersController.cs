using English.Authorization;
using English.Interfaces;
using English.Models;
using English.Models.ViewModels.AdminUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminUsersController(IUserService userService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        CancellationToken cancellationToken)
    {
        var users = await userService.GetUsersAsync(search, cancellationToken);
        return View(new AdminUsersIndexViewModel
        {
            Search = search?.Trim(),
            Users = users
                .Select(user => new AdminUserListItemViewModel(
                    user.Id,
                    user.FullName,
                    user.Email,
                    GetRoleName(user.Role),
                    user.IsActive))
                .ToArray()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await userService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return View(new AdminUserDetailsViewModel(
            user.Id,
            user.FullName,
            user.Email,
            GetRoleName(user.Role),
            user.IsActive,
            user.CreatedAt));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await userService.SetActiveAsync(id, false, cancellationToken))
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new
        {
            id,
            culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await userService.SetActiveAsync(id, true, cancellationToken))
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new
        {
            id,
            culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        });
    }

    private static string GetRoleName(byte role)
    {
        return role switch
        {
            (byte)UserRole.Student => nameof(UserRole.Student),
            (byte)UserRole.Admin => nameof(UserRole.Admin),
            _ => "Unknown"
        };
    }
}
