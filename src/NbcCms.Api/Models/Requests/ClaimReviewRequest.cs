using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class ClaimReviewRequest
{
    /// <summary>accept, decline or correction.</summary>
    [Required, StringLength(20)]
    public string Decision { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Comment { get; set; }
}
