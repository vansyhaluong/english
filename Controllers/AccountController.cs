using System.Security.Claims;
using System.Globalization;
using English.Authorization;
using English.Interfaces;
using English.Models;
using English.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace English.Controllers;

public sealed class AccountController(
    IAuthService authService,
    IUserService userService,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet]
    public IActionResult Register()
    {
        return User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Home")
            : View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var registered = await authService.RegisterAsync(
            model.FullName,
            model.Email,
            model.Password,
            cancellationToken);

        if (!registered)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            return View(model);
        }

        TempData["AccountMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login), CultureRoute());
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Home")
            : View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await authService.AuthenticateAsync(
            model.Email,
            model.Password,
            cancellationToken);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không hợp lệ.");
            return View(model);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl)
            : user.Role == UserRole.Admin
                ? RedirectToAction("Index", "AdminUsers", CultureRoute())
                : RedirectToAction(nameof(Profile), CultureRoute());
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ActiveAccount)]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var model = await CreateProfileViewModelAsync(userId, cancellationToken);
        return model is null ? Forbid() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ActiveAccount)]
    public async Task<IActionResult> Profile(
        UpdateProfileViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await CreateProfileViewModelAsync(userId, cancellationToken);
            return invalidViewModel is null ? Forbid() : View(invalidViewModel);
        }

        if (!await userService.UpdateProfileAsync(
                userId,
                model.FullName,
                cancellationToken))
        {
            return Forbid();
        }

        TempData["AccountMessage"] = localizer["ProfileUpdated"].Value;
        return RedirectToAction(nameof(Profile), CultureRoute());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ActiveAccount)]
    public async Task<IActionResult> UploadAvatar(
        AvatarUploadInputModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        if (model.Avatar is null || model.Avatar.Length == 0)
        {
            TempData["AvatarError"] = localizer["AvatarRequired"].Value;
            return RedirectToAction(nameof(Profile), CultureRoute());
        }

        if (model.Avatar.Length > AvatarUploadLimits.MaxSizeBytes)
        {
            TempData["AvatarError"] = localizer["AvatarTooLarge"].Value;
            return RedirectToAction(nameof(Profile), CultureRoute());
        }

        await using var content = model.Avatar.OpenReadStream();
        var result = await userService.UpdateAvatarAsync(
            userId,
            new AvatarUpload(content, model.Avatar.Length, model.Avatar.FileName),
            cancellationToken);

        if (result == AvatarUploadResult.UserNotFound)
        {
            return Forbid();
        }

        TempData[result == AvatarUploadResult.Success ? "AccountMessage" : "AvatarError"] =
            localizer[result switch
            {
                AvatarUploadResult.Success => "AvatarUpdated",
                AvatarUploadResult.InvalidSize => "AvatarTooLarge",
                AvatarUploadResult.InvalidContent => "AvatarInvalidContent",
                _ => "AvatarStorageFailed"
            }].Value;

        return RedirectToAction(nameof(Profile), CultureRoute());
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ActiveAccount)]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ActiveAccount)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await userService.ChangePasswordAsync(
            userId,
            model.CurrentPassword,
            model.NewPassword,
            cancellationToken);

        if (result == ChangePasswordResult.UserNotFound)
        {
            return Forbid();
        }

        if (result == ChangePasswordResult.InvalidCurrentPassword)
        {
            ModelState.AddModelError(
                nameof(model.CurrentPassword),
                localizer["CurrentPasswordIncorrect"]);
            return View(model);
        }

        TempData["AccountMessage"] = localizer["PasswordUpdated"].Value;
        return RedirectToAction(nameof(ChangePassword), CultureRoute());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home", CultureRoute());
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out userId);
    }

    private async Task<ProfileViewModel?> CreateProfileViewModelAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await userService.GetProfileAsync(userId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        return new ProfileViewModel
        {
            FullName = profile.FullName,
            Email = profile.Email,
            Role = profile.Role switch
            {
                (byte)UserRole.Student => nameof(UserRole.Student),
                (byte)UserRole.Admin => nameof(UserRole.Admin),
                _ => "Unknown"
            },
            AvatarFileId = profile.AvatarFileId
        };
    }

    private static object CultureRoute()
    {
        return new
        {
            culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        };
    }
}
