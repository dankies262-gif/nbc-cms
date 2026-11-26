using DocumentEntity = NbcCms.Api.Data.Entities.SupportingDocument;

namespace NbcCms.Api.Models.Response;

public class SupportingDocument
{
    public int DocumentId { get; set; }
    public int ClaimId { get; set; }
    public int UploadedBy { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IsActive { get; set; }

    public static SupportingDocument FromEntity(DocumentEntity document) => new()
    {
        DocumentId = document.DocumentId,
        ClaimId = document.ClaimId,
        UploadedBy = document.UploadedBy,
        FileName = document.FileName,
        DownloadUrl = $"/api/supporting-documents/{document.DocumentId}/download",
        UploadedAt = document.UploadedAt,
        IsActive = document.IsActive
    };
}
