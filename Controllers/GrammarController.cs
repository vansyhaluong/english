using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

public class GrammarController : Controller
{
    [HttpGet("/grammar")]
    [HttpGet("/vi/grammar")]
    public IActionResult Index() => View();
}
