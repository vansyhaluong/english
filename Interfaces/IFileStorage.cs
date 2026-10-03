namespace English.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(
        Stream content,
        string fileExtension,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
