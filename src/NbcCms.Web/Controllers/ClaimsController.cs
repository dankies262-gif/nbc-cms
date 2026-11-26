using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data;
using NbcCms.Web.Data.Entities;
using NbcCms.Web.Services;
using NbcCms.Web.ViewModels;

namespace NbcCms.Web.Controllers;

[Authorize]
public class ClaimsController(CmsDbContext db, DocumentStorage storage) : AppController
{
    private static readonly string[] EditableStatuses = [ClaimStatusNames.Draft, ClaimStatusNames.CorrectionRequested];

    public async Task<IActionResult> Index(string? status, string? search, string? scope)
    {
        var query = db.Claims.WithSummary();
        if (!IsStaff)
        {
            query = query.Where(c => c.ClaimantId == CurrentUserId);
        }

        var all = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        IEnumerable<Claim> filtered = all;

        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(c => c.Status.StatusName == status);
        }

        if (scope == "review" && IsStaff)
        {
            filtered = filtered.Where(c => ClaimStatusNames.AwaitingReview.Contains(c.Status.StatusName));
        }
        else if (scope == "corrections")
        {
            filtered = filtered.Where(c => c.Status.StatusName == ClaimStatusNames.CorrectionRequested);
        }
        else if (scope == "final")
        {
            filtered = filtered.Where(c => ClaimStatusNames.IsFinal(c.Status.StatusName));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(c => Matches(c, term));
        }

        var list = filtered.Select(c => c.ToListItem()).ToList();
        var statuses = await db.ClaimStatusLookup.OrderBy(s => s.StatusId).ToListAsync();

