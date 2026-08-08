using System.Collections.Generic;

namespace KlattschSharp.Text;

/// <summary>
/// The product of a compile. The top-level members describe the first
/// <c>[voice=N]</c> section; <see cref="Voices"/> lists every section.
/// </summary>
public sealed class CompileResult
{
    /// <summary>Result bundling <paramref name="schedule"/>, the utterance length, and any warnings.</summary>
    public CompileResult(
        Schedule schedule,
        double totalMs,
        IReadOnlyList<string> warnings,
        string? engine = null,
        IReadOnlyList<VoiceResult>? voices = null)
    {
        Schedule = schedule;
        TotalMs = totalMs;
        Warnings = warnings;
        Engine = engine;
        Voices = voices ?? new[] { new VoiceResult(schedule, totalMs, engine) };
    }

    /// <summary>
    /// Engine named by the last <c>[engine=NAME]</c> in the first section, or
    /// the compile option's engine. Ignored by the synthesizer.
    /// </summary>
    public string? Engine { get; }

    /// <summary>The first voice's schedule.</summary>
    public Schedule Schedule { get; }

    /// <summary>Longest voice length in ms, including the trailing fade-out.</summary>
    public double TotalMs { get; }

    /// <summary>Non-fatal diagnostics (unknown tokens, unmatched delimiters), in voice order.</summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// One entry per <c>[voice=N]</c> section, always at least one. Voices are
    /// simultaneous: render each and sum.
    /// </summary>
    public IReadOnlyList<VoiceResult> Voices { get; }
}
