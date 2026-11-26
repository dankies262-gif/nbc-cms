using Microsoft.EntityFrameworkCore;
using NbcCms.Api.Data;
using NbcCms.Api.Data.Entities;
using NbcCms.Api.Models;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;
using Claim = NbcCms.Api.Models.Response.Claim;
using ClaimEntity = NbcCms.Api.Data.Entities.Claim;

namespace NbcCms.Api.Services;

public class ClaimService(ApplicationDbContext db, AuditService audit, IWebHostEnvironment environment)
{
    public async Task<List<Claim>> ListAsync(
        int currentUserId,
        bool isStaff,
        string? status,
        string? scope,
        string? search)
    {
        var query = db.Claims
            .Include(c => c.Claimant)
            .Include(c => c.Status)
            .Include(c => c.Reviewer)
            .Include(c => c.Items)
            .Include(c => c.Corrections).ThenInclude(x => x.RequestedByUser)
            .Include(c => c.Documents)
            .Include(c => c.Report)
            .AsQueryable();

        if (!isStaff)
        {
            query = query.Where(c => c.ClaimantId == currentUserId);
        }

        var all = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        IEnumerable<ClaimEntity> filtered = all;

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusName = status.Trim();
            filtered = filtered.Where(c =>
                c.Status.StatusName.Equals(statusName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(scope))
        {
            filtered = scope.Trim().ToLowerInvariant() switch
            {
                "review" => filtered.Where(c => ClaimStatusNames.AwaitingReview.Contains(c.Status.StatusName)),
                "corrections" => filtered.Where(c => c.Status.StatusName == ClaimStatusNames.CorrectionRequested),
                "final" => filtered.Where(c => ClaimStatusNames.IsFinal(c.Status.StatusName)),
                "draft" => filtered.Where(c => c.Status.StatusName == ClaimStatusNames.Draft),
                _ => filtered
            };
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(c =>
                c.Reference.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (c.ProgrammeTitle?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || c.Claimant.FullName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.Select(ClaimMapping.ToResponse).ToList();
    }

    public async Task<Claim> GetAsync(int claimId, int currentUserId, bool isStaff)
    {
        var claim = await FindDetailedAsync(claimId)
            ?? throw ApiProblemException.NotFound("Claim not found.");

        EnsureVisible(claim, currentUserId, isStaff);
        return ClaimMapping.ToResponse(claim);
    }

    public async Task<Claim> CreateAsync(ClaimRequest request, int userId)
    {
        Validate(request);

        var status = await StatusAsync(ClaimStatusNames.Draft);
        var items = request.Items.Select(ToEntity).ToList();
        var now = DateTime.UtcNow;

        var claim = new ClaimEntity
        {
            ClaimantId = userId,
            StatusId = status.StatusId,
            ClaimDate = request.ClaimDate.Date,
            CreatedAt = now,
            UpdatedAt = now,
            RecordingDate = items.Min(i => i.RecordingDate),
            ProgrammeTitle = Header(request.ProgrammeTitle, items),
            FeesAmount = items.Sum(i => i.FeesAmount),
            Items = items
        };

        db.Claims.Add(claim);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, "claim", claim.ClaimId, "Create");

        return ClaimMapping.ToResponse((await FindDetailedAsync(claim.ClaimId))!);
    }

    public async Task<Claim> UpdateAsync(int claimId, ClaimRequest request, int userId)
    {
        var claim = await db.Claims
            .Include(c => c.Status)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId)
            ?? throw ApiProblemException.NotFound("Claim not found.");

        EnsureOwner(claim, userId);
        EnsureEditable(claim);

        Validate(request);

        db.ClaimItems.RemoveRange(claim.Items);

        var items = request.Items.Select(ToEntity).ToList();
        claim.Items = items;
        claim.ClaimDate = request.ClaimDate.Date;
        claim.RecordingDate = items.Min(i => i.RecordingDate);
        claim.ProgrammeTitle = Header(request.ProgrammeTitle, items);
        claim.FeesAmount = items.Sum(i => i.FeesAmount);
        claim.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, "claim", claim.ClaimId, "Update");

        return ClaimMapping.ToResponse((await FindDetailedAsync(claim.ClaimId))!);
    }

    public async Task<Claim> SubmitAsync(int claimId, int userId)
    {
        var claim = await db.Claims
            .Include(c => c.Status)
            .Include(c => c.Items)
            .Include(c => c.Corrections)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId)
            ?? throw ApiProblemException.NotFound("Claim not found.");

        EnsureOwner(claim, userId);

        var status = claim.Status.StatusName;
        if (status is not (ClaimStatusNames.Draft or ClaimStatusNames.CorrectionRequested))
        {
            throw ApiProblemException.Conflict(
                "Only draft claims or claims awaiting correction can be submitted.");
        }

        if (claim.Items.Count == 0)
        {
            throw ApiProblemException.BadRequest("Add at least one work record before submitting the claim.");
        }

        var now = DateTime.UtcNow;
        var resubmission = status == ClaimStatusNames.CorrectionRequested;

        if (resubmission)
        {
            foreach (var correction in claim.Corrections.Where(c => c.CorrectedAt is null))
            {
                correction.CorrectedAt = now;
            }
        }

        var next = await StatusAsync(resubmission ? ClaimStatusNames.Resubmitted : ClaimStatusNames.Submitted);
        claim.StatusId = next.StatusId;
        claim.SubmittedAt = now;
        claim.UpdatedAt = now;

        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, "claim", claim.ClaimId, resubmission ? "Resubmit" : "Submit");

