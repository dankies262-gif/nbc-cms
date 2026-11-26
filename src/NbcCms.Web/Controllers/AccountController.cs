using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data;
using NbcCms.Web.Data.Entities;
using NbcCms.Web.Services;
using NbcCms.Web.ViewModels;
using SecurityClaim = System.Security.Claims.Claim;

namespace NbcCms.Web.Controllers;

public class AccountController(CmsDbContext db, IPasswordHasher hasher) : AppController
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == model.Username.Trim());

        if (user is null || !user.IsActive || !hasher.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password, or this account is inactive.");
            await LogAuditAsync(db, "Users", user?.UserId, "LoginFailed", null);
            return View(model);
        }

        var claims = new List<SecurityClaim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.RoleName),
            new("FullName", user.FullName),
            new("RoleLabel", user.Role.RoleName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 24 * 14 : 8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        await LogAuditAsync(db, "Users", user.UserId, "Login", user.UserId);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await db.Users
            .Include(u => u.Role)
            .Include(u => u.BankingDetail)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user is null)
        {
            return NotFound();
        }

        var model = new ProfileViewModel { User = user };

        if (user.Role.RoleName == RoleNames.Artist)
        {
            var statuses = await db.Claims
                .Where(c => c.ClaimantId == user.UserId)
                .Select(c => c.Status.StatusName)
                .ToListAsync();

            model.ClaimCount = statuses.Count;
            model.PendingCount = statuses.Count(s => ClaimStatusNames.Open.Contains(s));
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            FlashError("Please correct the password fields and try again.");
            return RedirectToAction(nameof(Profile));
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null)
        {
            return NotFound();
        }

        if (!hasher.Verify(model.CurrentPassword, user.PasswordHash))
        {
            FlashError("Your current password is incorrect.");
            return RedirectToAction(nameof(Profile));
        }

        user.PasswordHash = hasher.Hash(model.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = user.UserId;
        await db.SaveChangesAsync();
        await LogAuditAsync(db, "Users", user.UserId, "PasswordChange");

        FlashSuccess("Your password has been updated.");
        return RedirectToAction(nameof(Profile));
    }
}
