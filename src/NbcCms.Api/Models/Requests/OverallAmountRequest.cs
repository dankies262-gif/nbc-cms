using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class OverallAmountRequest
{
    [Range(1, int.MaxValue)]
    public int ClaimId { get; set; }

    [Range(0.01, 10_000_000)]
    public decimal Rate { get; set; }

    [Range(0.01, 10_000)]
    public decimal Quantity { get; set; } = 1;

    [StringLength(250)]
    public string? ItemDescription { get; set; }
}
