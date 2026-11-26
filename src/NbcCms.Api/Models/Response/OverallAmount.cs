using AmountEntity = NbcCms.Api.Data.Entities.OverallAmount;

namespace NbcCms.Api.Models.Response;

public class OverallAmount
{
    public int CalculationId { get; set; }
    public int ClaimId { get; set; }
    public string? ClaimReference { get; set; }
    public decimal Rate { get; set; }
    public decimal Quantity { get; set; }
    public decimal CalculatedAmount { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static OverallAmount FromEntity(AmountEntity amount) => new()
    {
        CalculationId = amount.CalculationId,
        ClaimId = amount.ClaimId,
        ClaimReference = amount.Claim?.Reference,
        Rate = amount.Rate,
        Quantity = amount.Quantity,
        CalculatedAmount = amount.CalculatedAmount,
        ItemDescription = amount.ItemDescription,
        CalculatedAt = amount.CalculatedAt,
        UpdatedAt = amount.UpdatedAt
    };
}
