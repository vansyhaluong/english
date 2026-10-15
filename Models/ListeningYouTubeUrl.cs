using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace English.Models;

// Syntax validation only: never fetches the submitted URL or checks video availability.
public static partial class ListeningYouTubeUrl
{
    public static string? GetVideoId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        if (value.Length > 2048 || value.Any(char.IsWhiteSpace) || value.Any(char.IsControl)
            || value.Contains('\\') || value.Contains('@')
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length != 0)
            return null;

        var shortHost = uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase);
        if (!shortHost && !uri.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase)) return null;

        var query = QueryHelpers.ParseQuery(uri.Query);
        string? id;
        if (!shortHost && uri.AbsolutePath == "/watch")
        {
            if (!query.TryGetValue("v", out var values) || values.Count != 1) return null;
            id = values[0];
        }
        else
        {
            // A path ID plus a query ID is ambiguous, even when both happen to be equal.
            if (query.ContainsKey("v")) return null;
            var path = uri.AbsolutePath;
            if (shortHost) id = path.StartsWith('/') ? path[1..] : null;
            else if (path.StartsWith("/embed/", StringComparison.Ordinal)) id = path[7..];
            else if (path.StartsWith("/shorts/", StringComparison.Ordinal)) id = path[8..];
            else return null;
        }
        return id is not null && VideoIdPattern().IsMatch(id) ? id : null;
    }

    public static string Normalize(string videoId) => "https://www.youtube.com/watch?v=" + videoId;

    [GeneratedRegex(@"\A[A-Za-z0-9_-]{11}\z", RegexOptions.CultureInvariant)]
    private static partial Regex VideoIdPattern();
}
