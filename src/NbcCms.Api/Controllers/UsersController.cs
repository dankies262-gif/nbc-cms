using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Api.Models.Requests;
using UserDto = NbcCms.Api.Models.Response.Users;
using NbcCms.Api.Services;

namespace NbcCms.Api.Controllers;

[Route("api/users")]
[Authorize(Policy = "Staff")]
public class UsersController(UserService users) : ApiControllerBase
{
    [HttpGet]
    public Task<List<UserDto>> List(
        [FromQuery] string? search,
        [FromQuery] string? role) => users.ListAsync(search, role);

    [HttpGet("{id:int}")]
    public async Task<UserDto> Get(int id) => await users.GetAsync(id);

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] UserRequest request)
    {
        var created = await users.CreateAsync(request, CurrentUserId);
        return Created($"/api/users/{created.UserId}", created);
    }

    [HttpPut("{id:int}")]
    public Task<UserDto> Update(int id, [FromBody] UserRequest request)
        => users.UpdateAsync(id, request, CurrentUserId);

    [HttpPatch("{id:int}/status")]
    public Task<UserDto> SetStatus(int id, [FromBody] UpdateUserStatusRequest request)
        => users.SetStatusAsync(id, request.IsActive, CurrentUserId);
}
