using English.Models.ViewModels.Vocabulary;

namespace English.Interfaces;

public enum VocabularyResult { Success, NotFound, Invalid, Conflict }
public sealed record VocabularyError(string Field, string Resource);
public sealed record VocabularySaveResult(VocabularyResult Status, IReadOnlyList<VocabularyError>? Errors = null);

public interface IVocabularyService
{
    Task<VocabularyChoices> GetChoicesAsync(CancellationToken cancellationToken);
    Task<VocabularyListViewModel> ListAsync(VocabularyQuery query, bool admin, Guid userId, CancellationToken cancellationToken);
    Task<VocabularyDetailsViewModel?> GetAsync(int id, bool admin, Guid userId, CancellationToken cancellationToken);
    Task<VocabularySaveResult> SaveAsync(int? id, VocabularyInputModel input, CancellationToken cancellationToken);
    Task<VocabularyResult> DeleteAsync(int id, string? rowVersion, CancellationToken cancellationToken);
    Task<VocabularyResult> SetLearnedAsync(int id, Guid userId, bool learned, CancellationToken cancellationToken);
}
