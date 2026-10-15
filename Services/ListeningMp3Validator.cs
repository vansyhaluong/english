using English.Models;

namespace English.Services;

// Structural MPEG Layer III validation: complete consecutive frames, not extension/ID3 alone.
public static class ListeningMp3Validator
{
    public static async Task<byte[]?> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) != 0)
        {
            if (memory.Length + read > ListeningAudioLimits.MaxBytes) return null;
            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        var bytes = memory.ToArray();
        return IsValid(bytes) ? bytes : null;
    }

    private static bool IsValid(ReadOnlySpan<byte> data)
    {
        var offset = 0;
        if (data.Length >= 10 && data[..3].SequenceEqual("ID3"u8))
        {
            if (data[3] is < 2 or > 4 || data[4] == 255 || (data[6] | data[7] | data[8] | data[9]) >= 128) return false;
            offset = 10 + (data[6] << 21 | data[7] << 14 | data[8] << 7 | data[9]);
            if (data[3] == 4 && (data[5] & 16) != 0) offset += 10;
            if (offset > data.Length) return false;
        }
        var end = data.Length;
        if (end - offset >= 128 && data.Slice(end - 128, 3).SequenceEqual("TAG"u8)) end -= 128;
        int frames = 0, streamVersion = -1, streamRate = -1;
        int[] mpeg1Rates = [0,32,40,48,56,64,80,96,112,128,160,192,224,256,320];
        int[] mpeg2Rates = [0,8,16,24,32,40,48,56,64,80,96,112,128,144,160];
        int[] sampleRates = [44100,48000,32000];
        while (offset < end)
        {
            if (end - offset < 4 || data[offset] != 255 || (data[offset + 1] & 224) != 224) return false;
            var version = (data[offset + 1] >> 3) & 3;
            var layer = (data[offset + 1] >> 1) & 3;
            var bitrateIndex = data[offset + 2] >> 4;
            var rateIndex = (data[offset + 2] >> 2) & 3;
            if (version == 1 || layer != 1 || bitrateIndex is 0 or 15 || rateIndex == 3) return false;
            var rate = sampleRates[rateIndex] / (version == 3 ? 1 : version == 2 ? 2 : 4);
            var bitrate = (version == 3 ? mpeg1Rates : mpeg2Rates)[bitrateIndex] * 1000;
            var length = (version == 3 ? 144 : 72) * bitrate / rate + ((data[offset + 2] >> 1) & 1);
            if (length < 4 || offset + length > end || (frames > 0 && (version != streamVersion || rate != streamRate))) return false;
            streamVersion = version; streamRate = rate;
            offset += length; frames++;
        }
        return frames >= 2;
    }
}
