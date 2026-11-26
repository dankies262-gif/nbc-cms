using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data.Entities;
using NbcCms.Web.ViewModels;

namespace NbcCms.Web.Services;

public static class ClaimQueries
{
    public static IQueryable<Claim> WithSummary(this IQueryable<Claim> query) =>
        query
            .Include(c => c.Claimant)
            .Include(c => c.Status)
            .Include(c => c.Items)
            .Include(c => c.Documents)
            .Include(c => c.Corrections)
            .Include(c => c.Report);

    public static ClaimListItemViewModel ToListItem(this Claim claim) => new()
    {
        ClaimId = claim.ClaimId,
        Reference = claim.Reference,
        ClaimantName = claim.Claimant.FullName,
        ClaimantEmail = claim.Claimant.Email,
        ClaimDate = claim.ClaimDate,
        ProgrammeTitle = claim.ProgrammeTitle ?? "—",
        Amount = claim.FeesAmount ?? claim.Items.Sum(i => i.FeesAmount),
        Status = claim.Status.StatusName,
        SubmittedAt = claim.SubmittedAt,
        CreatedAt = claim.CreatedAt,
        ItemCount = claim.Items.Count,
        DocumentCount = claim.Documents.Count(d => d.IsActive),
        HasOpenCorrection = claim.Corrections.Any(c => c.CorrectedAt == null),
        HasReport = claim.Report != null
    };
}
