using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class BootstrapAdminRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
}
