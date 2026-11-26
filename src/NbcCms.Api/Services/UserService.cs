using Microsoft.EntityFrameworkCore;
using NbcCms.Api.Data;
using NbcCms.Api.Data.Entities;
using NbcCms.Api.Models;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;
using NbcCms.Api.Security;

namespace NbcCms.Api.Services;

public class UserService(ApplicationDbContext db, IPasswordHasher hasher, AuditService audit)
{
    public async Task<List<Users>> ListAsync(string? search, string? role)
    {
        var query = db.Users
            .Include(u => u.Role)
            .Include(u => u.BankingDetail)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.Username.Contains(term)
                || u.FirstName.Contains(term)
                || u.LastName.Contains(term)
                || u.Email.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            query = query.Where(u => u.Role.RoleName == roleName);
        }

        var users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var totals = await db.Claims
            .GroupBy(c => c.ClaimantId)
            .Select(g => new
            {
                UserId = g.Key,
                Count = g.Count(),
                Value = g.Sum(c => c.FeesAmount ?? 0m)
            })
            .ToDictionaryAsync(x => x.UserId);

        return users.Select(u =>
        {
            totals.TryGetValue(u.UserId, out var total);
            return Users.FromEntity(u, total?.Count ?? 0, total?.Value ?? 0m);
        }).ToList();
    }

    public async Task<Users> GetAsync(int userId)
    {
        var user = await LoadAsync(userId);
        return await MapAsync(user);
    }

    public async Task<Users> CreateAsync(UserRequest request, int actorId)
    {
        var role = await db.UserRoles.FirstOrDefaultAsync(r => r.RoleName == request.RoleName.Trim())
            ?? throw ApiProblemException.BadRequest($"Unknown role '{request.RoleName}'.");

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await db.Users.AnyAsync(u => u.Username == username))
        {
            throw ApiProblemException.Conflict("That username is already taken.");
        }

        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            throw ApiProblemException.Conflict("That email address is already in use.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw ApiProblemException.BadRequest("A password is required when creating an account.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = username,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            ContactNumber = Normalise(request.ContactNumber),
            RoleId = role.RoleId,
            IdNumber = Normalise(request.IdNumber),
            Address = Normalise(request.Address),
            IsActive = request.IsActive,
            CreatedAt = now,
            CreatedBy = actorId,
            PasswordHash = hasher.Hash(request.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        user.Role = role;
        await audit.RecordAsync(actorId, "Users", user.UserId, "UserCreate");

        return await MapAsync(user);
    }

    public async Task<Users> UpdateAsync(int userId, UserRequest request, int actorId)
    {
        var user = await LoadAsync(userId);

        var role = await db.UserRoles.FirstOrDefaultAsync(r => r.RoleName == request.RoleName.Trim())
            ?? throw ApiProblemException.BadRequest($"Unknown role '{request.RoleName}'.");

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await db.Users.AnyAsync(u => u.UserId != userId && u.Username == username))
        {
            throw ApiProblemException.Conflict("That username is already taken.");
        }

        if (await db.Users.AnyAsync(u => u.UserId != userId && u.Email == email))
        {
            throw ApiProblemException.Conflict("That email address is already in use.");
        }

        if (!request.IsActive && user.IsActive && user.UserId == actorId)
        {
            throw ApiProblemException.Conflict("You cannot deactivate your own account.");
        }

        user.Username = username;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.ContactNumber = Normalise(request.ContactNumber);
        user.RoleId = role.RoleId;
        user.IdNumber = Normalise(request.IdNumber);
        user.Address = Normalise(request.Address);
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = actorId;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = hasher.Hash(request.Password);
        }

        await db.SaveChangesAsync();
        user.Role = role;
        await audit.RecordAsync(actorId, "Users", user.UserId, "UserUpdate");

        return await MapAsync(user);
    }

    public async Task<Users> SetStatusAsync(int userId, bool isActive, int actorId)
    {
        var user = await LoadAsync(userId);

        if (!isActive && userId == actorId)
        {
            throw ApiProblemException.Conflict("You cannot deactivate your own account.");
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = actorId;
        await db.SaveChangesAsync();

        await audit.RecordAsync(actorId, "Users", user.UserId, isActive ? "UserActivate" : "UserDeactivate");
        return await MapAsync(user);
    }

    private async Task<User> LoadAsync(int userId) =>
        await db.Users
            .Include(u => u.Role)
            .Include(u => u.BankingDetail)
            .FirstOrDefaultAsync(u => u.UserId == userId)
        ?? throw ApiProblemException.NotFound("User not found.");

    private async Task<Users> MapAsync(User user)
    {
        var claims = await db.Claims
            .Where(c => c.ClaimantId == user.UserId)
            .Select(c => new { c.FeesAmount })
            .ToListAsync();

        return Users.FromEntity(user, claims.Count, claims.Sum(c => c.FeesAmount ?? 0m));
    }

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
