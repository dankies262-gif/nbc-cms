using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class UserRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? ContactNumber { get; set; }

    [Required, StringLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? IdNumber { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    /// <summary>Required when creating an account; optional (password reset) when updating.</summary>
    [StringLength(200, MinimumLength = 8)]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}
