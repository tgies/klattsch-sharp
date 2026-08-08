using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using KlattschSharp;
using KlattschSharp.Text;
using KlattschSharp.Wav;

using Xunit.Abstractions;

namespace KlattschSharp.Tests.Parity;

/// <summary>
/// Asserts byte-identical output against the reference for every golden in
/// tests/goldens/manifest.json. The render is encoded through
/// <see cref="WavWriter"/>.
/// </summary>
public sealed class GoldenParityTests
{
    private readonly ITestOutputHelper _out;

    public GoldenParityTests(ITestOutputHelper output)
    {
        _out = output;
    }

    public static IEnumerable<object[]> ManifestEntries()
    {
        string goldenDir = Path.Combine(AppContext.BaseDirectory, "goldens");
        string manifestPath = Path.Combine(goldenDir, "manifest.json");
        string json = File.ReadAllText(manifestPath);

        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        var entries = JsonSerializer.Deserialize<List<ManifestEntry>>(json, opts)
                      ?? throw new InvalidOperationException("manifest.json deserialized as null");

        foreach (var entry in entries)
        {
            yield return new object[]
            {
                entry.Name,
                entry.Text!,
                entry.ScheduleFile!,
                entry.Bank!,
                entry.SampleRate,
                entry.PeakNormalize,
            };
        }
    }

    // An empty manifest would make the parity theory pass vacuously.
    [Fact]
    public void ManifestHasEntries()
    {
        Assert.NotEmpty(ManifestEntries());
    }

    [Theory]
    [MemberData(nameof(ManifestEntries))]
    public void GoldenParity(
        string name,
        string? text,
        string? scheduleFile,
        string? bank,
        int sampleRate,
        double peakNormalize)
    {
        string goldenDir = Path.Combine(AppContext.BaseDirectory, "goldens");

        short[] jsSamples = ReadWavSamples(File.ReadAllBytes(Path.Combine(goldenDir, $"{name}.wav")));

        // A text case compiles to one part per [voice=N] section; a schedule
        // case loads a single part.
        var parts = new List<(Schedule Schedule, int Samples)>();
        int bufLen;
        if (scheduleFile != null)
        {
            var spec = LoadScheduleSpec(Path.Combine(goldenDir, scheduleFile));
            bufLen = spec.Samples;
            parts.Add((spec.ToSchedule((uint)sampleRate), spec.Samples));
        }
        else
        {
            var compileOpts = new CompileOptions { SampleRate = (uint)sampleRate };
            if (bank != null)
            {
                compileOpts.Bank = PhonemeBanks.Get(bank);
            }
            CompileResult result = Compiler.Compile(text!, compileOpts);
            bufLen = Math.Max(1, (int)Math.Ceiling(result.TotalMs * sampleRate / 1000.0));
            foreach (VoiceResult voice in result.Voices)
            {
                parts.Add((voice.Schedule, Math.Max(1, (int)Math.Ceiling(voice.TotalMs * sampleRate / 1000.0))));
            }
        }

        // Voices are simultaneous: render each into its own buffer and sum.
        float[] raw = new float[bufLen];
        foreach (var part in parts)
        {
            if (part.Schedule.Events.Count == 0)
            {
                continue;
            }
            float[] voiceBuf = new float[part.Samples];
            var synth = new FormantSynth(sampleRate);
            synth.QueueSchedule(part.Schedule);
            synth.Process(voiceBuf, 0, voiceBuf.Length);
            int copy = Math.Min(raw.Length, voiceBuf.Length);
            for (int i = 0; i < copy; i++)
            {
                raw[i] += voiceBuf[i];
            }
        }

        short[] csSamples = ReadWavSamples(WavWriter.Encode(raw, sampleRate, peakNormalize));

        long lenDiff = Math.Abs((long)csSamples.Length - (long)jsSamples.Length);
        if (lenDiff > 1)
        {
            Assert.Fail(
                $"{name}: length mismatch (cs {csSamples.Length} vs js {jsSamples.Length})");
        }

        int n = Math.Min(csSamples.Length, jsSamples.Length);
        int diffCount = 0;
        int maxStep = 0;
        int maxStepIdx = 0;

        for (int i = 0; i < n; i++)
        {
            int step = Math.Abs(csSamples[i] - jsSamples[i]);
            if (step == 0)
            {
                continue;
            }

            diffCount++;
            if (step > maxStep)
            {
                maxStep = step;
                maxStepIdx = i;
            }
        }

        _out.WriteLine($"{name}: diff_count={diffCount}/{n}, max_step={maxStep}, cs_len={csSamples.Length}, js_len={jsSamples.Length}");

        // A worst case of one i16 step points at platform libm; see tests/goldens/README.md
        Assert.True(
            diffCount == 0,
            $"{name}: {diffCount}/{n} samples differ from the JS reference, worst {maxStep} i16 step(s) at sample {maxStepIdx}");
    }

