using System.ComponentModel.DataAnnotations;
using English.Models;

namespace English.Models.ViewModels.Listening;

public sealed class StudentListeningQuery
{
    [StringLength(300, ErrorMessage = "ListeningTooLong")]
    public string? Search { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "ListeningClassificationRequired")]
    public int? LevelId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "ListeningClassificationRequired")]
    public int? TopicId { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class ListeningProgressInputModel
{
    [Required(ErrorMessage = "ListeningRequired")]
    public bool? IsLearned { get; set; }
}

public sealed class StudentListeningLessonViewModel
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
    public bool IsLearned { get; init; }
    public DateTimeOffset? LearnedAt { get; init; }
    public int TranscriptRevision { get; init; }
    public int CueCount { get; init; }
}

public sealed record StudentListeningListViewModel(StudentListeningQuery Query, ListeningChoices Choices,
    IReadOnlyList<StudentListeningLessonViewModel> Items, int TotalCount, int TotalPages, int PageSize);

public sealed record StudentListeningDetailsViewModel(StudentListeningLessonViewModel Lesson,
    IReadOnlyList<SubtitleCue> Cues);
