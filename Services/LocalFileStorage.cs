using System.Text.RegularExpressions;
using English.Interfaces;
using Microsoft.Extensions.Options;

namespace English.Services;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = string.Empty;
}

public sealed partial class LocalFileStorage : IFileStorage
{
    private readonly string storageRoot;

    public LocalFileStorage(
        IOptions<FileStorageOptions> options,
        IWebHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(options.Value.RootPath))
        {
            throw new InvalidOperationException("File storage is not configured.");
        }

        storageRoot = Path.GetFullPath(
            options.Value.RootPath,
            environment.ContentRootPath);

        if (IsSameOrChildPath(storageRoot, environment.ContentRootPath))
        {
            throw new InvalidOperationException(
                "File storage must be outside the application content root.");
        }
    }

    public async Task<string> SaveAsync(
        Stream content,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var extension = NormalizeExtension(fileExtension);
        Directory.CreateDirectory(storageRoot);

        var storageKey = $"{Guid.NewGuid():N}{extension}";
        var path = ResolvePath(storageKey);
        var created = false;

        try
        {
            await using var target = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            created = true;

            await content.CopyToAsync(target, cancellationToken);
            await target.FlushAsync(cancellationToken);
            return storageKey;
        }
        catch
        {
            if (created)
            {
                TryDelete(path);
            }

            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);

        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream content = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult<Stream?>(content);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TryDelete(ResolvePath(storageKey));
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            !StorageKeyPattern().IsMatch(storageKey) ||
            !string.Equals(Path.GetFileName(storageKey), storageKey, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        }

        var path = Path.GetFullPath(Path.Combine(storageRoot, storageKey));
        if (!IsSameOrChildPath(path, storageRoot))
        {
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        }

        return path;
    }

    private static string NormalizeExtension(string fileExtension)
    {
        var extension = fileExtension.Trim().TrimStart('.').ToLowerInvariant();
        if (!ExtensionPattern().IsMatch(extension))
        {
            throw new ArgumentException("Invalid file extension.", nameof(fileExtension));
        }

        return $".{extension}";
    }

    private static bool IsSameOrChildPath(string path, string parent)
    {
        var relativePath = Path.GetRelativePath(parent, path);
        return relativePath == "." ||
               (!Path.IsPathRooted(relativePath) &&
                relativePath != ".." &&
                !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [GeneratedRegex("^[a-f0-9]{32}\\.[a-z0-9]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex StorageKeyPattern();

    [GeneratedRegex("^[a-z0-9]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex ExtensionPattern();
}
