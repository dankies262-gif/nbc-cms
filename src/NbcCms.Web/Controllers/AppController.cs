using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Web.Data;
using NbcCms.Web.Data.Entities;

namespace NbcCms.Web.Controllers;

public abstract class AppController : Controller
{
    protected int CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }
    }

    protected bool IsStaff =>
        User.IsInRole(RoleNames.Secretary) || User.IsInRole(RoleNames.Admin);

    protected void FlashSuccess(string message) => TempData["Success"] = message;

    protected void FlashError(string message) => TempData["Error"] = message;

    protected async Task LogAuditAsync(CmsDbContext db, string table, int? recordId, string action, int? userId = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId ?? (CurrentUserId == 0 ? null : CurrentUserId),
            TableName = table,
            RecordID = recordId,
            ActionType = action,
            ActionDate = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
