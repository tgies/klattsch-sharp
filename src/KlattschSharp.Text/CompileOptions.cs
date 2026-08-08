using System.Collections.Generic;

namespace KlattschSharp.Text;

/// <summary>
/// The initial voice state and phoneme bank a compile starts from. Bare
/// directive letters (e.g. <c>b</c>) reset to these values.
/// </summary>
public sealed class CompileOptions
{
    /// <summary>Output sample rate in Hz. Quantizes the millisecond schedule.</summary>
    public uint SampleRate { get; set; } = 48000;

    /// <summary>Initial base pitch in Hz.</summary>
    public double BaseF0 { get; set; } = 120.0;

    /// <summary>Per-phoneme duration in ms (one rate slot).</summary>
    public double Rate { get; set; } = 110.0;

    /// <summary>Formant frequency / bandwidth scale factor.</summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>Initial vibrato depth in Hz.</summary>
    public double VibratoDepth { get; set; } = 0.0;

    /// <summary>Initial vibrato rate in Hz.</summary>
    public double VibratoRate { get; set; } = 5.0;

    /// <summary>Initial tremolo depth (0..1).</summary>
    public double TremoloDepth { get; set; } = 0.0;

    /// <summary>Initial tremolo rate in Hz.</summary>
    public double TremoloRate { get; set; } = 5.0;

    /// <summary>Initial aspiration / breathiness (0..1).</summary>
    public double Aspiration { get; set; } = 0.0;

    /// <summary>Initial spectral tilt.</summary>
    public double Tilt { get; set; } = 0.0;

    /// <summary>Initial glottal effort (0..1).</summary>
    public double Effort { get; set; } = 0.5;

    /// <summary>Default transition duration in ms for a plain phoneme onset.</summary>
    public double DefaultTransitionMs { get; set; } = 35.0;

    /// <summary>Phoneme source the compile starts with. <c>[bank]</c> restores it.</summary>
    public IPhonemeBank Bank { get; set; } = PhonemeBanks.English;

    /// <summary>Registry that <c>[bank=NAME]</c> resolves names against.</summary>
    public PhonemeBankRegistry Registry { get; set; } = PhonemeBanks.Registry;

    /// <summary>
    /// Engine name each voice section starts with; <c>[engine]</c> restores it.
    /// Metadata for a consuming engine, ignored by the synthesizer.
    /// </summary>
    public string? Engine { get; set; }

    /// <summary>
    /// Extended directives each voice section starts with, as though set by an
    /// uppercase bracket directive. A bare <c>[NAME]</c> deletes the entry
    /// rather than restoring the value seeded here.
    /// </summary>
    public IReadOnlyDictionary<string, double>? Extras { get; set; }
}
