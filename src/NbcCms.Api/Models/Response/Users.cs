using NbcCms.Api.Data.Entities;
using BankingEntity = NbcCms.Api.Data.Entities.ArtistBankingDetail;
using UserEntity = NbcCms.Api.Data.Entities.User;

namespace NbcCms.Api.Models.Response;

public class Users
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? IdNumber { get; set; }
    public string? Address { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ClaimCount { get; set; }
    public decimal ClaimValue { get; set; }
    public ArtistBankingDetails? Banking { get; set; }

    public static Users FromEntity(UserEntity user, int claimCount = 0, decimal claimValue = 0m) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        Email = user.Email,
        ContactNumber = user.ContactNumber,
        IdNumber = user.IdNumber,
        Address = user.Address,
        RoleId = user.RoleId,
        RoleName = user.Role?.RoleName ?? string.Empty,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        ClaimCount = claimCount,
        ClaimValue = claimValue,
        Banking = user.BankingDetail is null ? null : ArtistBankingDetails.FromEntity(user.BankingDetail)
    };
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAtUtc { get; set; }
    public Users User { get; set; } = null!;
}
