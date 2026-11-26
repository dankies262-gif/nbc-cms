using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data;
using NbcCms.Web.Data.Entities;
using NbcCms.Web.Services;
using NbcCms.Web.ViewModels;

namespace NbcCms.Web.Controllers;

[Authorize]
public class ReportsController(CmsDbContext db, PdfReportService pdf) : AppController
{
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> Index()
    {
        var ready = await db.Claims
            .WithSummary()
            .Where(c => c.Status.StatusName == ClaimStatusNames.Accepted && c.Report == null)
            .OrderByDescending(c => c.ReviewedAt ?? c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync();

        var generated = await db.Reports
            .Include(r => r.Claim)
            .ThenInclude(c => c.Claimant)
            .OrderByDescending(r => r.GeneratedAt)
            .Take(100)
            .ToListAsync();

        return View(new ReportsIndexViewModel
        {
            ReadyForReport = ready.Select(c => c.ToListItem()).ToList(),
            Generated = generated
        });
    }

    [Authorize(Policy = "Staff")]
    [HttpPost]
    public async Task<IActionResult> Generate(int claimId)
    {
        var claim = await db.Claims
            .Include(c => c.Claimant).ThenInclude(u => u.BankingDetail)
            .Include(c => c.Status)
            .Include(c => c.Reviewer)
            .Include(c => c.Items)
            .Include(c => c.Corrections).ThenInclude(x => x.RequestedByUser)
            .Include(c => c.Documents)
            .Include(c => c.Report)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim is null)
        {
            return NotFound();
        }

        if (claim.Status.StatusName != ClaimStatusNames.Accepted)
        {
            FlashError("A report can only be generated once the claim has been accepted.");
            return RedirectToAction("Details", "Claims", new { id = claimId });
        }

        if (claim.Report is not null)
        {
            return RedirectToAction(nameof(Download), new { id = claim.Report.ReportId });
        }

        var reportNumber = $"NBC-CMS-{Formatting.ToWindhoek(DateTime.UtcNow).Year}-{claim.ClaimId:D5}";
        var bytes = pdf.Build(claim);

        var report = new Report
        {
            ClaimId = claim.ClaimId,
            GeneratedBy = CurrentUserId,
            ReportNumber = reportNumber,
            FileName = $"Claim-Report-{claim.Reference}.pdf",
            GeneratedAt = DateTime.UtcNow,
            FileData = bytes
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync();

        report.FileURL = $"/Reports/Download/{report.ReportId}";
        await db.SaveChangesAsync();
        await LogAuditAsync(db, "report", report.ReportId, "Report");

        FlashSuccess($"Claim report {reportNumber} was generated.");
        return RedirectToAction(nameof(Download), new { id = report.ReportId });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var report = await db.Reports
            .Include(r => r.Claim)
            .FirstOrDefaultAsync(r => r.ReportId == id);

        if (report is null)
        {
            return NotFound();
        }

        if (!IsStaff && report.Claim.ClaimantId != CurrentUserId)
        {
            return Forbid();
        }

        if (report.FileData is null || report.FileData.Length == 0)
        {
            FlashError("The report file is not available. Please regenerate it.");
            return RedirectToAction("Details", "Claims", new { id = report.ClaimId });
        }

        return File(report.FileData, "application/pdf", report.FileName ?? $"{report.ReportNumber}.pdf");
    }
}
