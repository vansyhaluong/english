namespace English.Interfaces;

public sealed record MediaUrlValidationResult(
    bool IsValid,
    Uri? Url,
    string? ErrorMessage);

public interface IMediaUrlValidator
{
    MediaUrlValidationResult Validate(string? value);
}
