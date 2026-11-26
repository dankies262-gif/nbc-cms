using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Services;
using ClaimDto = NbcCms.Api.Models.Response.Claim;

namespace NbcCms.Api.Controllers;

[Route("api/claims")]
public class ClaimController(ClaimService claims) : ApiControllerBase
{
    /// <summary>
    /// Lists claims. Artists see only their own; Secretary/Admin see all.
    /// <paramref name="scope"/> may be review, corrections, final or draft.
    /// </summary>
    [HttpGet]
    public Task<List<ClaimDto>> List(
        [FromQuery] string? status,
        [FromQuery] string? scope,
        [FromQuery] string? search)
        => claims.ListAsync(CurrentUserId, IsStaff, status, scope, search);

    [HttpGet("{id:int}")]
    public Task<ClaimDto> Get(int id) => claims.GetAsync(id, CurrentUserId, IsStaff);

    /// <summary>Create a draft claim with its work records.</summary>
    [HttpPost]
    [Authorize(Policy = "ArtistOnly")]
    public async Task<ActionResult<ClaimDto>> Create([FromBody] ClaimRequest request)
    {
        var created = await claims.CreateAsync(request, CurrentUserId);
        return Created($"/api/claims/{created.ClaimId}", created);
    }

    /// <summary>Update a draft claim or a claim that is awaiting correction.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ArtistOnly")]
    public Task<ClaimDto> Update(int id, [FromBody] ClaimRequest request)
        => claims.UpdateAsync(id, request, CurrentUserId);

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ArtistOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        await claims.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }

    /// <summary>Submit a draft, or resubmit a claim after corrections.</summary>
    [HttpPost("{id:int}/submit")]
    [Authorize(Policy = "ArtistOnly")]
    public Task<ClaimDto> Submit(int id) => claims.SubmitAsync(id, CurrentUserId);

    /// <summary>Accept, decline or request a correction on a submitted claim.</summary>
    [HttpPost("{id:int}/review")]
    [Authorize(Policy = "Staff")]
    public Task<ClaimDto> Review(int id, [FromBody] ClaimReviewRequest request)
        => claims.ReviewAsync(id, request, CurrentUserId);
}
