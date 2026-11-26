using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Api.Models.Requests;
using NbcCms.Api.Models.Response;
using NbcCms.Api.Services;

namespace NbcCms.Api.Controllers;

[Route("api/auth")]
public class AuthController(AuthService auth) : ApiControllerBase
{
    /// <summary>Sign in and receive a JWT bearer token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<AuthResponse> Login([FromBody] LoginRequest request) => auth.LoginAsync(request);

    /// <summary>Create the first administrator. Only available while the Users table is empty.</summary>
    [HttpPost("bootstrap-admin")]
    [AllowAnonymous]
    public Task<AuthResponse> BootstrapAdmin([FromBody] BootstrapAdminRequest request)
        => auth.BootstrapAdminAsync(request);

    /// <summary>Profile of the signed-in user.</summary>
    [HttpGet("me")]
    public Task<Users> Me() => auth.GetProfileAsync(CurrentUserId);
}