        return View(new ClaimsIndexViewModel
        {
            Claims = list,
            Statuses = statuses,
            Search = search,
            Status = status,
            Scope = scope,
            IsStaffView = IsStaff,
            TotalCount = all.Count,
            AwaitingReviewCount = all.Count(c => ClaimStatusNames.AwaitingReview.Contains(c.Status.StatusName)),
            CorrectionCount = all.Count(c => c.Status.StatusName == ClaimStatusNames.CorrectionRequested),
            AcceptedCount = all.Count(c => c.Status.StatusName == ClaimStatusNames.Accepted),
            TotalValue = all.Sum(c => c.FeesAmount ?? 0)
        });
    }

    [Authorize(Policy = "ArtistOnly")]
    [HttpGet]
    public async Task<IActionResult> New()
    {
        var model = new ClaimFormViewModel
        {
            ClaimDate = DateTime.Today,
            Programmes = await ProgrammesAsync()
        };

        return View("Form", model);
    }

    [Authorize(Policy = "ArtistOnly")]
    [HttpPost]
    public async Task<IActionResult> New(ClaimFormViewModel model, string? intent)
    {
        if (!await ValidateClaimAsync(model))
        {
            return View("Form", model);
        }

        var now = DateTime.UtcNow;
        var submitNow = intent == "submit";

        var claim = new Claim
        {
            ClaimantId = CurrentUserId,
            StatusId = await StatusIdAsync(submitNow ? ClaimStatusNames.Submitted : ClaimStatusNames.Draft),
            ClaimDate = model.ClaimDate.Date,
            CreatedAt = now,
            SubmittedAt = submitNow ? now : null
        };

        ApplyItems(claim, model.Items, now);

        db.Claims.Add(claim);
        await db.SaveChangesAsync();
        await LogAuditAsync(db, "claim", claim.ClaimId, "Create");

        if (submitNow)
        {
            await LogAuditAsync(db, "claim", claim.ClaimId, "Submit");
            FlashSuccess($"Claim {claim.Reference} was submitted to the Secretary for review.");
        }
        else
        {
            FlashSuccess($"Claim {claim.Reference} was saved as a draft.");
        }

        return RedirectToAction(nameof(Details), new { id = claim.ClaimId });
    }

    [Authorize(Policy = "ArtistOnly")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var claim = await db.Claims
            .Include(c => c.Items)
            .Include(c => c.Corrections)
            .FirstOrDefaultAsync(c => c.ClaimId == id && c.ClaimantId == CurrentUserId);

        if (claim is null)
        {
            return NotFound();
        }

        var statusName = await StatusNameAsync(claim.StatusId);
        if (!EditableStatuses.Contains(statusName))
        {
            FlashError("Only draft claims or claims returned for correction can be edited.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var openCorrection = claim.Corrections
            .Where(c => c.CorrectedAt == null)
            .OrderByDescending(c => c.RequestedAt)
            .FirstOrDefault();

        var model = new ClaimFormViewModel
        {
            ClaimId = claim.ClaimId,
            Reference = claim.Reference,
            Status = statusName,
            ClaimDate = claim.ClaimDate,
            IsCorrection = openCorrection is not null,
            CorrectionComment = openCorrection?.CorrectionComment,
            Programmes = await ProgrammesAsync(),
            Items = claim.Items
                .OrderBy(i => i.RecordingDate)
                .Select(i => new ClaimItemInput
                {
                    RecordingDate = i.RecordingDate,
                    ProgrammeTitle = i.ProgrammeTitle,
                    Rate = i.Rate
                })
                .ToList()
        };

        if (model.Items.Count == 0)
        {
            model.Items.Add(new ClaimItemInput());
        }

        return View("Form", model);
    }

    [Authorize(Policy = "ArtistOnly")]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, ClaimFormViewModel model, string? intent)
    {
        var claim = await db.Claims
            .Include(c => c.Items)
            .Include(c => c.Corrections)
            .FirstOrDefaultAsync(c => c.ClaimId == id && c.ClaimantId == CurrentUserId);

        if (claim is null)
        {
            return NotFound();
        }

        var statusName = await StatusNameAsync(claim.StatusId);
        if (!EditableStatuses.Contains(statusName))
        {
            FlashError("This claim can no longer be edited.");
            return RedirectToAction(nameof(Details), new { id });
        }

        model.ClaimId = claim.ClaimId;
        model.Reference = claim.Reference;
        model.Status = statusName;

        if (!await ValidateClaimAsync(model))
        {
            return View("Form", model);
        }

        var now = DateTime.UtcNow;
        claim.ClaimDate = model.ClaimDate.Date;
        claim.UpdatedAt = now;

        claim.Items.Clear();
        ApplyItems(claim, model.Items, now);

        var resubmit = intent == "resubmit" && statusName == ClaimStatusNames.CorrectionRequested;
        var submitNow = intent == "submit" && statusName == ClaimStatusNames.Draft;

        if (resubmit)
        {
            foreach (var correction in claim.Corrections.Where(c => c.CorrectedAt == null))
            {
                correction.CorrectedAt = now;
            }

            claim.StatusId = await StatusIdAsync(ClaimStatusNames.Resubmitted);
            claim.SubmittedAt = now;
        }
        else if (submitNow)
        {
            claim.StatusId = await StatusIdAsync(ClaimStatusNames.Submitted);
            claim.SubmittedAt = now;
        }

        await db.SaveChangesAsync();
        await LogAuditAsync(db, "claim", claim.ClaimId, resubmit ? "Resubmit" : "Update");

        FlashSuccess(resubmit
            ? $"Claim {claim.Reference} was corrected and resubmitted for review."
            : $"Claim {claim.Reference} was updated.");

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var claim = await db.Claims
            .Include(c => c.Claimant).ThenInclude(u => u.BankingDetail)
            .Include(c => c.Status)
            .Include(c => c.Reviewer)
            .Include(c => c.Items)
            .Include(c => c.Documents).ThenInclude(d => d.UploadedByUser)
            .Include(c => c.Corrections).ThenInclude(x => x.RequestedByUser)
            .Include(c => c.Report)
            .FirstOrDefaultAsync(c => c.ClaimId == id);

        if (claim is null)
        {
            return NotFound();
        }

        if (!IsStaff && claim.ClaimantId != CurrentUserId)
        {
            return Forbid();
        }

        var status = claim.Status.StatusName;
        var openCorrection = claim.Corrections
            .Where(c => c.CorrectedAt == null)
            .OrderByDescending(c => c.RequestedAt)
            .FirstOrDefault();

        var canEdit = !IsStaff && EditableStatuses.Contains(status);

        return View(new ClaimDetailsViewModel
        {
            Claim = claim,
            Items = claim.Items.OrderBy(i => i.RecordingDate).ThenBy(i => i.ClaimItemId).ToList(),
            Corrections = claim.Corrections.OrderByDescending(c => c.RequestedAt).ToList(),
            Documents = claim.Documents.Where(d => d.IsActive).OrderByDescending(d => d.UploadedAt).ToList(),
            Report = claim.Report,
            OpenCorrection = openCorrection,
            CanEdit = canEdit,
            CanReview = IsStaff && ClaimStatusNames.AwaitingReview.Contains(status),
            CanGenerateReport = IsStaff && status == ClaimStatusNames.Accepted && claim.Report is null,
            CanUpload = canEdit || IsStaff,
            MaxUploadMegabytes = storage.MaxMegabytes,
            AllowedExtensions = storage.Extensions
        });
    }

    [Authorize(Policy = "ArtistOnly")]
    [HttpPost]
    public async Task<IActionResult> Submit(int id)
    {
        var claim = await db.Claims
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.ClaimId == id && c.ClaimantId == CurrentUserId);

        if (claim is null)
        {
            return NotFound();
        }

        if (await StatusNameAsync(claim.StatusId) != ClaimStatusNames.Draft)
        {
            FlashError("Only draft claims can be submitted.");
            return RedirectToAction(nameof(Details), new { id });
        }

        if (claim.Items.Count == 0)
        {
            FlashError("Add at least one work record before submitting the claim.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        var now = DateTime.UtcNow;
        claim.StatusId = await StatusIdAsync(ClaimStatusNames.Submitted);
        claim.SubmittedAt = now;
        claim.UpdatedAt = now;
        await db.SaveChangesAsync();
        await LogAuditAsync(db, "claim", claim.ClaimId, "Submit");

        FlashSuccess($"Claim {claim.Reference} was submitted for review.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = "ArtistOnly")]
    [HttpPost]
    public async Task<IActionResult> Resubmit(int id)
    {
        var claim = await db.Claims
            .Include(c => c.Items)
            .Include(c => c.Corrections)
            .FirstOrDefaultAsync(c => c.ClaimId == id && c.ClaimantId == CurrentUserId);

        if (claim is null)
        {
            return NotFound();
        }

        if (await StatusNameAsync(claim.StatusId) != ClaimStatusNames.CorrectionRequested)
        {
            FlashError("This claim is not awaiting corrections.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        foreach (var correction in claim.Corrections.Where(c => c.CorrectedAt == null))
        {
            correction.CorrectedAt = now;
        }

        claim.StatusId = await StatusIdAsync(ClaimStatusNames.Resubmitted);
        claim.SubmittedAt = now;
        claim.UpdatedAt = now;
        await db.SaveChangesAsync();
        await LogAuditAsync(db, "claim", claim.ClaimId, "Resubmit");

        FlashSuccess($"Claim {claim.Reference} was resubmitted for review.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = "Staff")]
    [HttpPost]
    public async Task<IActionResult> Review(int id, string decision, string? comment)
    {
        var claim = await db.Claims
            .Include(c => c.Items)
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c => c.ClaimId == id);

        if (claim is null)
        {
            return NotFound();
        }

        if (!ClaimStatusNames.AwaitingReview.Contains(claim.Status.StatusName))
        {
            FlashError("This claim is not currently awaiting review.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var note = comment?.Trim();
        if (decision is "decline" or "correction" && string.IsNullOrWhiteSpace(note))
        {
            FlashError("Provide a reason before returning or declining the claim.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        claim.ReviewedBy = CurrentUserId;
        claim.ReviewedAt = now;
        claim.UpdatedAt = now;

        switch (decision)
        {
            case "accept":
                claim.StatusId = await StatusIdAsync(ClaimStatusNames.Accepted);
                claim.RejectionComment = null;

                var previous = await db.OverallAmounts.Where(a => a.ClaimId == claim.ClaimId).ToListAsync();
                if (previous.Count > 0)
                {
                    db.OverallAmounts.RemoveRange(previous);
                }

                db.OverallAmounts.Add(new OverallAmount
                {
                    ClaimId = claim.ClaimId,
                    Rate = claim.FeesAmount ?? 0,
                    Quantity = claim.Items.Count,
                    CalculatedAmount = claim.FeesAmount ?? 0,
                    CalculatedBy = CurrentUserId,
                    CalculatedAt = now,
                    ItemDescription = "Approved claim total",
                    UpdatedBy = CurrentUserId,
                    UpdatedAt = now
                });

                await db.SaveChangesAsync();
                await LogAuditAsync(db, "claim", claim.ClaimId, "Accept");
                FlashSuccess($"Claim {claim.Reference} has been accepted. You can now generate the claim report.");
                break;

            case "decline":
                claim.StatusId = await StatusIdAsync(ClaimStatusNames.Declined);
                claim.RejectionComment = Truncate(note!, 1000);
                await db.SaveChangesAsync();
                await LogAuditAsync(db, "claim", claim.ClaimId, "Decline");
                FlashSuccess($"Claim {claim.Reference} has been declined.");
                break;

            case "correction":
                claim.StatusId = await StatusIdAsync(ClaimStatusNames.CorrectionRequested);
                claim.RejectionComment = Truncate(note!, 1000);
                db.ClaimCorrections.Add(new ClaimCorrection
                {
                    ClaimId = claim.ClaimId,
                    RequestedBy = CurrentUserId,
                    CorrectionComment = note!,
                    RequestedAt = now
                });
                await db.SaveChangesAsync();
                await LogAuditAsync(db, "claim", claim.ClaimId, "Correction");
                FlashSuccess($"Correction requested for claim {claim.Reference}.");
                break;

            default:
                FlashError("Unknown review decision.");
                break;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static bool Matches(Claim claim, string term)
    {
        if (term.All(char.IsDigit) && int.TryParse(term, out var numericId))
        {
            return claim.ClaimId == numericId;
        }

        return claim.Reference.Contains(term, StringComparison.OrdinalIgnoreCase)
               || claim.Claimant.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
               || (claim.Claimant.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
               || (claim.ProgrammeTitle?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private async Task<bool> ValidateClaimAsync(ClaimFormViewModel model)
    {
        var today = DateTime.Today;

        if (model.ClaimDate == default)
        {
            model.ClaimDate = today;
        }

        if (model.ClaimDate.Date > today)
        {
            ModelState.AddModelError(nameof(model.ClaimDate), "The claim date cannot be in the future.");
        }

        model.Programmes = await ProgrammesAsync();
        var validProgrammes = await db.Programmes
            .Select(p => p.ProgrammeName)
            .ToListAsync();
        var lookup = validProgrammes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (model.Items is null || model.Items.Count == 0)
        {
            ModelState.AddModelError("Items", "Add at least one work record to the claim.");
            model.Items = [new ClaimItemInput()];
            return false;
        }

        for (var i = 0; i < model.Items.Count; i++)
        {
            var item = model.Items[i];

            if (string.IsNullOrWhiteSpace(item.ProgrammeTitle))
            {
                ModelState.AddModelError($"Items[{i}].ProgrammeTitle", "Choose a programme.");
            }
            else if (!lookup.Contains(item.ProgrammeTitle.Trim()))
            {
                ModelState.AddModelError($"Items[{i}].ProgrammeTitle", "Choose a programme from the list.");
            }

            if (item.RecordingDate is null)
            {
                ModelState.AddModelError($"Items[{i}].RecordingDate", "Choose the recording date.");
            }
            else if (item.RecordingDate.Value.Date > today)
            {
                ModelState.AddModelError($"Items[{i}].RecordingDate", "The recording date cannot be in the future.");
            }

            if (item.Rate is null or <= 0)
            {
                ModelState.AddModelError($"Items[{i}].Rate", "Enter a fee greater than zero.");
            }
        }

        return ModelState.IsValid;
    }

    private static void ApplyItems(Claim claim, IEnumerable<ClaimItemInput> inputs, DateTime now)
    {
        var items = inputs.Select(i => new ClaimItem
        {
            RecordingDate = i.RecordingDate!.Value.Date,
            ProgrammeTitle = i.ProgrammeTitle.Trim(),
            Rate = Math.Round(i.Rate!.Value, 2),
            FeesAmount = Math.Round(i.Rate!.Value, 2),
            CreatedAt = now
        }).ToList();

        foreach (var item in items)
        {
            claim.Items.Add(item);
        }

        claim.FeesAmount = items.Sum(i => i.FeesAmount);
        claim.ProgrammeTitle = Truncate(
            string.Join(", ", items.Select(i => i.ProgrammeTitle).Distinct(StringComparer.OrdinalIgnoreCase)),
            500);
        claim.RecordingDate = items.Min(i => i.RecordingDate);
    }

    private async Task<List<Programme>> ProgrammesAsync() =>
        await db.Programmes
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProgrammeName)
            .ToListAsync();

    private async Task<int> StatusIdAsync(string name) =>
        await db.ClaimStatusLookup
            .Where(s => s.StatusName == name)
            .Select(s => s.StatusId)
            .FirstAsync();

    private async Task<string> StatusNameAsync(int statusId) =>
        await db.ClaimStatusLookup
            .Where(s => s.StatusId == statusId)
            .Select(s => s.StatusName)
            .FirstAsync();

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
