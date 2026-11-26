using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class LoginRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
