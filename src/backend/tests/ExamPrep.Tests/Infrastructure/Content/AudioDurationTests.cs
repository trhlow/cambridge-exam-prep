using System.Buffers.Binary;
using System.Text;
using ExamPrep.Infrastructure.Content;

namespace ExamPrep.Tests.Infrastructure.Content;

/// <summary>Recording lengths read from the file headers, so the rewind cap and the monitor know where a recording ends.</summary>
public sealed class AudioDurationTests
{
    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    private static byte[] U32Le(uint v) { var b = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, v); return b; }

    private static byte[] U32Be(uint v) { var b = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, v); return b; }

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public void Wav_DataSizeOverBytesPerSecond_SkippingOtherChunks()
    {
        var fmt = Concat(Ascii("fmt "), U32Le(16), [1, 0, 1, 0], U32Le(8000), U32Le(16000), [2, 0, 16, 0]);
        var list = Concat(Ascii("LIST"), U32Le(3), Ascii("abc"), [0]);
        var data = Concat(Ascii("data"), U32Le(32000), new byte[32000]);
        var wav = Concat(Ascii("RIFF"), U32Le(0), Ascii("WAVE"), fmt, list, data);

        Assert.Equal(2000, AudioDuration.Milliseconds(wav, ".wav"));
    }

    [Fact]
    public void Mp3_AddsUpEveryFrame_AfterAnId3Tag()
    {
        // MPEG-1 layer III, 128 kbit/s, 44.1 kHz: 417-byte frames of 1152 samples.
        var frame = new byte[417];
        frame[0] = 0xFF; frame[1] = 0xFB; frame[2] = 0x90;
        var id3 = Concat(Ascii("ID3"), [4, 0, 0, 0, 0, 0, 20], new byte[20]);
        var mp3 = Concat([id3, .. Enumerable.Repeat(frame, 100)]);

        Assert.Equal((int)Math.Round(100 * 1152 / 44.1), AudioDuration.Milliseconds(mp3, ".MP3"));
    }

    [Fact]
    public void M4a_MovieHeaderDurationOverTimeScale()
    {
        var mvhd = Concat(U32Be(8 + 100), Ascii("mvhd"), new byte[12], U32Be(44100), U32Be(441000), new byte[80]);
        var moov = Concat(U32Be((uint)(8 + mvhd.Length)), Ascii("moov"), mvhd);
        var m4a = Concat(U32Be(16), Ascii("ftypM4A "), new byte[4], moov);

        Assert.Equal(10_000, AudioDuration.Milliseconds(m4a, ".m4a"));
    }

    private static byte[] OggPage(long granule, byte[] packet) =>
        Concat(Ascii("OggS"), [0, 0], BitConverter.GetBytes(granule), new byte[12], [1, (byte)packet.Length], packet);

    [Fact]
    public void OggVorbis_LastGranuleOverSampleRate()
    {
        var ident = Concat([1], Ascii("vorbis"), new byte[4], [1], U32Le(22050), new byte[10]);
        var ogg = Concat(OggPage(0, ident), OggPage(22050 * 3, new byte[5]));

        Assert.Equal(3000, AudioDuration.Milliseconds(ogg, ".ogg"));
    }

    [Fact]
    public void OggOpus_Counts48kHzAfterThePreSkip()
    {
        var head = Concat(Ascii("OpusHead"), [1, 1], [0x38, 0x01], new byte[8]); // pre-skip 312
        var ogg = Concat(OggPage(0, head), OggPage(48000 * 2 + 312, new byte[5]));

        Assert.Equal(2000, AudioDuration.Milliseconds(ogg, ".ogg"));
    }

    [Theory]
    [InlineData(".mp3")]
    [InlineData(".wav")]
    [InlineData(".m4a")]
    [InlineData(".ogg")]
    [InlineData(".png")]
    public void NotARecording_HasNoLength(string extension)
    {
        Assert.Null(AudioDuration.Milliseconds(Ascii("placeholder for audio/part1"), extension));
        Assert.Null(AudioDuration.Milliseconds([], extension));
    }

    [Fact]
    public void TheSampleRecording_MatchesWhatADecoderPlays()
    {
        // ffmpeg decodes setup/exam-package/samples/a2-key-fs-listening/audio/part1.mp3 to 4:23.74.
        var mp3 = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Samples", "a2-key-fs-listening", "audio", "part1.mp3"));

        Assert.InRange(AudioDuration.Milliseconds(mp3, ".mp3")!.Value, 263_640, 263_840);
    }
}
