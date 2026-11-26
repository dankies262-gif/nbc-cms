using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class UserRoleRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }
}
