using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NbcCms.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("Error")]
    public IActionResult Index()
    {
        ViewData["StatusCode"] = 500;
        return View("~/Views/Shared/Error.cshtml");
    }

    [Route("Error/{statusCode:int}")]
    public IActionResult StatusCodeHandler(int statusCode)
    {
        ViewData["StatusCode"] = statusCode;
        return View("~/Views/Shared/Error.cshtml");
    }
}
