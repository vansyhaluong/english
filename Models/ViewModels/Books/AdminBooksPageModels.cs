namespace English.Models.ViewModels.Books;

public sealed record AdminBooksIndexPage(AdminBooksQuery Query, BooksPage<AdminBookDto> Books,
    IReadOnlyList<BookTopicOption> Topics);
public sealed record AdminBookFormPage(int? BookId, AdminBookUploadInput Input, IReadOnlyList<BookTopicOption> Topics,
    string? CurrentCoverUrl = null);
public sealed record AdminChaptersPage(AdminBookDto Book, IReadOnlyList<AdminChapterSummary> Chapters);
public sealed record AdminChapterFormPage(int BookId, string BookTitle, int? ChapterId, ChapterInput Input);
public sealed record AdminBookDeletePage(int BookId, int? ChapterId, string Title, BookVersionInput Input);
