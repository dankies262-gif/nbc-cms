using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;

namespace NbcCms.Api.Services.Interfaces;

public record DocumentDownload(string FullPath, string FileName, string ContentType);

public interface ISupportingDocumentService
{
    Task<List<SupportingDocument>> ListAsync(int claimId, int userId, bool isStaff);
    Task<SupportingDocument> UploadAsync(SupportingDocumentRequest request, int userId, bool isStaff);
    Task<DocumentDownload> DownloadAsync(int documentId, int userId, bool isStaff);
    Task DeleteAsync(int documentId, int userId, bool isStaff);
}
