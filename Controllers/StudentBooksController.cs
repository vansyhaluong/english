using English.Authorization;
using English.Models.ViewModels.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace English.Controllers;

[Authorize(Policy = AuthorizationPolicies.StudentOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("/books/library")]
public sealed class StudentBooksController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet("{bookId:int}/read")]
    public IActionResult Read(int bookId, int? chapterId)
    {
        if (!ModelState.IsValid || bookId <= 0 || chapterId is <= 0) return BadRequest();
        return View(new StudentBookReaderPageModel(bookId, chapterId));
    }
}
