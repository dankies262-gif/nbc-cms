using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Api.Models.Requests;
using RoleDto = NbcCms.Api.Models.Response.UserRole;
using NbcCms.Api.Services;

namespace NbcCms.Api.Controllers;

[Route("api/user-roles")]
[Authorize(Policy = "Staff")]
public class UserRolesController(UserRoleService roles) : ApiControllerBase
{
    [HttpGet]
    public Task<List<RoleDto>> List() => roles.ListAsync();

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] UserRoleRequest request)
    {
        var created = await roles.CreateAsync(request, CurrentUserId);
        return Created($"/api/user-roles/{created.RoleId}", created);
    }

    [HttpPut("{id:int}")]
    public Task<RoleDto> Update(int id, [FromBody] UserRoleRequest request)
        => roles.UpdateAsync(id, request, CurrentUserId);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await roles.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }
}
