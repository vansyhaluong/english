namespace English.Models;

public static class ListeningAudioLimits
{
    public const int MaxBytes = 20 * 1024 * 1024;
    public const int MaxRequestBytes = MaxBytes + 64 * 1024;
}
public sealed record ListeningDatabaseCheck(bool IsSafe, string Message);
public sealed record ListeningVoaItem(int Number, string Title, string AudioUrl, int CueCount, int Revision, int? Id = null);
public sealed record ListeningVoaReport(bool CanImport, string Message, IReadOnlyList<ListeningVoaItem> Items, bool Imported = false);
