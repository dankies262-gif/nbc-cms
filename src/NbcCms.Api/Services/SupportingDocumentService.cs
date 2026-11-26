using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using NbcCms.Api.Data;
using NbcCms.Api.Data.Entities;
using NbcCms.Api.Models;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Services.Interfaces;
using DocEntity = NbcCms.Api.Data.Entities.SupportingDocument;
using DocResponse = NbcCms.Api.Models.Response.SupportingDocument;

namespace NbcCms.Api.Services;

public class SupportingDocumentService(
    ApplicationDbContext db,
    AuditService audit,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ISupportingDocumentService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx" };

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private long MaxUploadBytes =>
        configuration.GetValue<long?>("Cms:MaxUploadBytes") ?? 10 * 1024 * 1024;

    private string UploadRoot => Path.Combine(environment.ContentRootPath, "App_Data", "uploads");

    public async Task<List<DocResponse>> ListAsync(int claimId, int userId, bool isStaff)
    {
        var claim = await db.Claims
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClaimId == claimId)
            ?? throw ApiProblemException.NotFound("Claim not found.");

        if (!isStaff && claim.ClaimantId != userId)
        {
            throw ApiProblemException.NotFound("Claim not found.");
        }

        var documents = await db.SupportingDocuments
            .AsNoTracking()
            .Where(d => d.ClaimId == claimId && d.IsActive)
            .OrderBy(d => d.UploadedAt)
            .ToListAsync();

        return documents.Select(DocResponse.FromEntity).ToList();
    }

    public async Task<DocResponse> UploadAsync(SupportingDocumentRequest request, int userId, bool isStaff)
    {
        var claim = await db.Claims
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId)
            ?? throw ApiProblemException.NotFound("Claim not found.");

        EnsureAccess(claim, userId, isStaff, requireEditable: true);

        var file = request.File
            ?? throw ApiProblemException.BadRequest("Choose a file to upload.");

        if (file.Length == 0)
        {
            throw ApiProblemException.BadRequest("The selected file is empty.");
        }

        if (file.Length > MaxUploadBytes)
        {
            throw ApiProblemException.BadRequest(
                $"The file is too large. The maximum size is {MaxUploadBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw ApiProblemException.BadRequest(
                "Unsupported file type. Upload a PDF, PNG, JPG, DOCX or XLSX file.");
        }

        var folder = Path.Combine(UploadRoot, claim.ClaimId.ToString());
        Directory.CreateDirectory(folder);

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, storedName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var document = new DocEntity
        {
            ClaimId = claim.ClaimId,
            UploadedBy = userId,
            FileName = Path.GetFileName(file.FileName),
            FileURL = Path.Combine(claim.ClaimId.ToString(), storedName),
            UploadedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.SupportingDocuments.Add(document);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, "supportingDocument", claim.ClaimId, "DocumentUpload");

        return DocResponse.FromEntity(document);
    }

    public async Task<DocumentDownload> DownloadAsync(int documentId, int userId, bool isStaff)
    {
        var document = await db.SupportingDocuments
            .Include(d => d.Claim)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive)
            ?? throw ApiProblemException.NotFound("Document not found.");

        if (!isStaff && document.Claim.ClaimantId != userId)
        {
            throw ApiProblemException.NotFound("Document not found.");
        }

        var fullPath = Path.Combine(UploadRoot, document.FileURL);
        if (!File.Exists(fullPath))
        {
            throw ApiProblemException.NotFound("The stored file could not be found. Upload it again.");
        }

        if (!ContentTypes.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new DocumentDownload(fullPath, document.FileName, contentType);
    }

    public async Task DeleteAsync(int documentId, int userId, bool isStaff)
    {
        var document = await db.SupportingDocuments
            .Include(d => d.Claim)
            .ThenInclude(c => c.Status)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive)
            ?? throw ApiProblemException.NotFound("Document not found.");

        EnsureAccess(document.Claim, userId, isStaff, requireEditable: true);

        document.IsActive = false;
        await db.SaveChangesAsync();

        var fullPath = Path.Combine(UploadRoot, document.FileURL);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        await audit.RecordAsync(userId, "supportingDocument", document.ClaimId, "DocumentDelete");
    }

    private static void EnsureAccess(Claim claim, int userId, bool isStaff, bool requireEditable)
    {
        if (!isStaff && claim.ClaimantId != userId)
        {
            throw ApiProblemException.NotFound("Claim not found.");
        }

        if (requireEditable
            && !isStaff
            && claim.Status.StatusName is not (ClaimStatusNames.Draft or ClaimStatusNames.CorrectionRequested))
        {
            throw ApiProblemException.Conflict(
                "Documents can only be changed while the claim is a draft or awaiting correction.");
        }
    }
}
