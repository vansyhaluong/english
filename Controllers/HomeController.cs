using English.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return RenderError(500);
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int code)
    {
        return RenderError(code is 403 or 404 ? code : 404);
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult AccessDenied()
    {
        return RenderError(403);
    }

    private IActionResult RenderError(int statusCode)
    {
        Response.StatusCode = statusCode;
        return View("Error", new ErrorViewModel { StatusCode = statusCode });
    }
}
