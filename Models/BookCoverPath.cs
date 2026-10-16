using System.Text.RegularExpressions;

namespace English.Models;

public static partial class BookCoverPath
{
    public const string Prefix = "/books/covers/";

    public static bool IsStorageKey(string key) => KeyPattern().IsMatch(key);

    public static string? DisplayUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl) || value.Any(char.IsWhiteSpace)) return null;
        if (value.StartsWith(Prefix, StringComparison.Ordinal) && IsStorageKey(value[Prefix.Length..])) return value;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && uri.UserInfo.Length == 0 ? value : null;
    }

    [GeneratedRegex("\\A[a-f0-9]{32}\\.(jpg|png|webp)\\z", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}
