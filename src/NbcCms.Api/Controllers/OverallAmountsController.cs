using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;
using NbcCms.Api.Services.Interfaces;

namespace NbcCms.Api.Controllers;

[Route("api/overall-amounts")]
[Authorize(Policy = "Staff")]
public class OverallAmountsController(IOverallAmountService amounts) : ApiControllerBase
{
    [HttpGet]
    public Task<List<OverallAmount>> List([FromQuery] int? claimId) => amounts.ListAsync(claimId);

    [HttpPost]
    public async Task<ActionResult<OverallAmount>> Create([FromBody] OverallAmountRequest request)
    {
        var created = await amounts.CreateAsync(request, CurrentUserId);
        return Created($"/api/overall-amounts/{created.CalculationId}", created);
    }

    [HttpPut("{id:int}")]
    public Task<OverallAmount> Update(int id, [FromBody] OverallAmountRequest request)
        => amounts.UpdateAsync(id, request, CurrentUserId);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await amounts.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }
}
