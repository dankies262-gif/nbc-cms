namespace NbcCms.Api.Data.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public string? IdNumber { get; set; }
    public string? Address { get; set; }
    public int? CreatedBy { get; set; }
    public string? PasswordHash { get; set; }

    public UserRole Role { get; set; } = null!;
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    public ArtistBankingDetail? BankingDetail { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class UserRole
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}

public static class AppRoles
{
    public const string Artist = "Artist";
    public const string Secretary = "Secretary";
    public const string Admin = "Admin";

    public static readonly string[] Staff = [Secretary, Admin];
}
