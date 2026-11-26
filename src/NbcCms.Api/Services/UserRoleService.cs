using Microsoft.EntityFrameworkCore;
using NbcCms.Api.Data;
using NbcCms.Api.Data.Entities;
using NbcCms.Api.Models;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;
using RoleDto = NbcCms.Api.Models.Response.UserRole;
using UserRoleEntity = NbcCms.Api.Data.Entities.UserRole;

namespace NbcCms.Api.Services;

public class UserRoleService(ApplicationDbContext db, AuditService audit)
{
    public async Task<List<RoleDto>> ListAsync()
    {
        var roles = await db.UserRoles.OrderBy(r => r.RoleName).ToListAsync();
        var counts = await db.Users
            .GroupBy(u => u.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count);

        return roles.Select(r => RoleDto.FromEntity(r, counts.GetValueOrDefault(r.RoleId))).ToList();
    }

    public async Task<RoleDto> CreateAsync(UserRoleRequest request, int actorId)
    {
        var name = request.RoleName.Trim();
        if (await db.UserRoles.AnyAsync(r => r.RoleName == name))
        {
            throw ApiProblemException.Conflict($"A role named '{name}' already exists.");
        }

        var role = new UserRoleEntity { RoleName = name, Description = Normalise(request.Description) };
        db.UserRoles.Add(role);
        await db.SaveChangesAsync();
        await audit.RecordAsync(actorId, "userRole", role.RoleId, "UserRoleCreate");

        return RoleDto.FromEntity(role);
    }

    public async Task<RoleDto> UpdateAsync(int roleId, UserRoleRequest request, int actorId)
    {
        var role = await db.UserRoles.FirstOrDefaultAsync(r => r.RoleId == roleId)
            ?? throw ApiProblemException.NotFound("Role not found.");

        var name = request.RoleName.Trim();
        if (!string.Equals(role.RoleName, name, StringComparison.OrdinalIgnoreCase))
        {
            if (AppRoles.Staff.Contains(role.RoleName, StringComparer.OrdinalIgnoreCase)
                || role.RoleName.Equals(AppRoles.Artist, StringComparison.OrdinalIgnoreCase))
            {
                throw ApiProblemException.Conflict("System roles cannot be renamed.");
            }

            if (await db.UserRoles.AnyAsync(r => r.RoleId != roleId && r.RoleName == name))
            {
                throw ApiProblemException.Conflict($"A role named '{name}' already exists.");
            }
        }

        role.RoleName = name;
        role.Description = Normalise(request.Description);
        await db.SaveChangesAsync();
        await audit.RecordAsync(actorId, "userRole", role.RoleId, "UserRoleUpdate");

        var count = await db.Users.CountAsync(u => u.RoleId == role.RoleId);
        return RoleDto.FromEntity(role, count);
    }

    public async Task DeleteAsync(int roleId, int actorId)
    {
        var role = await db.UserRoles.FirstOrDefaultAsync(r => r.RoleId == roleId)
            ?? throw ApiProblemException.NotFound("Role not found.");

        if (AppRoles.Staff.Contains(role.RoleName, StringComparer.OrdinalIgnoreCase)
            || role.RoleName.Equals(AppRoles.Artist, StringComparison.OrdinalIgnoreCase))
        {
            throw ApiProblemException.Conflict("System roles cannot be removed.");
        }

        if (await db.Users.AnyAsync(u => u.RoleId == roleId))
        {
            throw ApiProblemException.Conflict("Reassign the accounts that use this role before removing it.");
        }

        db.UserRoles.Remove(role);
        await db.SaveChangesAsync();
        await audit.RecordAsync(actorId, "userRole", roleId, "UserRoleDelete");
    }

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
