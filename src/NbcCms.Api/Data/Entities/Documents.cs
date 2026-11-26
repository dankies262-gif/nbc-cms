namespace NbcCms.Api.Data.Entities;

public class SupportingDocument
{
    public int DocumentId { get; set; }
    public int ClaimId { get; set; }
    public int UploadedBy { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileURL { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Claim Claim { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}

public class Report
{
    public int ReportId { get; set; }
    public int ClaimId { get; set; }
    public int GeneratedBy { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileURL { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public byte[]? FileData { get; set; }

    public Claim Claim { get; set; } = null!;
    public User GeneratedByUser { get; set; } = null!;
}

public class AuditLog
{
    public int AuditId { get; set; }
    public int? UserId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int? RecordID { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}

public class ArtistBankingDetail
{
    public int BankingId { get; set; }
    public int ArtistId { get; set; }
    public string Bank { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }
    public int UpdatedBy { get; set; }

    public User Artist { get; set; } = null!;
}

public class Programme
{
    public int ProgrammeId { get; set; }
    public string ProgrammeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class OverallAmount
{
    public int CalculationId { get; set; }
    public int ClaimId { get; set; }
    public decimal Rate { get; set; }
    public decimal Quantity { get; set; }
    public decimal CalculatedAmount { get; set; }
    public int CalculatedBy { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string ItemDescription { get; set; } = "Secretary allocation";
    public int UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Claim Claim { get; set; } = null!;
}
