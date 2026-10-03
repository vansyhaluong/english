using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Memory;

namespace English.Services;

internal static class AvatarImageContent
{
    private const int MaxDecodedAllocationMegabytes = 256;

    private static readonly DecoderOptions DecodeOptions = new()
    {
        Configuration = new Configuration(
            new JpegConfigurationModule(),
            new PngConfigurationModule(),
            new WebpConfigurationModule())
        {
            MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
            {
                AllocationLimitMegabytes = MaxDecodedAllocationMegabytes
            })
        }
    };

    public static bool TryDecode(
        ReadOnlySpan<byte> content,
        out string contentType,
        out string extension)
    {
        contentType = string.Empty;
        extension = string.Empty;

        try
        {
            // Load decodes pixels in every frame. Identify/DetectFormat alone
            // cannot validate compressed image data. Never encode the result.
            using var image = Image.Load(DecodeOptions, content);
            var format = image.Metadata.DecodedImageFormat;

            // Decoders may recover images whose closing bytes are missing.
            // These completeness checks supplement, never replace, Load.
            if (format == JpegFormat.Instance &&
                content.Length >= 2 && content[^2] == 0xff && content[^1] == 0xd9)
            {
                contentType = "image/jpeg";
                extension = ".jpg";
                return true;
            }

            ReadOnlySpan<byte> pngEnd = [0, 0, 0, 0, 0x49, 0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82];
            if (format == PngFormat.Instance && content.EndsWith(pngEnd))
            {
                contentType = "image/png";
                extension = ".png";
                return true;
            }

            if (format == WebpFormat.Instance && content.Length >= 12 &&
                BinaryPrimitives.ReadUInt32LittleEndian(content.Slice(4, 4)) == content.Length - 8)
            {
                contentType = "image/webp";
                extension = ".webp";
                return true;
            }
        }
        catch (Exception exception) when (
            exception is ImageFormatException or InvalidMemoryOperationException or
                IOException or ArgumentException or NotSupportedException or OverflowException)
        {
            // The caller returns the existing localized InvalidContent message.
        }

        return false;
    }
}
