using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data;
using NbcCms.Web.Data.Entities;

namespace NbcCms.Web.Controllers;

[Authorize(Policy = "Staff")]
public class AuditController(CmsDbContext db) : AppController
{
    public async Task<IActionResult> Index(string? actionType, string? search)
    {
        var query = db.AuditLogs.Include(a => a.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(a => a.ActionType == actionType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                (a.User != null && (a.User.Username.Contains(term)
                    || a.User.FirstName.Contains(term)
                    || a.User.LastName.Contains(term)))
                || a.TableName.Contains(term)
                || a.ActionType.Contains(term));
        }

        var logs = await query
            .OrderByDescending(a => a.ActionDate)
            .Take(300)
            .ToListAsync();

        ViewData["Actions"] = await db.AuditLogs
            .Select(a => a.ActionType)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
        ViewData["Action"] = actionType;
        ViewData["Search"] = search;

        return View(logs);
    }
}
