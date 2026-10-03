using English.Interfaces;

namespace English.Services;

public sealed class MediaUrlValidator : IMediaUrlValidator
{
    private const string InvalidMessage =
        "Media URL must be an absolute HTTP or HTTPS URL.";

    public MediaUrlValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new MediaUrlValidationResult(false, null, InvalidMessage);
        }

        return new MediaUrlValidationResult(true, uri, null);
    }
}
