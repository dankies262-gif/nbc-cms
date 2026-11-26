using Microsoft.EntityFrameworkCore;
using NbcCms.Api.Data;
using NbcCms.Api.Data.Entities;
using NbcCms.Api.Models;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;

namespace NbcCms.Api.Services;

public class ArtistBankingDetailsService(ApplicationDbContext db, AuditService audit)
{
    public async Task<ArtistBankingDetails> GetAsync(int artistId, int currentUserId, bool isStaff)
    {
        if (!isStaff && artistId != currentUserId)
        {
            throw ApiProblemException.Forbidden("You can only view your own banking details.");
        }

        var artist = await LoadArtistAsync(artistId);
        if (artist.BankingDetail is null)
        {
            throw ApiProblemException.NotFound("No banking details are on record for this artist.");
        }

        return ArtistBankingDetails.FromEntity(artist.BankingDetail);
    }

    public async Task<ArtistBankingDetails> UpsertAsync(
        int artistId,
        ArtistBankingDetailsRequest request,
        int actorId,
        bool isStaff)
    {
        if (!isStaff)
        {
            throw ApiProblemException.Forbidden("Only the Secretary's office can change banking details.");
        }

        var artist = await LoadArtistAsync(artistId);
        var now = DateTime.UtcNow;

        if (artist.BankingDetail is null)
        {
            artist.BankingDetail = new ArtistBankingDetail
            {
                ArtistId = artistId,
                Bank = request.Bank.Trim(),
                Branch = request.Branch.Trim(),
                BranchCode = request.BranchCode.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = actorId,
                UpdatedBy = actorId
            };
        }
        else
        {
            artist.BankingDetail.Bank = request.Bank.Trim();
            artist.BankingDetail.Branch = request.Branch.Trim();
            artist.BankingDetail.BranchCode = request.BranchCode.Trim();
            artist.BankingDetail.UpdatedAt = now;
            artist.BankingDetail.UpdatedBy = actorId;
        }

        await db.SaveChangesAsync();
        await audit.RecordAsync(actorId, "artistBankingDetails", artist.BankingDetail.BankingId, "BankingUpdate");

        return ArtistBankingDetails.FromEntity(artist.BankingDetail);
    }

    private async Task<User> LoadArtistAsync(int artistId)
    {
        var artist = await db.Users
            .Include(u => u.Role)
            .Include(u => u.BankingDetail)
            .FirstOrDefaultAsync(u => u.UserId == artistId);

        if (artist is null || !artist.Role.RoleName.Equals(AppRoles.Artist, StringComparison.OrdinalIgnoreCase))
        {
            throw ApiProblemException.NotFound("Artist not found.");
        }

        return artist;
    }
}
