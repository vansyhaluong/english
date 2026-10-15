namespace English.Models;

public static class ListeningSubtitleLimits
{
    public const int MaxSizeBytes = 1024 * 1024;
    public const int MaxRequestBytes = MaxSizeBytes + 64 * 1024;
}

public sealed record SubtitleCue(int Sequence, int StartMs, int EndMs, string Text);
public sealed record SubtitleError(string Resource, int Line = 0);
public sealed record SubtitleParseResult(IReadOnlyList<SubtitleCue> Cues, SubtitleError? Error = null)
{
    public bool IsValid => Error is null;
}
public sealed record ListeningSubtitleUpload(Stream Content, string FileName, long Length,
    string? RowVersion, int? ExpectedRevision);
