using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NbcCms.Api.Data.Entities;

namespace NbcCms.Api.Controllers;

[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    protected string CurrentUsername =>
        User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    protected bool IsStaff =>
        User.IsInRole(AppRoles.Secretary) || User.IsInRole(AppRoles.Admin);

    protected bool IsArtist => User.IsInRole(AppRoles.Artist);
}
