using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

public class ListeningController : Controller
{
    [HttpGet("/listening")]
    [HttpGet("/vi/listening")]
    public IActionResult Index() => View();
}
