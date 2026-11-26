using Microsoft.EntityFrameworkCore;
using NbcCms.Api.Data;
using NbcCms.Api.Models;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Services.Interfaces;
using AmountDto = NbcCms.Api.Models.Response.OverallAmount;
using AmountEntity = NbcCms.Api.Data.Entities.OverallAmount;
using ClaimEntity = NbcCms.Api.Data.Entities.Claim;

namespace NbcCms.Api.Services;

public class OverallAmountService(ApplicationDbContext db, AuditService audit) : IOverallAmountService
{
    public async Task<List<AmountDto>> ListAsync(int? claimId)
    {
        var query = db.OverallAmounts
            .Include(a => a.Claim)
            .AsQueryable();

        if (claimId is > 0)
        {
            query = query.Where(a => a.ClaimId == claimId);
        }

        var amounts = await query
            .OrderByDescending(a => a.CalculatedAt)
            .ToListAsync();

        return amounts.Select(AmountDto.FromEntity).ToList();
    }

    public async Task<AmountDto> CreateAsync(OverallAmountRequest request, int userId)
    {
        var claim = await EnsureClaimAsync(request.ClaimId);

        var now = DateTime.UtcNow;
        var amount = new AmountEntity
        {
            ClaimId = request.ClaimId,
            Rate = request.Rate,
            Quantity = request.Quantity,
            CalculatedAmount = decimal.Round(request.Rate * request.Quantity, 2),
            ItemDescription = string.IsNullOrWhiteSpace(request.ItemDescription)
                ? "Secretary allocation"
                : request.ItemDescription.Trim(),
            Claim = claim,
            CalculatedBy = userId,
            CalculatedAt = now,
            UpdatedBy = userId,
            UpdatedAt = now
        };

        db.OverallAmounts.Add(amount);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, "overallAmount", amount.CalculationId, "OverallAmount");

        return AmountDto.FromEntity(amount);
    }

    public async Task<AmountDto> UpdateAsync(int calculationId, OverallAmountRequest request, int userId)
    {
        var amount = await db.OverallAmounts
            .Include(a => a.Claim)
            .FirstOrDefaultAsync(a => a.CalculationId == calculationId)
            ?? throw ApiProblemException.NotFound("Overall amount not found.");

        amount.Claim = await EnsureClaimAsync(request.ClaimId);

        amount.ClaimId = request.ClaimId;
        amount.Rate = request.Rate;
        amount.Quantity = request.Quantity;
        amount.CalculatedAmount = decimal.Round(request.Rate * request.Quantity, 2);
        amount.ItemDescription = string.IsNullOrWhiteSpace(request.ItemDescription)
            ? "Secretary allocation"
            : request.ItemDescription.Trim();
        amount.UpdatedBy = userId;
        amount.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, "overallAmount", amount.CalculationId, "OverallAmountUpdate");

        return AmountDto.FromEntity(amount);
    }

    public async Task DeleteAsync(int calculationId, int userId)
    {
        var amount = await db.OverallAmounts
            .Include(a => a.Claim)
            .FirstOrDefaultAsync(a => a.CalculationId == calculationId)
            ?? throw ApiProblemException.NotFound("Overall amount not found.");

        db.OverallAmounts.Remove(amount);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, "overallAmount", calculationId, "OverallAmountDelete");
    }

    private async Task<ClaimEntity> EnsureClaimAsync(int claimId)
    {
        return await db.Claims.FirstOrDefaultAsync(c => c.ClaimId == claimId)
            ?? throw ApiProblemException.NotFound("Claim not found.");
    }
}
