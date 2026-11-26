using NbcCms.Api.Models.Requests;
using AmountDto = NbcCms.Api.Models.Response.OverallAmount;

namespace NbcCms.Api.Services.Interfaces;

public interface IOverallAmountService
{
    Task<List<AmountDto>> ListAsync(int? claimId);
    Task<AmountDto> CreateAsync(OverallAmountRequest request, int userId);
    Task<AmountDto> UpdateAsync(int calculationId, OverallAmountRequest request, int userId);
    Task DeleteAsync(int calculationId, int userId);
}
