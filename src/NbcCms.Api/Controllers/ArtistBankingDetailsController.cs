using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;
using NbcCms.Api.Services;

namespace NbcCms.Api.Controllers;

[Route("api/artist-banking-details")]
public class ArtistBankingDetailsController(ArtistBankingDetailsService banking) : ApiControllerBase
{
    /// <summary>Banking details of the signed-in artist.</summary>
    [HttpGet]
    [Authorize(Policy = "ArtistOnly")]
    public Task<ArtistBankingDetails> GetOwn() => banking.GetAsync(CurrentUserId, CurrentUserId, isStaff: false);

    /// <summary>Banking details for a specific artist (Secretary/Admin only).</summary>
    [HttpGet("{artistId:int}")]
    [Authorize(Policy = "Staff")]
    public Task<ArtistBankingDetails> Get(int artistId)
        => banking.GetAsync(artistId, CurrentUserId, isStaff: true);

    /// <summary>Create or replace an artist's banking details (Secretary/Admin only).</summary>
    [HttpPut("{artistId:int}")]
    [Authorize(Policy = "Staff")]
    public Task<ArtistBankingDetails> Upsert(int artistId, [FromBody] ArtistBankingDetailsRequest request)
        => banking.UpsertAsync(artistId, request, CurrentUserId, isStaff: true);
}
