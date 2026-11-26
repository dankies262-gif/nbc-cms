using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class ClaimItemRequest
{
    [Required]
    public DateTime RecordingDate { get; set; }

    [Required, StringLength(250, MinimumLength = 2)]
    public string ProgrammeTitle { get; set; } = string.Empty;

    [Range(0.01, 10_000_000, ErrorMessage = "Rate must be greater than zero.")]
    public decimal Rate { get; set; }
}

public class ClaimRequest
{
    [Required]
    public DateTime ClaimDate { get; set; } = DateTime.Today;

    /// <summary>Optional free-text label kept on the claim header; derived from the items when omitted.</summary>
    [StringLength(500)]
    public string? ProgrammeTitle { get; set; }

    [Required, MinLength(1, ErrorMessage = "A claim needs at least one work record.")]
    public List<ClaimItemRequest> Items { get; set; } = [];
}
