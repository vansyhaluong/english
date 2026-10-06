using English.Models.ViewModels.Grammar;

namespace English.Interfaces;

public enum GrammarResult { Success, NotFound, Invalid, Conflict }
public sealed record GrammarError(string Field, string Resource);
public sealed record GrammarSaveResult(GrammarResult Status, IReadOnlyList<GrammarError>? Errors = null);
public interface IGrammarService
{
    Task<GrammarChoices> GetChoicesAsync(CancellationToken cancellationToken);
    Task<GrammarListViewModel> ListAsync(GrammarQuery query, bool admin, Guid userId, CancellationToken cancellationToken);
    Task<GrammarDetailsViewModel?> GetAsync(int id, bool admin, Guid userId, CancellationToken cancellationToken);
    Task<GrammarSaveResult> SaveAsync(int? id, GrammarInputModel input, CancellationToken cancellationToken);
    Task<GrammarResult> DeleteAsync(int id, string? rowVersion, CancellationToken cancellationToken);
    Task<GrammarResult> SetLearnedAsync(int id, Guid userId, bool learned, CancellationToken cancellationToken);
}
