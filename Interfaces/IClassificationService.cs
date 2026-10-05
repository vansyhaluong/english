namespace English.Interfaces;

public enum ClassificationKind
{
    Topic,
    GrammarGroup
}

public sealed record ClassificationSummary(int Id, string Name, string? Description);
public sealed record ClassificationDetails(int Id, string Name, string? Description, byte[] RowVersion);
public sealed record LevelSummary(string Code, byte SortOrder);

public enum ClassificationResult
{
    Success,
    NotFound,
    InvalidInput,
    Conflict,
    Referenced
}

public interface IClassificationService
{
    Task<IReadOnlyList<ClassificationSummary>> GetAllAsync(ClassificationKind kind, CancellationToken cancellationToken);
    Task<ClassificationDetails?> GetAsync(ClassificationKind kind, int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<LevelSummary>> GetLevelsAsync(CancellationToken cancellationToken);
    Task<ClassificationResult> CreateAsync(ClassificationKind kind, string name, string? description, CancellationToken cancellationToken);
    Task<ClassificationResult> EditAsync(ClassificationKind kind, int id, string name, string? description, byte[] rowVersion, CancellationToken cancellationToken);
    Task<ClassificationResult> DeleteAsync(ClassificationKind kind, int id, byte[] rowVersion, CancellationToken cancellationToken);
}
