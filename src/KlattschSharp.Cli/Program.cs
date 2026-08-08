using System;
using System.IO;

using KlattschSharp;
using KlattschSharp.Text;
using KlattschSharp.Wav;

int sampleRate = 48000;
bool normalize = true;
string? bankName = null;
string? inputPath = null;
string? outputPath = null;

for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    if (arg == "--sample-rate")
    {
        if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out sampleRate) || sampleRate <= 0)
        {
            Console.Error.WriteLine("error: --sample-rate requires a positive integer argument");
            return 1;
        }
        i++;
    }
    else if (arg == "--bank")
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine("error: --bank requires a bank name argument");
            return 1;
        }
        bankName = args[i + 1];
        i++;
    }
    else if (arg == "--no-normalize")
    {
        normalize = false;
    }
    else if (arg.StartsWith("--", StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"error: unknown flag: {arg}");
        return 1;
    }
    else if (inputPath is null)
    {
        inputPath = arg;
    }
    else if (outputPath is null)
    {
        outputPath = arg;
    }
    else
    {
        Console.Error.WriteLine($"error: unexpected argument: {arg}");
        return 1;
    }
}

if (inputPath is null || outputPath is null)
{
    Console.Error.WriteLine("usage: klattsch <input.klatt> <out.wav> [--sample-rate N] [--bank NAME] [--no-normalize]");
    return 1;
}

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"input not found: {inputPath}");
    return 2;
}

try
{
    string source = File.ReadAllText(inputPath);

    var opts = new CompileOptions { SampleRate = (uint)sampleRate };
    if (bankName != null)
    {
        opts.Bank = PhonemeBanks.Get(bankName);
    }
    CompileResult result = Compiler.Compile(source, opts);

    foreach (string warning in result.Warnings)
    {
        Console.Error.WriteLine($"warning: {warning}");
    }

    // [voice=N] sections are simultaneous, render each into its own buffer and sum
    int totalSamples = Math.Max(1, (int)Math.Ceiling(result.TotalMs * sampleRate / 1000.0));
    float[] buf = new float[totalSamples];

    foreach (VoiceResult voice in result.Voices)
    {
        if (voice.Schedule.Events.Count == 0)
        {
            continue;
        }
        int voiceSamples = Math.Max(1, (int)Math.Ceiling(voice.TotalMs * sampleRate / 1000.0));
        float[] voiceBuf = new float[voiceSamples];
        FormantSynth synth = new FormantSynth(sampleRate);
        synth.QueueSchedule(voice.Schedule);
        synth.Process(voiceBuf, 0, voiceBuf.Length);
        int copy = Math.Min(buf.Length, voiceBuf.Length);
        for (int i = 0; i < copy; i++)
        {
            buf[i] += voiceBuf[i];
        }
    }

    var metadata = new WavMetadata
    {
        Software = "KlattschSharp · https://klatts.ch",
        Comment = source,
    };
    byte[] wav = WavWriter.Encode(buf, sampleRate, normalize ? 0.95 : 0.0, metadata);
    File.WriteAllBytes(outputPath, wav);

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 3;
}
