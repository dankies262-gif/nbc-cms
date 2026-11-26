using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class ReportRequest
{
    [Range(1, int.MaxValue)]
    public int ClaimId { get; set; }

    /// <summary>Rebuild the stored PDF when a report already exists for the claim.</summary>
    public bool Regenerate { get; set; }
}
