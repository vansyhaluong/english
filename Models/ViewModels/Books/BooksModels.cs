using System.ComponentModel.DataAnnotations;

namespace English.Models.ViewModels.Books;

public sealed class BooksQuery
{
    [StringLength(300)] public string? Search { get; set; }
    [Range(1, int.MaxValue)] public int? TopicId { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class AdminBooksQuery
{
    [StringLength(300)] public string? Search { get; set; }
    [Range(1, int.MaxValue)] public int? TopicId { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsDeleted { get; set; }
    public int Page { get; set; } = 1;
}

public class BookVersionInput
{
    [Required] public string? RowVersion { get; set; }
}

public sealed class BookInput
{
    [Required, StringLength(300)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Author { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int TopicId { get; set; }
    public string? Description { get; set; }
    [StringLength(2048)] public string? CoverUrl { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? RowVersion { get; set; }
}

public sealed class ChapterInput
{
    [Required, StringLength(300)] public string Title { get; set; } = string.Empty;
    [Required] public string ContentEn { get; set; } = string.Empty;
    public string? TranslationVi { get; set; }
    public string? VocabularyNotes { get; set; }
    [Range(1, int.MaxValue)] public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? RowVersion { get; set; }
}

public sealed class BookVisibilityInput : BookVersionInput
{
    [Required] public bool? IsVisible { get; set; }
}

public sealed class ChapterOrderInput : BookVersionInput
{
    [Range(1, int.MaxValue)] public int ChapterId { get; set; }
    [Range(1, int.MaxValue)] public int DisplayOrder { get; set; }
}

public sealed class ChaptersOrderInput
{
    [Required, MinLength(1)] public List<ChapterOrderInput> Chapters { get; set; } = [];
}

public sealed class ChapterReadInput
{
    [Required] public bool? IsRead { get; set; }
}

public sealed record BookTopicOption(int Id, string Name);
public sealed record BooksPage<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int TotalPages, int PageSize);

public sealed class AdminBookDto
{
    public int Id { get; init; }
    public int TopicId { get; init; }
    public string Topic { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CoverUrl { get; init; }
    public bool IsVisible { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class AdminChapterDto
{
    public int Id { get; init; }
    public int BookId { get; init; }
    public int DisplayOrder { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ContentEn { get; init; } = string.Empty;
    public string? TranslationVi { get; init; }
    public string? VocabularyNotes { get; init; }
    public bool IsVisible { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed record AdminChapterSummary(int Id, int BookId, int DisplayOrder, string Title,
    bool IsVisible, bool IsDeleted, DateTimeOffset? DeletedAt, byte[] RowVersion);
public sealed record AdminBookDetailsDto(AdminBookDto Book, IReadOnlyList<AdminChapterSummary> Chapters);

public sealed class StudentBookDto
{
    public int Id { get; init; }
    public int TopicId { get; init; }
    public string Topic { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CoverUrl { get; init; }
    public int AvailableChapterCount { get; init; }
    public int ReadChapterCount { get; init; }
    public int? ContinueChapterId { get; init; }
    public bool IsCompleted => AvailableChapterCount > 0 && ReadChapterCount == AvailableChapterCount;
}

public sealed record StudentChapterSummary(int Id, int DisplayOrder, string Title, bool IsRead, DateTimeOffset? ReadAt);
public sealed record StudentBookDetailsDto(StudentBookDto Book, IReadOnlyList<StudentChapterSummary> Chapters);
public sealed record StudentBookReaderPageModel(int BookId, int? ChapterId);
// All chapter text is plain text. A future Razor/JS client must encode it, never render raw HTML.
public sealed record StudentChapterDto(int Id, int BookId, int DisplayOrder, string Title, string ContentEn,
    string? TranslationVi, string? VocabularyNotes, bool IsRead, DateTimeOffset? ReadAt,
    int? PreviousChapterId, int? NextChapterId);
