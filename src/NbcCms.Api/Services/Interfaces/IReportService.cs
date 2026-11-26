using ClaimDto = NbcCms.Api.Models.Response.Claim;
using ReportDto = NbcCms.Api.Models.Response.Report;

namespace NbcCms.Api.Services.Interfaces;

public interface IReportService
{
    Task<List<ReportDto>> ListAsync(int take = 100);
    Task<List<ClaimDto>> ReadyAsync();
    Task<ReportDto> GenerateAsync(int claimId, int userId, bool regenerate = false);
    Task<(byte[] Bytes, string FileName)> DownloadAsync(int reportId, int userId, bool isStaff);
}
