using System.ComponentModel.DataAnnotations;

namespace English.Models.ViewModels.Listening;

public sealed class ListeningInputModel
{
    private string title = string.Empty;
    private string audioUrl = string.Empty;
    [Required(ErrorMessage = "ListeningRequired"), StringLength(300, ErrorMessage = "ListeningTooLong")]
    public string Title { get => title; set => title = value?.Trim() ?? string.Empty; }
    public string? Description { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "ListeningClassificationRequired")]
    public int LevelId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "ListeningClassificationRequired")]
    public int TopicId { get; set; }
    [StringLength(2048, ErrorMessage = "ListeningTooLong")]
    public string? AudioUrl { get => audioUrl; set => audioUrl = value?.Trim() ?? string.Empty; }
    public bool IsVisible { get; set; } = true;
    public string? RowVersion { get; set; }
}
public sealed class ListeningQuery
{
    [StringLength(300, ErrorMessage = "ListeningTooLong")]
    public string? Search { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "ListeningClassificationRequired")]
    public int? LevelId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "ListeningClassificationRequired")]
    public int? TopicId { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsDeleted { get; set; }
    public int Page { get; set; } = 1;
}
public sealed class ListeningDeleteInputModel { public string? RowVersion { get; set; } }
public sealed class ListeningVisibilityInputModel
{
    [Required(ErrorMessage = "ListeningRequired")]
    public bool? IsVisible { get; set; }
    public string? RowVersion { get; set; }
}
public sealed record ListeningOption(int Id, string Name);
public sealed record ListeningChoices(IReadOnlyList<ListeningOption> Levels, IReadOnlyList<ListeningOption> Topics);
public sealed class ListeningDetailsViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int LevelId { get; init; }
    public string Level { get; init; } = string.Empty;
    public int TopicId { get; init; }
    public string Topic { get; init; } = string.Empty;
    public string AudioUrl { get; init; } = string.Empty;
    public string? YouTubeVideoId => ListeningYouTubeUrl.GetVideoId(AudioUrl);
    public bool AudioLocked { get; init; }
    public bool IsVisible { get; init; }
    public bool IsDeleted { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public int TranscriptRevision { get; init; }
    public int CueCount { get; init; }
}
public sealed record ListeningListViewModel(ListeningQuery Query, ListeningChoices Choices,
    IReadOnlyList<ListeningDetailsViewModel> Items, int TotalPages);
public sealed record ListeningFormViewModel(int? Id, ListeningInputModel Input, ListeningChoices Choices,
    bool AudioLocked = false, string? CurrentAudioUrl = null);
public sealed record ListeningDeleteViewModel(ListeningDetailsViewModel Item, ListeningDeleteInputModel Input);
public sealed class ListeningSubtitleInputModel
{
    [Required(ErrorMessage = "ListeningSubtitleRequired")]
    public IFormFile? File { get; set; }
    [Required(ErrorMessage = "ListeningConflict")]
    public string? RowVersion { get; set; }
    [Required(ErrorMessage = "ListeningConflict"), Range(0, int.MaxValue, ErrorMessage = "ListeningConflict")]
    public int? ExpectedRevision { get; set; }
}
public sealed record ListeningSubtitleViewModel(ListeningDetailsViewModel Item, ListeningSubtitleInputModel Input);
