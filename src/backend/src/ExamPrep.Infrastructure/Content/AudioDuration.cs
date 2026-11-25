using System.Buffers.Binary;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Infrastructure.Content;

/// <summary>
/// The length of a recording, read from the file itself (no decoder): it fills <c>assets.duration_ms</c>, which caps an
/// invigilator's rewind at the end of the recording and stops the monitor's audio position there. Null when the file
/// is not one of the package's audio types or cannot be read; those features then stay off for that paper.
/// </summary>
public static class AudioDuration
{
    public static int? Milliseconds(byte[] content, string extension)
    {
        try
        {
            var seconds = extension.ToLowerInvariant() switch
            {
                ".wav" => Wav(content),
                ".mp3" => Mp3(content),
                ".m4a" => Mp4(content),
                ".ogg" => Ogg(content),
                _ => null,
            };
            return seconds is > 0 and < int.MaxValue / 1000.0 ? (int)Math.Round(seconds.Value * 1000) : null;
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            // A truncated or damaged file: no length rather than a failed import.
            return null;
        }
    }

    /// <summary>
    /// Recordings stored before lengths were read get theirs from the stored file, once at start-up. Returns how many it
    /// filled; a file that cannot be read keeps no length and is tried again at the next start.
    /// </summary>
    public static async Task<int> FillMissingAsync(ExamPrepDbContext db, IAssetStorage storage, CancellationToken cancellationToken)
    {
        var missing = await db.Assets.Where(a => a.Kind == AssetKind.Audio && a.DurationMs == null).ToListAsync(cancellationToken);
        foreach (var asset in missing)
        {
            var path = storage.ResolvePath(asset.StoredPath);
            if (File.Exists(path))
            {
                asset.DurationMs = Milliseconds(await File.ReadAllBytesAsync(path, cancellationToken), Path.GetExtension(path));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return missing.Count(a => a.DurationMs is not null);
    }

    /// <summary>RIFF/WAVE: the data chunk's size divided by the fmt chunk's bytes per second.</summary>
    private static double? Wav(byte[] b)
    {
        if (b.Length < 12 || !b.AsSpan(0, 4).SequenceEqual("RIFF"u8) || !b.AsSpan(8, 4).SequenceEqual("WAVE"u8))
        {
            return null;
        }

        uint byteRate = 0;
        for (var at = 12; at + 8 <= b.Length;)
        {
            var size = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(at + 4));
            if (b.AsSpan(at, 4).SequenceEqual("fmt "u8))
            {
                byteRate = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(at + 16));
            }
            else if (b.AsSpan(at, 4).SequenceEqual("data"u8))
            {
                return byteRate == 0 ? null : (double)Math.Min(size, (uint)(b.Length - at - 8)) / byteRate;
            }

            if (size > b.Length - at - 8)
            {
                return null;
            }

            at += 8 + (int)size + (int)(size & 1);
        }

        return null;
    }

    private static readonly int[][] Mp3Kbps =
    [
        [0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448], // MPEG-1 layer I
        [0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384],    // MPEG-1 layer II
        [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320],     // MPEG-1 layer III
        [0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256],    // MPEG-2/2.5 layer I
        [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160],         // MPEG-2/2.5 layers II and III
    ];

    /// <summary>MPEG audio: every frame's samples added up, so constant and variable bitrates both come out exact.</summary>
    private static double? Mp3(byte[] b)
    {
        var at = 0;
        if (b.Length >= 10 && b.AsSpan(0, 3).SequenceEqual("ID3"u8))
        {
            at = 10 + (b[6] << 21 | b[7] << 14 | b[8] << 7 | b[9]) + ((b[5] & 0x10) != 0 ? 10 : 0);
        }

        double seconds = 0;
        while (at + 4 <= b.Length)
        {
            int version = (b[at + 1] >> 3) & 3, layer = (b[at + 1] >> 1) & 3, rateIndex = b[at + 2] >> 4, srIndex = (b[at + 2] >> 2) & 3;
            if (b[at] != 0xFF || (b[at + 1] & 0xE0) != 0xE0 || version == 1 || layer == 0 || rateIndex is 0 or 15 || srIndex == 3)
            {
                at++; // not a frame header (tag, junk): look further
                continue;
            }

            var mpeg1 = version == 3;
            var sampleRate = new[] { 44100, 48000, 32000 }[srIndex] >> (mpeg1 ? 0 : version == 2 ? 1 : 2);
            var table = mpeg1 ? 3 - layer : layer == 3 ? 3 : 4;
            var bitRate = Mp3Kbps[table][rateIndex] * 1000;
            var padding = (b[at + 2] >> 1) & 1;
            var (samples, length) = layer switch
            {
                3 => (384, (12 * bitRate / sampleRate + padding) * 4),
                2 => (1152, 144 * bitRate / sampleRate + padding),
                _ => (mpeg1 ? 1152 : 576, (mpeg1 ? 144 : 72) * bitRate / sampleRate + padding),
            };
            seconds += (double)samples / sampleRate;
            at += length;
        }

        return seconds;
    }

    /// <summary>MP4/M4A: the movie header's duration over its time scale.</summary>
    private static double? Mp4(byte[] b)
    {
        var moov = Box(b, 0, b.Length, "moov"u8);
        var mvhd = moov is { } m ? Box(b, m.Start, m.End, "mvhd"u8) : null;
        if (mvhd is not { } h)
        {
            return null;
        }

        var v1 = b[h.Start] == 1;
        var timescale = BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(h.Start + (v1 ? 20 : 12)));
        var duration = v1 ? BinaryPrimitives.ReadUInt64BigEndian(b.AsSpan(h.Start + 24)) : BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(h.Start + 16));
        return timescale == 0 ? null : (double)duration / timescale;
    }

    /// <summary>The content range of the first box of this type between start and end.</summary>
    private static (int Start, int End)? Box(byte[] b, int start, int end, ReadOnlySpan<byte> type)
    {
        for (var at = start; at + 8 <= end;)
        {
            long size = BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(at));
            var header = 8;
            if (size == 1)
            {
                size = (long)BinaryPrimitives.ReadUInt64BigEndian(b.AsSpan(at + 8));
                header = 16;
            }
            else if (size == 0)
            {
                size = end - at;
            }

            if (size < header || at + size > end)
            {
                return null;
            }

            if (b.AsSpan(at + 4, 4).SequenceEqual(type))
            {
                return (at + header, (int)(at + size));
            }

            at += (int)size;
        }

        return null;
    }

    /// <summary>Ogg Vorbis or Opus: the last page's granule position (samples) over the sample rate.</summary>
    private static double? Ogg(byte[] b)
    {
        if (b.Length < 28 || !b.AsSpan(0, 4).SequenceEqual("OggS"u8))
        {
            return null;
        }

        var packet = 27 + b[26];
        double rate;
        long preSkip = 0;
        if (b.AsSpan(packet, 7).SequenceEqual("\u0001vorbis"u8))
        {
            rate = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(packet + 12));
        }
        else if (b.AsSpan(packet, 8).SequenceEqual("OpusHead"u8))
        {
            rate = 48000; // Opus granules always count 48 kHz samples
            preSkip = BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(packet + 10));
        }
        else
        {
            return null;
        }

        var last = b.AsSpan().LastIndexOf("OggS"u8);
        var granule = BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(last + 6));
        return rate == 0 ? null : (granule - preSkip) / rate;
    }
}
