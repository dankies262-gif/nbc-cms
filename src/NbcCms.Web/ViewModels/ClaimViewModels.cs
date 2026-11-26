using System.ComponentModel.DataAnnotations;
using NbcCms.Web.Data.Entities;

namespace NbcCms.Web.ViewModels;

public class ClaimItemInput
{
    [Required(ErrorMessage = "Choose the recording date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Recording date")]
    public DateTime? RecordingDate { get; set; }

    [Required(ErrorMessage = "Choose a programme.")]
    [StringLength(250)]
    [Display(Name = "Programme")]
    public string ProgrammeTitle { get; set; } = string.Empty;

    [Range(0.01, 10_000_000, ErrorMessage = "Enter a fee greater than zero.")]
    [Display(Name = "Fee (N$)")]
    public decimal? Rate { get; set; }
}

public class ClaimFormViewModel
{
    public int? ClaimId { get; set; }
    public string? Reference { get; set; }
    public string? Status { get; set; }
    public bool IsCorrection { get; set; }
    public string? CorrectionComment { get; set; }

    [Required(ErrorMessage = "Choose the claim date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Claim date")]
    public DateTime ClaimDate { get; set; } = DateTime.Today;

    [MinLength(1, ErrorMessage = "Add at least one work record to the claim.")]
    public List<ClaimItemInput> Items { get; set; } = [new ClaimItemInput()];

    public List<Programme> Programmes { get; set; } = [];
}

public class ClaimListItemViewModel
{
    public int ClaimId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ClaimantName { get; set; } = string.Empty;
    public string? ClaimantEmail { get; set; }
    public DateTime ClaimDate { get; set; }
    public string ProgrammeTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public int DocumentCount { get; set; }
    public bool HasOpenCorrection { get; set; }
    public bool HasReport { get; set; }
}

public class ClaimsIndexViewModel
{
    public List<ClaimListItemViewModel> Claims { get; set; } = [];
    public List<ClaimStatus> Statuses { get; set; } = [];
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Scope { get; set; }
    public bool IsStaffView { get; set; }
    public int TotalCount { get; set; }
    public int AwaitingReviewCount { get; set; }
    public int CorrectionCount { get; set; }
    public int AcceptedCount { get; set; }
    public decimal TotalValue { get; set; }
}

public class ClaimDetailsViewModel
{
    public Claim Claim { get; set; } = null!;
    public List<ClaimItem> Items { get; set; } = [];
    public List<ClaimCorrection> Corrections { get; set; } = [];
    public List<SupportingDocument> Documents { get; set; } = [];
    public Report? Report { get; set; }
    public ClaimCorrection? OpenCorrection { get; set; }
    public bool CanEdit { get; set; }
    public bool CanReview { get; set; }
    public bool CanGenerateReport { get; set; }
    public bool CanUpload { get; set; }
    public long MaxUploadMegabytes { get; set; }
    public string[] AllowedExtensions { get; set; } = [];
}

public class DashboardViewModel
{
    public string Role { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsStaff { get; set; }

    public int TotalClaims { get; set; }
    public int Drafts { get; set; }
    public int AwaitingReview { get; set; }
    public int AwaitingArtist { get; set; }
    public int Accepted { get; set; }
    public int Declined { get; set; }
    public int ReportsGenerated { get; set; }
    public int ActiveArtists { get; set; }
    public int ActiveProgrammes { get; set; }
    public int Documents { get; set; }

    public decimal TotalValue { get; set; }
    public decimal ApprovedValue { get; set; }
    public decimal PendingValue { get; set; }
    public decimal DeclinedValue { get; set; }

    public List<ClaimListItemViewModel> RecentClaims { get; set; } = [];
    public List<ClaimListItemViewModel> NeedsAttention { get; set; } = [];
    public List<AuditLog> RecentActivity { get; set; } = [];
}

public class ArtistFormViewModel
{
    public int? UserId { get; set; }
    public bool IsEdit => UserId.HasValue;

    [Required(ErrorMessage = "Enter a first name.")]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a surname.")]
    [StringLength(100)]
    [Display(Name = "Surname")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a username.")]
    [StringLength(150, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9._-]+$", ErrorMessage = "Use letters, numbers, dots, dashes or underscores only.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [StringLength(50)]
    [Display(Name = "ID number")]
    public string? IdNumber { get; set; }

    [StringLength(250)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "Active account")]
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string? ConfirmPassword { get; set; }

    [StringLength(100)]
    public string? Bank { get; set; }

    [StringLength(100)]
    public string? Branch { get; set; }

    [StringLength(50)]
    [Display(Name = "Branch code")]
    public string? BranchCode { get; set; }
}

public class ArtistListItemViewModel
{
    public User User { get; set; } = null!;
    public int ClaimCount { get; set; }
    public decimal TotalClaimed { get; set; }
    public DateTime? LastClaimAt { get; set; }
}

public class ProgrammeFormViewModel
{
    public int? ProgrammeId { get; set; }

    [Required(ErrorMessage = "Enter the programme name.")]
    [StringLength(200)]
    [Display(Name = "Programme name")]
    public string ProgrammeName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class ProgrammesPageViewModel
{
    public List<Programme> Programmes { get; set; } = [];
    public ProgrammeFormViewModel Form { get; set; } = new();
    public int ClaimUsage { get; set; }
}

public class ReportsIndexViewModel
{
    public List<ClaimListItemViewModel> ReadyForReport { get; set; } = [];
    public List<Report> Generated { get; set; } = [];
}
