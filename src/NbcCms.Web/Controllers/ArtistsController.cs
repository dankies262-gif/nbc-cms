using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data;
using NbcCms.Web.Data.Entities;
using NbcCms.Web.Services;
using NbcCms.Web.ViewModels;

namespace NbcCms.Web.Controllers;

[Authorize(Policy = "Staff")]
public class ArtistsController(CmsDbContext db, IPasswordHasher hasher) : AppController
{
    public async Task<IActionResult> Index(string? search)
    {
        var artists = await db.Users
            .Include(u => u.Role)
            .Include(u => u.BankingDetail)
            .Where(u => u.Role.RoleName == RoleNames.Artist)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var stats = await db.Claims
            .GroupBy(c => c.ClaimantId)
            .Select(g => new
            {
                ClaimantId = g.Key,
                Count = g.Count(),
                Total = g.Sum(c => c.FeesAmount ?? 0),
                LastClaimAt = g.Max(c => (DateTime?)c.CreatedAt)
            })
            .ToListAsync();

        var byArtist = stats.ToDictionary(s => s.ClaimantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            artists = artists
                .Where(u => u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                            || u.Email.Contains(term, StringComparison.OrdinalIgnoreCase)
                            || u.Username.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var model = artists.Select(u =>
        {
            byArtist.TryGetValue(u.UserId, out var stat);
            return new ArtistListItemViewModel
            {
                User = u,
                ClaimCount = stat?.Count ?? 0,
                TotalClaimed = stat?.Total ?? 0,
                LastClaimAt = stat?.LastClaimAt
            };
        }).ToList();

        ViewData["Search"] = search;
        return View(model);
    }

    [HttpGet]
    public IActionResult New() => View("Form", new ArtistFormViewModel());

    [HttpPost]
    public async Task<IActionResult> New(ArtistFormViewModel model)
    {
        model.UserId = null;
        if (!await ValidateArtistAsync(model, isNew: true))
        {
            return View("Form", model);
        }

        var artistRoleId = await db.UserRoles
            .Where(r => r.RoleName == RoleNames.Artist)
            .Select(r => r.RoleId)
            .FirstAsync();

        var user = new User
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Username = model.Username.Trim(),
            Email = model.Email.Trim(),
            ContactNumber = model.ContactNumber?.Trim(),
            IdNumber = model.IdNumber?.Trim(),
            Address = model.Address?.Trim(),
            RoleId = artistRoleId,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserId,
            PasswordHash = hasher.Hash(model.Password!)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        if (HasBanking(model))
        {
            db.ArtistBankingDetails.Add(new ArtistBankingDetail
            {
                ArtistId = user.UserId,
                Bank = model.Bank!.Trim(),
                Branch = model.Branch!.Trim(),
                BranchCode = model.BranchCode!.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId,
                UpdatedBy = CurrentUserId
            });
            await db.SaveChangesAsync();
        }

        await LogAuditAsync(db, "Users", user.UserId, "UserCreate");
        FlashSuccess($"Artist account for {user.FullName} was created.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .Include(u => u.BankingDetail)
            .FirstOrDefaultAsync(u => u.UserId == id && u.Role.RoleName == RoleNames.Artist);

        if (user is null)
        {
            return NotFound();
        }

        return View("Form", new ArtistFormViewModel
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            ContactNumber = user.ContactNumber,
            IdNumber = user.IdNumber,
            Address = user.Address,
            IsActive = user.IsActive,
            Bank = user.BankingDetail?.Bank,
            Branch = user.BankingDetail?.Branch,
            BranchCode = user.BankingDetail?.BranchCode
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ArtistFormViewModel model)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .Include(u => u.BankingDetail)
            .FirstOrDefaultAsync(u => u.UserId == id && u.Role.RoleName == RoleNames.Artist);

        if (user is null)
        {
            return NotFound();
        }

        model.UserId = id;
        if (!await ValidateArtistAsync(model, isNew: false))
        {
            return View("Form", model);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.Username = model.Username.Trim();
        user.Email = model.Email.Trim();
        user.ContactNumber = model.ContactNumber?.Trim();
        user.IdNumber = model.IdNumber?.Trim();
        user.Address = model.Address?.Trim();
        user.IsActive = model.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = CurrentUserId;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = hasher.Hash(model.Password);
        }

        if (HasBanking(model))
        {
            if (user.BankingDetail is null)
            {
                user.BankingDetail = new ArtistBankingDetail
                {
                    ArtistId = user.UserId,
                    Bank = model.Bank!.Trim(),
                    Branch = model.Branch!.Trim(),
                    BranchCode = model.BranchCode!.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = CurrentUserId,
                    UpdatedBy = CurrentUserId
                };
            }
            else
            {
                user.BankingDetail.Bank = model.Bank!.Trim();
                user.BankingDetail.Branch = model.Branch!.Trim();
                user.BankingDetail.BranchCode = model.BranchCode!.Trim();
                user.BankingDetail.UpdatedAt = DateTime.UtcNow;
                user.BankingDetail.UpdatedBy = CurrentUserId;
            }
        }

        await db.SaveChangesAsync();
        await LogAuditAsync(db, "Users", user.UserId, "UserUpdate");
        FlashSuccess($"{user.FullName}'s account was updated.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id && u.Role.RoleName == RoleNames.Artist);

        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = CurrentUserId;
        await db.SaveChangesAsync();
        await LogAuditAsync(db, "Users", user.UserId, user.IsActive ? "UserActivate" : "UserDeactivate");

        FlashSuccess(user.IsActive
            ? $"{user.FullName}'s account was reactivated."
            : $"{user.FullName}'s account was deactivated.");

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ValidateArtistAsync(ArtistFormViewModel model, bool isNew)
    {
        model.Username = model.Username?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim() ?? string.Empty;
        var id = model.UserId ?? 0;

        if (await db.Users.AnyAsync(u => u.Username == model.Username && u.UserId != id))
        {
            ModelState.AddModelError(nameof(model.Username), "That username is already in use.");
        }

        if (await db.Users.AnyAsync(u => u.Email == model.Email && u.UserId != id))
        {
            ModelState.AddModelError(nameof(model.Email), "That email address is already registered.");
        }

        if (isNew || !string.IsNullOrWhiteSpace(model.Password))
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Enter a password for the artist.");
            }
            else if (model.Password.Length < 8)
            {
                ModelState.AddModelError(nameof(model.Password), "Use at least 8 characters.");
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "The passwords do not match.");
            }
        }

        if (HasBanking(model))
        {
            if (string.IsNullOrWhiteSpace(model.Bank))
            {
                ModelState.AddModelError(nameof(model.Bank), "Enter the bank name.");
            }

            if (string.IsNullOrWhiteSpace(model.Branch))
            {
                ModelState.AddModelError(nameof(model.Branch), "Enter the branch.");
            }

            if (string.IsNullOrWhiteSpace(model.BranchCode))
            {
                ModelState.AddModelError(nameof(model.BranchCode), "Enter the branch code.");
            }
        }

        return ModelState.IsValid;
    }

    private static bool HasBanking(ArtistFormViewModel model) =>
        !string.IsNullOrWhiteSpace(model.Bank)
        || !string.IsNullOrWhiteSpace(model.Branch)
        || !string.IsNullOrWhiteSpace(model.BranchCode);
}
