using English.Models.ViewModels.Books;

namespace English.Interfaces;

public enum BooksResult { Success, NotFound, Invalid, Conflict }
public sealed record BooksError(string Field, string Code);
public sealed record BooksMutationResult(BooksResult Status, int? Id = null, byte[]? RowVersion = null,
    IReadOnlyList<BooksError>? Errors = null);
public sealed record BookCoverUpload(Stream Content, long SizeBytes);
public sealed record BookCoverFile(byte[] Content, string ContentType);

public interface IBooksService
{
    Task<IReadOnlyList<BookTopicOption>> GetTopicsAsync(CancellationToken cancellationToken);
    Task<BooksPage<AdminBookDto>> ListAdminAsync(AdminBooksQuery query, CancellationToken cancellationToken);
    Task<AdminBookDetailsDto?> GetAdminAsync(int bookId, CancellationToken cancellationToken);
    Task<AdminChapterDto?> GetAdminChapterAsync(int bookId, int chapterId, CancellationToken cancellationToken);
    Task<BooksMutationResult> SaveBookAsync(int? bookId, BookInput input, CancellationToken cancellationToken);
    Task<BooksMutationResult> SaveBookWithCoverAsync(int? bookId, BookInput input, BookCoverUpload? cover, CancellationToken cancellationToken);
    Task<BookCoverFile?> GetCoverAsync(string storageKey, bool isAdmin, CancellationToken cancellationToken);
    Task<BooksMutationResult> SetBookVisibilityAsync(int bookId, BookVisibilityInput input, CancellationToken cancellationToken);
    Task<BooksMutationResult> DeleteBookAsync(int bookId, BookVersionInput input, CancellationToken cancellationToken);
    Task<BooksMutationResult> SaveChapterAsync(int bookId, int? chapterId, ChapterInput input, CancellationToken cancellationToken);
    Task<BooksMutationResult> SaveVocabularyNotesAsync(int bookId, int chapterId, ChapterVocabularyInput input, CancellationToken cancellationToken);
    Task<BooksMutationResult> SetChapterVisibilityAsync(int bookId, int chapterId, BookVisibilityInput input, CancellationToken cancellationToken);
    Task<BooksMutationResult> DeleteChapterAsync(int bookId, int chapterId, BookVersionInput input, CancellationToken cancellationToken);
    Task<BooksMutationResult> ReorderChaptersAsync(int bookId, ChaptersOrderInput input, CancellationToken cancellationToken);
    Task<BooksPage<StudentBookDto>> ListStudentAsync(BooksQuery query, Guid userId, CancellationToken cancellationToken);
    Task<StudentBookDetailsDto?> GetStudentAsync(int bookId, Guid userId, CancellationToken cancellationToken);
    Task<StudentChapterDto?> GetChapterAsync(int bookId, int chapterId, Guid userId, CancellationToken cancellationToken);
    Task<BooksMutationResult> SetReadAsync(int bookId, int chapterId, Guid userId, bool isRead, CancellationToken cancellationToken);
    Task<BooksMutationResult> RecordOpenedAsync(int bookId, int chapterId, Guid userId, CancellationToken cancellationToken);
}
