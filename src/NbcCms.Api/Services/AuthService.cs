using Microsoft.EntityFrameworkCore;
using NbcCms.Api.Data;
using NbcCms.Api.Data.Entities;
using NbcCms.Api.Models;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;
using NbcCms.Api.Security;

namespace NbcCms.Api.Services;

public class AuthService(ApplicationDbContext db, IPasswordHasher hasher, JwtTokenService tokens, AuditService audit)
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var username = request.Username.Trim();
        var user = await db.Users
            .Include(u => u.Role)
            .Include(u => u.BankingDetail)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            await audit.RecordAsync(null, "Users", user?.UserId, "LoginFailed");
            throw ApiProblemException.Unauthorized("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            await audit.RecordAsync(user.UserId, "Users", user.UserId, "LoginFailed");
            throw ApiProblemException.Forbidden("This account has been deactivated. Contact the Secretary's office.");
        }

        var (token, expires) = tokens.Create(user);
        await audit.RecordAsync(user.UserId, "Users", user.UserId, "Login");

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expires,
            User = await MapUserAsync(user)
        };
    }

    public async Task<AuthResponse> BootstrapAdminAsync(BootstrapAdminRequest request)
    {
        if (await db.Users.AnyAsync())
        {
            throw ApiProblemException.Conflict(
                "Bootstrap is only available before the first account exists. Sign in and create accounts through /api/users.");
        }

        var role = await db.UserRoles.FirstOrDefaultAsync(r => r.RoleName == AppRoles.Admin)
            ?? throw new InvalidOperationException("The Admin role has not been seeded.");

        var username = request.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Username == username || u.Email == request.Email.Trim()))
        {
            throw ApiProblemException.Conflict("An account with that username or email already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = username,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            RoleId = role.RoleId,
            IsActive = true,
            CreatedAt = now,
            PasswordHash = hasher.Hash(request.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        user.Role = role;
        await audit.RecordAsync(user.UserId, "Users", user.UserId, "UserCreate");

        var (token, expires) = tokens.Create(user);
        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expires,
            User = await MapUserAsync(user)
        };
    }

    public async Task<Users> GetProfileAsync(int userId)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .Include(u => u.BankingDetail)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw ApiProblemException.NotFound("The signed-in account no longer exists.");

        return await MapUserAsync(user);
    }

    private async Task<Users> MapUserAsync(User user)
    {
        var claims = await db.Claims
            .Where(c => c.ClaimantId == user.UserId)
            .Select(c => new { c.FeesAmount })
            .ToListAsync();

        return Users.FromEntity(user, claims.Count, claims.Sum(c => c.FeesAmount ?? 0m));
    }
}
