using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Services.Interfaces;
using ClaimDto = NbcCms.Api.Models.Response.Claim;
using ReportDto = NbcCms.Api.Models.Response.Report;

namespace NbcCms.Api.Controllers;

[Route("api/reports")]
public class ReportsController(IReportService reports) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Staff")]
    public Task<List<ReportDto>> List([FromQuery] int take = 100)
        => reports.ListAsync(take);

    /// <summary>Claims that have been accepted and are ready for a report.</summary>
    [HttpGet("ready")]
    [Authorize(Policy = "Staff")]
    public Task<List<ClaimDto>> Ready() => reports.ReadyAsync();

    [HttpPost]
    [Authorize(Policy = "Staff")]
    public async Task<ActionResult<ReportDto>> Generate([FromBody] ReportRequest request)
    {
        var report = await reports.GenerateAsync(request.ClaimId, CurrentUserId, request.Regenerate);
        return Created($"/api/reports/{report.ReportId}", report);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var (bytes, fileName) = await reports.DownloadAsync(id, CurrentUserId, IsStaff);
        return File(bytes, "application/pdf", fileName);
    }
}
