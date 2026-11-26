using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data;
using NbcCms.Web.Data.Entities;
using NbcCms.Web.ViewModels;

namespace NbcCms.Web.Controllers;

[Authorize(Policy = "Staff")]
public class ProgrammesController(CmsDbContext db) : AppController
{
    public async Task<IActionResult> Index(int? edit)
    {
        var programmes = await db.Programmes
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.ProgrammeName)
            .ToListAsync();

        var model = new ProgrammesPageViewModel
        {
            Programmes = programmes,
            ClaimUsage = await db.ClaimItems.Select(i => i.ProgrammeTitle).Distinct().CountAsync()
        };

        if (edit.HasValue)
        {
            var programme = programmes.FirstOrDefault(p => p.ProgrammeId == edit.Value);
            if (programme is not null)
            {
                model.Form = new ProgrammeFormViewModel
                {
                    ProgrammeId = programme.ProgrammeId,
                    ProgrammeName = programme.ProgrammeName,
                    Description = programme.Description,
                    IsActive = programme.IsActive
                };
            }
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Save(ProgrammeFormViewModel form)
    {
        form.ProgrammeName = form.ProgrammeName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(form.ProgrammeName))
        {
            ModelState.AddModelError(nameof(form.ProgrammeName), "Enter the programme name.");
        }

        var id = form.ProgrammeId ?? 0;
        if (await db.Programmes.AnyAsync(p => p.ProgrammeName == form.ProgrammeName && p.ProgrammeId != id))
        {
            ModelState.AddModelError(nameof(form.ProgrammeName), "A programme with that name already exists.");
        }

        if (!ModelState.IsValid)
        {
            var programmes = await db.Programmes
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.ProgrammeName)
                .ToListAsync();

            return View("Index", new ProgrammesPageViewModel
            {
                Programmes = programmes,
                Form = form,
                ClaimUsage = await db.ClaimItems.Select(i => i.ProgrammeTitle).Distinct().CountAsync()
            });
        }

        if (form.ProgrammeId is null)
        {
            var programme = new Programme
            {
                ProgrammeName = form.ProgrammeName,
                Description = form.Description?.Trim(),
                IsActive = form.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            db.Programmes.Add(programme);
            await db.SaveChangesAsync();
            await LogAuditAsync(db, "programme", programme.ProgrammeId, "ProgrammeCreate");
            FlashSuccess($"Programme '{programme.ProgrammeName}' was added.");
        }
        else
        {
            var programme = await db.Programmes.FirstOrDefaultAsync(p => p.ProgrammeId == form.ProgrammeId.Value);
            if (programme is null)
            {
                return NotFound();
            }

            programme.ProgrammeName = form.ProgrammeName;
            programme.Description = form.Description?.Trim();
            programme.IsActive = form.IsActive;
            programme.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await LogAuditAsync(db, "programme", programme.ProgrammeId, "ProgrammeUpdate");
            FlashSuccess($"Programme '{programme.ProgrammeName}' was updated.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var programme = await db.Programmes.FirstOrDefaultAsync(p => p.ProgrammeId == id);
        if (programme is null)
        {
            return NotFound();
        }

        programme.IsActive = !programme.IsActive;
        programme.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await LogAuditAsync(db, "programme", programme.ProgrammeId, "ProgrammeUpdate");

        FlashSuccess(programme.IsActive
            ? $"'{programme.ProgrammeName}' is now available for new claims."
            : $"'{programme.ProgrammeName}' was deactivated.");

        return RedirectToAction(nameof(Index));
    }
}
