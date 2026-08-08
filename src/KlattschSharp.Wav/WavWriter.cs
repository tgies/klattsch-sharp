using System;
using System.Collections.Generic;
using System.Text;

namespace KlattschSharp.Wav;

/// <summary>
/// Encodes mono float PCM into a 16-bit RIFF/WAVE byte buffer, with optional
/// peak normalization and a <c>LIST/INFO</c> chunk.
/// </summary>
public static class WavWriter
{
    /// <summary>Encode <paramref name="samples"/> at <paramref name="sampleRate"/> Hz with default peak normalization to 0.95.</summary>
    public static byte[] Encode(float[] samples, int sampleRate)
        => Encode(samples, sampleRate, peakNormalize: 0.95, metadata: null);

    /// <summary>Encode with an explicit peak-normalization target. A value of 0 (or less) disables normalization.</summary>
    public static byte[] Encode(float[] samples, int sampleRate, double peakNormalize)
        => Encode(samples, sampleRate, peakNormalize, metadata: null);

    /// <summary>
    /// Encode with an explicit peak-normalization target and optional metadata.
    /// A <paramref name="peakNormalize"/> of 0 or less disables normalization;
    /// a null <paramref name="metadata"/> omits the LIST/INFO chunk.
    /// </summary>
    public static byte[] Encode(float[] samples, int sampleRate, double peakNormalize, WavMetadata? metadata)
    {
        if (samples == null) throw new ArgumentNullException(nameof(samples));
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));

        // Gain, clamp, and the i16 product are all double: f32 intermediates
        // move samples by one i16 step
        double gain = 1.0;
        if (peakNormalize > 0.0)
        {
            double peak = 0.0;
            for (int i = 0; i < samples.Length; i++)
            {
                double a = samples[i];
                if (a < 0.0) a = -a;
                if (a > peak) peak = a;
            }
            if (peak > 0.0) gain = peakNormalize / peak;
        }

        int dataBytes = samples.Length * 2;
        byte[]? infoBytes = metadata is null ? null : BuildInfoChunk(metadata);
        int totalSize = 44 + dataBytes + (infoBytes?.Length ?? 0);
        var buf = new byte[totalSize];

        WriteAscii(buf, 0, "RIFF");
        WriteUInt32LE(buf, 4, (uint)(totalSize - 8));
        WriteAscii(buf, 8, "WAVE");
        WriteAscii(buf, 12, "fmt ");
        WriteUInt32LE(buf, 16, 16u);
        WriteUInt16LE(buf, 20, 1);                   // PCM
        WriteUInt16LE(buf, 22, 1);                   // mono
        WriteUInt32LE(buf, 24, (uint)sampleRate);
        WriteUInt32LE(buf, 28, (uint)(sampleRate * 2));
        WriteUInt16LE(buf, 32, 2);
        WriteUInt16LE(buf, 34, 16);
        WriteAscii(buf, 36, "data");
        WriteUInt32LE(buf, 40, (uint)dataBytes);

        const int offset = 44;
        for (int i = 0; i < samples.Length; i++)
        {
            double s = samples[i] * gain;
            if (s > 1.0) s = 1.0;
            else if (s < -1.0) s = -1.0;
            // Math.Floor(x + 0.5) rounds half up, Math.Round is half-to-even
            short q = (short)Math.Floor(s * 32767.0 + 0.5);
            WriteInt16LE(buf, offset + i * 2, q);
        }

        if (infoBytes != null)
            Buffer.BlockCopy(infoBytes, 0, buf, 44 + dataBytes, infoBytes.Length);

        return buf;
    }

    private static byte[] BuildInfoChunk(WavMetadata m)
    {
        var subs = new List<(string Id, byte[] Data)>();
        if (!string.IsNullOrEmpty(m.Software)) subs.Add(("ISFT", Encoding.UTF8.GetBytes(m.Software!)));
        if (!string.IsNullOrEmpty(m.Comment)) subs.Add(("ICMT", Encoding.UTF8.GetBytes(m.Comment!)));
        if (subs.Count == 0) return Array.Empty<byte>();

        int payloadSize = 4; // 'INFO'
        foreach (var s in subs) payloadSize += 8 + s.Data.Length + (s.Data.Length % 2);

        var buf = new byte[8 + payloadSize];
        int o = 0;
        WriteAscii(buf, o, "LIST"); o += 4;
        WriteUInt32LE(buf, o, (uint)payloadSize); o += 4;
        WriteAscii(buf, o, "INFO"); o += 4;
        foreach (var s in subs)
        {
            WriteAscii(buf, o, s.Id); o += 4;
            WriteUInt32LE(buf, o, (uint)s.Data.Length); o += 4;
            Buffer.BlockCopy(s.Data, 0, buf, o, s.Data.Length);
            o += s.Data.Length + (s.Data.Length % 2); // pad to word boundary
        }
        return buf;
    }

    private static void WriteAscii(byte[] buf, int offset, string s)
    {
        for (int i = 0; i < s.Length; i++) buf[offset + i] = (byte)s[i];
    }

    private static void WriteUInt16LE(byte[] buf, int offset, ushort v)
    {
        buf[offset] = (byte)(v & 0xFF);
        buf[offset + 1] = (byte)((v >> 8) & 0xFF);
    }

    private static void WriteUInt32LE(byte[] buf, int offset, uint v)
    {
        buf[offset] = (byte)(v & 0xFF);
        buf[offset + 1] = (byte)((v >> 8) & 0xFF);
        buf[offset + 2] = (byte)((v >> 16) & 0xFF);
        buf[offset + 3] = (byte)((v >> 24) & 0xFF);
    }

    private static void WriteInt16LE(byte[] buf, int offset, short v)
        => WriteUInt16LE(buf, offset, unchecked((ushort)v));
}
