namespace English.Services;

internal static class BookCoverImages
{
    public const int MaxSizeBytes = 2 * 1024 * 1024;

    public static async Task<byte[]> ReadBoundedAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var content = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            if (content.Length + read > MaxSizeBytes) throw new InvalidDataException("Book cover exceeds size limit.");
            await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (content.Length == 0) throw new InvalidDataException("Book cover is empty.");
        return content.ToArray();
    }
}
