using System;
using System.Text;
using KlattschSharp.Wav;

namespace KlattschSharp.Tests.Wav;

public sealed class WavWriterTests
{
    [Fact]
    public void WritesValidRiffWaveHeader()
    {
        var samples = new float[480];
        var bytes = WavWriter.Encode(samples, 48_000);
        Assert.Equal((byte)'R', bytes[0]); Assert.Equal((byte)'I', bytes[1]);
        Assert.Equal((byte)'F', bytes[2]); Assert.Equal((byte)'F', bytes[3]);
        Assert.Equal((byte)'W', bytes[8]); Assert.Equal((byte)'A', bytes[9]);
        Assert.Equal((byte)'V', bytes[10]); Assert.Equal((byte)'E', bytes[11]);
        Assert.Equal((byte)'f', bytes[12]); Assert.Equal((byte)'m', bytes[13]);
        Assert.Equal((byte)'t', bytes[14]); Assert.Equal((byte)' ', bytes[15]);
        Assert.Equal(1, BitConverter.ToInt16(bytes, 20));   // PCM
        Assert.Equal(1, BitConverter.ToInt16(bytes, 22));   // mono
        Assert.Equal(48_000, BitConverter.ToInt32(bytes, 24));
        Assert.Equal(96_000, BitConverter.ToInt32(bytes, 28));
        Assert.Equal(16, BitConverter.ToInt16(bytes, 34));
        Assert.Equal(960, BitConverter.ToInt32(bytes, 40)); // data chunk size
    }

    [Fact]
    public void PeakNormalizationScalesToTarget()
    {
        var samples = new float[100];
        samples[50] = 0.5f;
        var bytes = WavWriter.Encode(samples, 48_000, peakNormalize: 0.95);
        // gain = 0.95 / 0.5, so the peak lands at floor(0.95 * 32767 + 0.5).
        Assert.Equal(31_129, BitConverter.ToInt16(bytes, 44 + 50 * 2));
    }

    [Fact]
    public void SamplesBeyondUnityClampToFullScale()
    {
        var samples = new float[] { 2f, -2f, 1f, -1f };
        var bytes = WavWriter.Encode(samples, 48_000, peakNormalize: 0);
        Assert.Equal(32_767, BitConverter.ToInt16(bytes, 44));
        Assert.Equal(-32_767, BitConverter.ToInt16(bytes, 46));
        Assert.Equal(32_767, BitConverter.ToInt16(bytes, 48));
        Assert.Equal(-32_767, BitConverter.ToInt16(bytes, 50));
    }

    [Fact]
    public void MetadataChunkIsAppendedAfterData()
    {
        var samples = new float[10];
        var meta = new WavMetadata { Software = "klattsch-test", Comment = "AH" };
        var bytes = WavWriter.Encode(samples, 48_000, peakNormalize: 0, metadata: meta);
        // data chunk is 10 * 2 = 20 bytes. LIST starts at offset 44 + 20 = 64.
        Assert.Equal("LIST", Encoding.ASCII.GetString(bytes, 64, 4));
        Assert.Equal("INFO", Encoding.ASCII.GetString(bytes, 72, 4));
    }

    [Fact]
    public void MetadataChunkPadsOddLengthSubChunksAndSizesTheRiff()
    {
        var meta = new WavMetadata { Software = "klattsch-test", Comment = "AH" };
        var bytes = WavWriter.Encode(new float[10], 48_000, peakNormalize: 0, metadata: meta);

        // LIST payload = 'INFO' + (8 + 13 + 1 pad) + (8 + 2) = 36, so the file
        // is 44 header + 20 data + 8 LIST header + 36 = 108 bytes.
        Assert.Equal(108, bytes.Length);
        Assert.Equal(bytes.Length - 8, BitConverter.ToInt32(bytes, 4));
        Assert.Equal(36, BitConverter.ToInt32(bytes, 68));

        Assert.Equal("ISFT", Encoding.ASCII.GetString(bytes, 76, 4));
        Assert.Equal(13, BitConverter.ToInt32(bytes, 80));
        Assert.Equal("klattsch-test", Encoding.ASCII.GetString(bytes, 84, 13));
        Assert.Equal(0, bytes[97]);

        // The odd sub-chunk's pad byte is what puts ICMT on a word boundary.
        Assert.Equal("ICMT", Encoding.ASCII.GetString(bytes, 98, 4));
        Assert.Equal(2, BitConverter.ToInt32(bytes, 102));
        Assert.Equal("AH", Encoding.ASCII.GetString(bytes, 106, 2));
    }
}