    // Walk RIFF chunks after the WAVE fourcc, find "data", return its i16 LE samples.
    private static short[] ReadWavSamples(byte[] bytes)
    {
        if (bytes.Length < 12
            || bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F'
            || bytes[8] != 'W' || bytes[9] != 'A' || bytes[10] != 'V' || bytes[11] != 'E')
        {
            throw new InvalidDataException("not a RIFF WAVE buffer");
        }

        int offset = 12;
        while (offset + 8 <= bytes.Length)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
            int chunkSize = BitConverter.ToInt32(bytes, offset + 4);

            if (chunkId == "data")
            {
                int dataStart = offset + 8;
                int sampleCount = chunkSize / 2;
                short[] samples = new short[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    samples[i] = BitConverter.ToInt16(bytes, dataStart + i * 2);
                }

                return samples;
            }

            // Chunks are word-aligned: header + size + a pad byte if odd.
            offset += 8 + chunkSize + (chunkSize & 1);
        }

        throw new InvalidDataException("no data chunk found");
    }

    private static ScheduleSpec LoadScheduleSpec(string path)
    {
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return JsonSerializer.Deserialize<ScheduleSpec>(File.ReadAllText(path), opts)
               ?? throw new InvalidOperationException($"{path} deserialized as null");
    }

    private sealed class ManifestEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("scheduleFile")]
        public string? ScheduleFile { get; set; }

        [JsonPropertyName("bank")]
        public string? Bank { get; set; }

        [JsonPropertyName("sampleRate")]
        public int SampleRate { get; set; }

        // Peak-normalization target; absent (0) renders unnormalized.
        [JsonPropertyName("peakNormalize")]
        public double PeakNormalize { get; set; }
    }

    // A committed *.schedule.json: raw MsEvents driving FormantSynth directly,
    // bypassing the text compiler. Target keys use the JS parameter names.
    private sealed class ScheduleSpec
    {
        [JsonPropertyName("sampleRate")]
        public int SampleRate { get; set; }

        [JsonPropertyName("samples")]
        public int Samples { get; set; }

        [JsonPropertyName("schedule")]
        public List<SpecEvent> Schedule { get; set; } = new();

        public Schedule ToSchedule(uint sampleRate)
        {
            var events = new List<MsEvent>(Schedule.Count);
            foreach (var e in Schedule)
            {
                events.Add(new MsEvent(e.AtMs, ToParamUpdate(e.Target), e.TransitionMs));
            }
            return KlattschSharp.Schedule.FromMsEvents(sampleRate, events);
        }

        private static ParamUpdate ToParamUpdate(Dictionary<string, double> target)
        {
            double? f0 = null, voicing = null, f1 = null, bw1 = null, a1 = null;
            double? f2 = null, bw2 = null, a2 = null, f3 = null, bw3 = null, a3 = null;
            double? gain = null, vibratoDepth = null, vibratoRate = null;
            double? tremoloDepth = null, tremoloRate = null, aspiration = null, tilt = null, effort = null;

            foreach (var kv in target)
            {
                switch (kv.Key)
                {
                    case "F0": f0 = kv.Value; break;
                    case "voicing": voicing = kv.Value; break;
                    case "F1": f1 = kv.Value; break;
                    case "BW1": bw1 = kv.Value; break;
                    case "A1": a1 = kv.Value; break;
                    case "F2": f2 = kv.Value; break;
                    case "BW2": bw2 = kv.Value; break;
                    case "A2": a2 = kv.Value; break;
                    case "F3": f3 = kv.Value; break;
                    case "BW3": bw3 = kv.Value; break;
                    case "A3": a3 = kv.Value; break;
                    case "gain": gain = kv.Value; break;
                    case "vibratoDepth": vibratoDepth = kv.Value; break;
                    case "vibratoRate": vibratoRate = kv.Value; break;
                    case "tremoloDepth": tremoloDepth = kv.Value; break;
                    case "tremoloRate": tremoloRate = kv.Value; break;
                    case "aspiration": aspiration = kv.Value; break;
                    case "tilt": tilt = kv.Value; break;
                    case "effort": effort = kv.Value; break;
                    default:
                        throw new InvalidDataException($"unknown schedule target key: {kv.Key}");
                }
            }

            return new ParamUpdate
            {
                F0 = f0,
                Voicing = voicing,
                F1 = f1,
                BW1 = bw1,
                A1 = a1,
                F2 = f2,
                BW2 = bw2,
                A2 = a2,
                F3 = f3,
                BW3 = bw3,
                A3 = a3,
                Gain = gain,
                VibratoDepth = vibratoDepth,
                VibratoRate = vibratoRate,
                TremoloDepth = tremoloDepth,
                TremoloRate = tremoloRate,
                Aspiration = aspiration,
                Tilt = tilt,
                Effort = effort,
            };
        }
    }

    private sealed class SpecEvent
    {
        [JsonPropertyName("atMs")]
        public double AtMs { get; set; }

        [JsonPropertyName("transitionMs")]
        public double TransitionMs { get; set; }

        [JsonPropertyName("target")]
        public Dictionary<string, double> Target { get; set; } = new();
    }
}
