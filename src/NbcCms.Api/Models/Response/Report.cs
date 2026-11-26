using ReportEntity = NbcCms.Api.Data.Entities.Report;

namespace NbcCms.Api.Models.Response;

public class Report
{
    public int ReportId { get; set; }
    public int ClaimId { get; set; }
    public string? ClaimReference { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedByName { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;

    public static Report FromEntity(ReportEntity report) => new()
    {
        ReportId = report.ReportId,
        ClaimId = report.ClaimId,
        ClaimReference = report.Claim?.Reference,
        ReportNumber = report.ReportNumber,
        FileName = report.FileName,
        GeneratedAt = report.GeneratedAt,
        GeneratedByName = report.GeneratedByUser?.FullName,
        DownloadUrl = $"/api/reports/{report.ReportId}/download"
    };
}
