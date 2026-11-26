using RoleEntity = NbcCms.Api.Data.Entities.UserRole;

namespace NbcCms.Api.Models.Response;

public class UserRole
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }

    public static UserRole FromEntity(RoleEntity role, int userCount = 0) => new()
    {
        RoleId = role.RoleId,
        RoleName = role.RoleName,
        Description = role.Description,
        UserCount = userCount
    };
}
