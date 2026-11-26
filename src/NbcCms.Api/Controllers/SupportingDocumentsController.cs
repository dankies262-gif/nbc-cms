using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;
using NbcCms.Api.Services.Interfaces;

namespace NbcCms.Api.Controllers;

[Route("api/supporting-documents")]
public class SupportingDocumentsController(ISupportingDocumentService documents) : ApiControllerBase
{
    [HttpGet]
    public Task<List<SupportingDocument>> List([FromQuery] int claimId)
        => documents.ListAsync(claimId, CurrentUserId, IsStaff);

    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public Task<SupportingDocument> Upload([FromForm] SupportingDocumentRequest request)
        => documents.UploadAsync(request, CurrentUserId, IsStaff);

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var download = await documents.DownloadAsync(id, CurrentUserId, IsStaff);
        return PhysicalFile(download.FullPath, download.ContentType, download.FileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await documents.DeleteAsync(id, CurrentUserId, IsStaff);
        return NoContent();
    }
}
