namespace NbcCms.Web.Data.Entities;

public class Claim
{
    public int ClaimId { get; set; }
    public int ClaimantId { get; set; }
    public int StatusId { get; set; }
    public DateTime ClaimDate { get; set; } = DateTime.Today;
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? RecordingDate { get; set; }
    public string? ProgrammeTitle { get; set; }
    public decimal? FeesAmount { get; set; }
    public string? RejectionComment { get; set; }

    public User Claimant { get; set; } = null!;
    public ClaimStatus Status { get; set; } = null!;
    public User? Reviewer { get; set; }
    public ICollection<ClaimItem> Items { get; set; } = new List<ClaimItem>();
    public ICollection<ClaimCorrection> Corrections { get; set; } = new List<ClaimCorrection>();
    public ICollection<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();
    public Report? Report { get; set; }

    public string Reference => $"CLM-{ClaimId:D6}";
}

public class ClaimItem
{
    public int ClaimItemId { get; set; }
    public int ClaimId { get; set; }
    public DateTime RecordingDate { get; set; }
    public string ProgrammeTitle { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal FeesAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Claim Claim { get; set; } = null!;
}

public class ClaimStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
}

public class ClaimCorrection
{
    public int CorrectionId { get; set; }
    public int ClaimId { get; set; }
    public int RequestedBy { get; set; }
    public string CorrectionComment { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CorrectedAt { get; set; }

    public Claim Claim { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
}

public static class ClaimStatusNames
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string CorrectionRequested = "Request Submission Correction";
    public const string Resubmitted = "Resubmitted";
    public const string Accepted = "Accepted";
    public const string Declined = "Declined";

    public static readonly string[] AwaitingReview = [Submitted, Resubmitted];
    public static readonly string[] AwaitingArtist = [CorrectionRequested];
    public static readonly string[] Open = [Draft, Submitted, CorrectionRequested, Resubmitted];

    public static bool IsFinal(string status) => status is Accepted or Declined;

    public static string Slug(string status) => status switch
    {
        "Request Submission Correction" => "correction",
        _ => status.ToLowerInvariant()
    };
}