        return ClaimMapping.ToResponse((await FindDetailedAsync(claim.ClaimId))!);
    }

    public async Task<Claim> ReviewAsync(int claimId, ClaimReviewRequest request, int staffId)
    {
        var claim = await db.Claims
            .Include(c => c.Status)
            .Include(c => c.Corrections)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId)
            ?? throw ApiProblemException.NotFound("Claim not found.");

        if (!ClaimStatusNames.AwaitingReview.Contains(claim.Status.StatusName))
        {
            throw ApiProblemException.Conflict("Only submitted or resubmitted claims can be reviewed.");
        }

        var decision = request.Decision.Trim().ToLowerInvariant();
        var comment = request.Comment?.Trim();
        var now = DateTime.UtcNow;

        claim.ReviewedBy = staffId;
        claim.ReviewedAt = now;
        claim.UpdatedAt = now;

        switch (decision)
        {
            case "accept":
            {
                var accepted = await StatusAsync(ClaimStatusNames.Accepted);
                claim.StatusId = accepted.StatusId;
                claim.RejectionComment = null;
                await db.SaveChangesAsync();
                await audit.RecordAsync(staffId, "claim", claim.ClaimId, "Accept");
                break;
            }
            case "decline":
            {
                if (string.IsNullOrWhiteSpace(comment))
                {
                    throw ApiProblemException.BadRequest("Provide a reason before declining the claim.");
                }

                var declined = await StatusAsync(ClaimStatusNames.Declined);
                claim.StatusId = declined.StatusId;
                claim.RejectionComment = comment;
                await db.SaveChangesAsync();
                await audit.RecordAsync(staffId, "claim", claim.ClaimId, "Decline");
                break;
            }
            case "correction":
            {
                if (string.IsNullOrWhiteSpace(comment))
                {
                    throw ApiProblemException.BadRequest("Provide a comment before requesting a correction.");
                }

                var correctionStatus = await StatusAsync(ClaimStatusNames.CorrectionRequested);
                claim.StatusId = correctionStatus.StatusId;
                claim.RejectionComment = null;
                db.ClaimCorrections.Add(new ClaimCorrection
                {
                    ClaimId = claim.ClaimId,
                    RequestedBy = staffId,
                    CorrectionComment = comment,
                    RequestedAt = now
                });

                await db.SaveChangesAsync();
                await audit.RecordAsync(staffId, "claim", claim.ClaimId, "Correction");
                break;
            }
            default:
                throw ApiProblemException.BadRequest("Decision must be accept, decline or correction.");
        }

        return ClaimMapping.ToResponse((await FindDetailedAsync(claim.ClaimId))!);
    }

    public async Task DeleteAsync(int claimId, int userId)
    {
        var claim = await db.Claims
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId)
            ?? throw ApiProblemException.NotFound("Claim not found.");

        EnsureOwner(claim, userId);

        if (claim.Status.StatusName != ClaimStatusNames.Draft)
        {
            throw ApiProblemException.Conflict("Only draft claims can be deleted.");
        }

        db.Claims.Remove(claim);
        await db.SaveChangesAsync();

        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "uploads", claimId.ToString());
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        await audit.RecordAsync(userId, "claim", claimId, "Delete");
    }

    private async Task<ClaimEntity?> FindDetailedAsync(int claimId) =>
        await db.Claims
            .Include(c => c.Claimant)
            .Include(c => c.Status)
            .Include(c => c.Reviewer)
            .Include(c => c.Items)
            .Include(c => c.Corrections).ThenInclude(x => x.RequestedByUser)
            .Include(c => c.Documents)
            .Include(c => c.Report)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

    private async Task<ClaimStatus> StatusAsync(string name) =>
        await db.ClaimStatusLookup.FirstOrDefaultAsync(s => s.StatusName == name)
        ?? throw new InvalidOperationException($"Claim status '{name}' is missing from the database.");

    private static void EnsureVisible(ClaimEntity claim, int userId, bool isStaff)
    {
        if (!isStaff && claim.ClaimantId != userId)
        {
            throw ApiProblemException.NotFound("Claim not found.");
        }
    }

    private static void EnsureOwner(ClaimEntity claim, int userId)
    {
        if (claim.ClaimantId != userId)
        {
            throw ApiProblemException.NotFound("Claim not found.");
        }
    }

    private static void EnsureEditable(ClaimEntity claim)
    {
        if (!ClaimStatusNames.Editable.Contains(claim.Status.StatusName))
        {
            throw ApiProblemException.Conflict(
                "Only draft claims or claims awaiting correction can be edited.");
        }
    }

    private static void Validate(ClaimRequest request)
    {
        if (request.ClaimDate.Date > DateTime.Today)
        {
            throw ApiProblemException.BadRequest("The claim date cannot be in the future.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw ApiProblemException.BadRequest("A claim needs at least one work record.");
        }

        foreach (var item in request.Items)
        {
            if (item.RecordingDate.Date > DateTime.Today)
            {
                throw ApiProblemException.BadRequest("Recording dates cannot be in the future.");
            }

            if (string.IsNullOrWhiteSpace(item.ProgrammeTitle))
            {
                throw ApiProblemException.BadRequest("Every work record needs a programme title.");
            }

            if (item.Rate <= 0)
            {
                throw ApiProblemException.BadRequest("Every work record needs a rate greater than zero.");
            }
        }
    }

    private static ClaimItem ToEntity(ClaimItemRequest item) => new()
    {
        RecordingDate = item.RecordingDate.Date,
        ProgrammeTitle = item.ProgrammeTitle.Trim(),
        Rate = item.Rate,
        FeesAmount = item.Rate,
        CreatedAt = DateTime.UtcNow
    };

    private static string Header(string? provided, List<ClaimItem> items)
    {
        if (!string.IsNullOrWhiteSpace(provided))
        {
            return provided.Trim();
        }

        return string.Join(", ", items
            .Select(i => i.ProgrammeTitle)
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
