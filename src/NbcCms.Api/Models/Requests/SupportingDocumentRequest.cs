using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class SupportingDocumentRequest
{
    [Range(1, int.MaxValue)]
    public int ClaimId { get; set; }

    [Required(ErrorMessage = "Choose a file to upload.")]
    public IFormFile? File { get; set; }
}
