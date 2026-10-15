using English.Models.ViewModels.Listening;
using English.Models;

namespace English.Interfaces;

public enum ListeningResult { Success, NotFound, Invalid, Conflict }
public sealed record ListeningError(string Field, string Resource);
public sealed record ListeningSaveResult(ListeningResult Status, IReadOnlyList<ListeningError>? Errors = null);
public sealed record ListeningSubtitleImportResult(ListeningResult Status, bool Unchanged = false, SubtitleError? Error = null);
public interface IListeningService
{
    Task<StudentListeningListViewModel> ListStudentAsync(StudentListeningQuery query, Guid userId, CancellationToken cancellationToken);
    Task<StudentListeningDetailsViewModel?> GetStudentAsync(int id, Guid userId, CancellationToken cancellationToken);
    Task<ListeningResult> SetLearnedAsync(int id, Guid userId, bool learned, CancellationToken cancellationToken);
    Task<FileStream?> OpenAudioAsync(string key, CancellationToken cancellationToken);
    Task<ListeningChoices> GetChoicesAsync(CancellationToken cancellationToken);
    Task<ListeningListViewModel> ListAsync(ListeningQuery query, CancellationToken cancellationToken);
    Task<ListeningDetailsViewModel?> GetAsync(int id, CancellationToken cancellationToken);
    Task<ListeningSaveResult> SaveAsync(int? id, ListeningInputModel input, CancellationToken cancellationToken);
    Task<ListeningResult> SetVisibilityAsync(int id, ListeningVisibilityInputModel input, CancellationToken cancellationToken);
    Task<ListeningResult> DeleteAsync(int id, string? rowVersion, CancellationToken cancellationToken);
    Task<ListeningSubtitleImportResult> ImportSubtitleAsync(int id, ListeningSubtitleUpload upload, CancellationToken cancellationToken);
}
