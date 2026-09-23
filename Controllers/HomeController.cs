using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using English.Data;

namespace EnglishHub.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var levels = await _context.Levels
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        foreach (var level in levels)
        {
            Console.WriteLine($"{level.Id} - {level.Code}");
        }

        return View();
    }
}
