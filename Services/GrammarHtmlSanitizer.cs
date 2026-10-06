using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace English.Services;

// Grammar-only policy. No attributes, URLs, CSS, embedded media or arbitrary tags.
public sealed class GrammarHtmlSanitizer
{
    private readonly HtmlSanitizer sanitizer;
    public GrammarHtmlSanitizer()
    {
        sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "br", "strong", "b", "em", "i", "ul", "ol", "li", "table", "thead", "tbody", "tr", "th", "td" })
            sanitizer.AllowedTags.Add(tag);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedAtRules.Clear();
        sanitizer.AllowedSchemes.Clear();
        sanitizer.KeepChildNodes = false;
    }
    public string Sanitize(string? html) => sanitizer.Sanitize(html ?? string.Empty).Trim();
    public bool HasContent(string html)
    {
        var document = new HtmlParser().ParseDocument(html);
        return !string.IsNullOrWhiteSpace(document.Body?.TextContent.Replace('\u00a0', ' '));
    }
}
