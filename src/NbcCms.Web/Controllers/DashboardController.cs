using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data;
using NbcCms.Web.Data.Entities;
using NbcCms.Web.Services;
using NbcCms.Web.ViewModels;

namespace NbcCms.Web.Controllers;

[Authorize]
public class DashboardController(CmsDbContext db) : AppController
{
    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            IsStaff = IsStaff,
            Role = User.FindFirstValue("RoleLabel") ?? (IsStaff ? "Secretary" : "Artist"),
            DisplayName = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "User"
        };

        if (IsStaff)
        {
            await BuildSecretaryDashboard(model);
        }
        else
        {
            await BuildArtistDashboard(model);
        }

        return View(model);
    }

    private async Task BuildSecretaryDashboard(DashboardViewModel model)
    {
        var claims = await db.Claims
            .WithSummary()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var artistRoleId = await db.UserRoles
            .Where(r => r.RoleName == RoleNames.Artist)
            .Select(r => r.RoleId)
            .FirstAsync();

        model.TotalClaims = claims.Count;
        model.Drafts = claims.Count(c => c.Status.StatusName == ClaimStatusNames.Draft);
        model.AwaitingReview = claims.Count(c => ClaimStatusNames.AwaitingReview.Contains(c.Status.StatusName));
        model.AwaitingArtist = claims.Count(c => ClaimStatusNames.AwaitingArtist.Contains(c.Status.StatusName));
        model.Accepted = claims.Count(c => c.Status.StatusName == ClaimStatusNames.Accepted);
        model.Declined = claims.Count(c => c.Status.StatusName == ClaimStatusNames.Declined);
        model.TotalValue = claims.Sum(c => c.FeesAmount ?? 0);
        model.ApprovedValue = claims.Where(c => c.Status.StatusName == ClaimStatusNames.Accepted).Sum(c => c.FeesAmount ?? 0);
        model.PendingValue = claims.Where(c => ClaimStatusNames.Open.Contains(c.Status.StatusName)).Sum(c => c.FeesAmount ?? 0);
        model.DeclinedValue = claims.Where(c => c.Status.StatusName == ClaimStatusNames.Declined).Sum(c => c.FeesAmount ?? 0);
        model.ActiveArtists = await db.Users.CountAsync(u => u.RoleId == artistRoleId && u.IsActive);
        model.ActiveProgrammes = await db.Programmes.CountAsync(p => p.IsActive);
        model.ReportsGenerated = await db.Reports.CountAsync();
        model.Documents = await db.SupportingDocuments.CountAsync(d => d.IsActive);

        model.RecentClaims = claims.Take(6).Select(c => c.ToListItem()).ToList();
        model.NeedsAttention = claims
            .Where(c => ClaimStatusNames.AwaitingReview.Contains(c.Status.StatusName))
            .OrderBy(c => c.SubmittedAt ?? c.CreatedAt)
            .Take(5)
            .Select(c => c.ToListItem())
            .ToList();

        model.RecentActivity = await db.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.ActionDate)
            .Take(8)
            .ToListAsync();
    }

    private async Task BuildArtistDashboard(DashboardViewModel model)
    {
        var claims = await db.Claims
            .Where(c => c.ClaimantId == CurrentUserId)
            .WithSummary()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        model.TotalClaims = claims.Count;
        model.Drafts = claims.Count(c => c.Status.StatusName == ClaimStatusNames.Draft);
        model.AwaitingReview = claims.Count(c => ClaimStatusNames.AwaitingReview.Contains(c.Status.StatusName));
        model.AwaitingArtist = claims.Count(c => ClaimStatusNames.AwaitingArtist.Contains(c.Status.StatusName));
        model.Accepted = claims.Count(c => c.Status.StatusName == ClaimStatusNames.Accepted);
        model.Declined = claims.Count(c => c.Status.StatusName == ClaimStatusNames.Declined);
        model.TotalValue = claims.Sum(c => c.FeesAmount ?? 0);
        model.ApprovedValue = claims.Where(c => c.Status.StatusName == ClaimStatusNames.Accepted).Sum(c => c.FeesAmount ?? 0);
        model.PendingValue = claims.Where(c => ClaimStatusNames.Open.Contains(c.Status.StatusName)).Sum(c => c.FeesAmount ?? 0);
        model.DeclinedValue = claims.Where(c => c.Status.StatusName == ClaimStatusNames.Declined).Sum(c => c.FeesAmount ?? 0);
        model.Documents = claims.SelectMany(c => c.Documents).Count(d => d.IsActive);
        model.ReportsGenerated = claims.Count(c => c.Report != null);
        model.ActiveProgrammes = await db.Programmes.CountAsync(p => p.IsActive);

        model.RecentClaims = claims.Take(6).Select(c => c.ToListItem()).ToList();
        model.NeedsAttention = claims
            .Where(c => c.Status.StatusName == ClaimStatusNames.CorrectionRequested)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Take(5)
            .Select(c => c.ToListItem())
            .ToList();
    }
}
