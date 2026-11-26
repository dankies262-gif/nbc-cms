using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data;
using NbcCms.Web.Data.Entities;
using NbcCms.Web.Services;

namespace NbcCms.Web.Controllers;

[Authorize]
public class DocumentsController(CmsDbContext db, DocumentStorage storage) : AppController
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    [HttpPost]
    public async Task<IActionResult> Upload(int claimId, IFormFile? file)
    {
        var claim = await db.Claims
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim is null)
        {
            return NotFound();
        }

        if (!IsStaff && claim.ClaimantId != CurrentUserId)
        {
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            FlashError("Choose a file to upload.");
            return RedirectToAction("Details", "Claims", new { id = claimId });
        }

        var status = claim.Status.StatusName;
        if (!IsStaff && status is not (ClaimStatusNames.Draft or ClaimStatusNames.CorrectionRequested))
        {
            FlashError("Supporting documents can only be added while the claim is a draft or awaiting correction.");
            return RedirectToAction("Details", "Claims", new { id = claimId });
        }

        try
        {
            var (relativePath, originalName) = await storage.SaveAsync(claimId, file);

            db.SupportingDocuments.Add(new SupportingDocument
            {
                ClaimId = claimId,
                UploadedBy = CurrentUserId,
                FileName = originalName,
                FileURL = relativePath,
                UploadedAt = DateTime.UtcNow,
                IsActive = true
            });

            await db.SaveChangesAsync();
            await LogAuditAsync(db, "supportingDocument", claimId, "DocumentUpload");
            FlashSuccess($"'{originalName}' was attached to the claim.");
        }
        catch (InvalidOperationException ex)
        {
            FlashError(ex.Message);
        }

        return RedirectToAction("Details", "Claims", new { id = claimId });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var document = await db.SupportingDocuments
            .Include(d => d.Claim)
            .FirstOrDefaultAsync(d => d.DocumentId == id && d.IsActive);

        if (document is null)
        {
            return NotFound();
        }

        if (!IsStaff && document.Claim.ClaimantId != CurrentUserId)
        {
            return Forbid();
        }

        var path = storage.ResolvePath(document.FileURL);
        if (path is null)
        {
            FlashError("The stored file could not be found. Please ask the Secretary to upload it again.");
            return RedirectToAction("Details", "Claims", new { id = document.ClaimId });
        }

        if (!ContentTypes.TryGetContentType(path, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(path, contentType, document.FileName);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await db.SupportingDocuments
            .Include(d => d.Claim)
            .ThenInclude(c => c.Status)
            .FirstOrDefaultAsync(d => d.DocumentId == id && d.IsActive);

        if (document is null)
        {
            return NotFound();
        }

        var isOwner = document.Claim.ClaimantId == CurrentUserId;
        if (!IsStaff && !isOwner)
        {
            return Forbid();
        }

        if (!IsStaff && document.Claim.Status.StatusName is not (ClaimStatusNames.Draft or ClaimStatusNames.CorrectionRequested))
        {
            FlashError("Documents can only be removed while the claim is a draft or awaiting correction.");
            return RedirectToAction("Details", "Claims", new { id = document.ClaimId });
        }

        document.IsActive = false;
        await db.SaveChangesAsync();
        storage.Delete(document.FileURL);
        await LogAuditAsync(db, "supportingDocument", document.ClaimId, "DocumentDelete");

        FlashSuccess("The supporting document was removed.");
        return RedirectToAction("Details", "Claims", new { id = document.ClaimId });
    }
}
