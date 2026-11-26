using NbcCms.Api.Data.Entities;
using ClaimEntity = NbcCms.Api.Data.Entities.Claim;
using CorrectionEntity = NbcCms.Api.Data.Entities.ClaimCorrection;
using ItemEntity = NbcCms.Api.Data.Entities.ClaimItem;

namespace NbcCms.Api.Models.Response;

public class Claim
{
    public int ClaimId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int ClaimantId { get; set; }
    public string ClaimantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusSlug { get; set; } = string.Empty;
    public DateTime ClaimDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RejectionComment { get; set; }
    public string? ProgrammeTitle { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public List<ClaimItemInfo> Items { get; set; } = [];
    public List<ClaimCorrectionInfo> Corrections { get; set; } = [];
    public List<SupportingDocument> Documents { get; set; } = [];
    public ReportInfo? Report { get; set; }
}

public class ClaimItemInfo
{
    public int ClaimItemId { get; set; }
    public DateTime RecordingDate { get; set; }
    public string ProgrammeTitle { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal FeesAmount { get; set; }
}

public class ClaimCorrectionInfo
{
    public int CorrectionId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int RequestedBy { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? CorrectedAt { get; set; }
    public bool IsResolved { get; set; }
}

public class ReportInfo
{
    public int ReportId { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public static class ClaimMapping
{
    public static Claim ToResponse(ClaimEntity claim) => new()
    {
        ClaimId = claim.ClaimId,
        Reference = claim.Reference,
        ClaimantId = claim.ClaimantId,
        ClaimantName = claim.Claimant?.FullName ?? string.Empty,
        Status = claim.Status?.StatusName ?? string.Empty,
        StatusSlug = claim.Status is null ? string.Empty : ClaimStatusNames.Slug(claim.Status.StatusName),
        ClaimDate = claim.ClaimDate,
        SubmittedAt = claim.SubmittedAt,
        ReviewedBy = claim.ReviewedBy,
        ReviewedByName = claim.Reviewer?.FullName,
        ReviewedAt = claim.ReviewedAt,
        CreatedAt = claim.CreatedAt,
        UpdatedAt = claim.UpdatedAt,
        RejectionComment = claim.RejectionComment,
        ProgrammeTitle = claim.ProgrammeTitle,
        TotalAmount = claim.FeesAmount ?? claim.Items.Sum(i => i.FeesAmount),
        ItemCount = claim.Items.Count,
        Items = claim.Items
            .OrderBy(i => i.RecordingDate)
            .ThenBy(i => i.ClaimItemId)
            .Select(ToItem)
            .ToList(),
        Corrections = claim.Corrections
            .OrderBy(c => c.RequestedAt)
            .Select(ToCorrection)
            .ToList(),
        Documents = claim.Documents
            .Where(d => d.IsActive)
            .OrderBy(d => d.UploadedAt)
            .Select(SupportingDocument.FromEntity)
            .ToList(),
        Report = claim.Report is null ? null : new ReportInfo
        {
            ReportId = claim.Report.ReportId,
            ReportNumber = claim.Report.ReportNumber,
            FileName = claim.Report.FileName,
            GeneratedAt = claim.Report.GeneratedAt,
            DownloadUrl = $"/api/reports/{claim.Report.ReportId}/download"
        }
    };

    private static ClaimItemInfo ToItem(ItemEntity item) => new()
    {
        ClaimItemId = item.ClaimItemId,
        RecordingDate = item.RecordingDate,
        ProgrammeTitle = item.ProgrammeTitle,
        Rate = item.Rate,
        FeesAmount = item.FeesAmount
    };

    private static ClaimCorrectionInfo ToCorrection(CorrectionEntity correction) => new()
    {
        CorrectionId = correction.CorrectionId,
        Comment = correction.CorrectionComment,
        RequestedBy = correction.RequestedBy,
        RequestedByName = correction.RequestedByUser?.FullName ?? string.Empty,
        RequestedAt = correction.RequestedAt,
        CorrectedAt = correction.CorrectedAt,
        IsResolved = correction.CorrectedAt.HasValue
    };
}
