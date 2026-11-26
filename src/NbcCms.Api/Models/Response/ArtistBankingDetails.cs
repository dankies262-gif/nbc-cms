using BankingEntity = NbcCms.Api.Data.Entities.ArtistBankingDetail;

namespace NbcCms.Api.Models.Response;

public class ArtistBankingDetails
{
    public int BankingId { get; set; }
    public int ArtistId { get; set; }
    public string Bank { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ArtistBankingDetails FromEntity(BankingEntity banking) => new()
    {
        BankingId = banking.BankingId,
        ArtistId = banking.ArtistId,
        Bank = banking.Bank,
        Branch = banking.Branch,
        BranchCode = banking.BranchCode,
        CreatedAt = banking.CreatedAt,
        UpdatedAt = banking.UpdatedAt
    };
}
