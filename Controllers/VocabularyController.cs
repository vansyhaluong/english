using English.Models;
using English.Data;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace English.Controllers;

public class VocabularyController : Controller
{
    [HttpGet("/vocabulary")]
    [HttpGet("/vi/vocabulary")]
    public IActionResult Index(string? level, string? search, int page = 1)
    {
        level = level?.ToUpperInvariant();
        if (!new[] { "A1", "A2", "B1", "B2", "C1", "C2" }.Contains(level)) level = null;
        search = search?.Trim();
        var compare = CultureInfo.GetCultureInfo("vi-VN").CompareInfo;
        bool Matches(string text) => string.IsNullOrEmpty(search) || compare.IndexOf(text, search, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
        var decks = VocabularySamples.Decks.Where(d => (level == null || d.Level == level) &&
            (Matches(d.Title) || Matches(d.Description) || d.Words.Any(w => Matches(w.Word) || Matches(w.Meaning)))).ToArray();
        var pages = Math.Max(1, (int)Math.Ceiling(decks.Length / 12d));
        page = Math.Clamp(page, 1, pages);
        return View(new VocabularyViewModel(decks.Skip((page - 1) * 12).Take(12).ToArray(), level, search, page, pages, decks.Length));
    }
}
