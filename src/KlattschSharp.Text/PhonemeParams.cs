namespace KlattschSharp.Text;

/// <summary>A diphthong's offglide endpoint, overlaying the base phoneme's F1/F2/F3.</summary>
public readonly struct GlideTo
{
    /// <summary>Offglide endpoint at the given formant frequencies in Hz.</summary>
    public GlideTo(double f1, double f2, double f3)
    {
        F1 = f1;
        F2 = f2;
        F3 = f3;
    }

    /// <summary>First formant endpoint frequency in Hz.</summary>
    public double F1 { get; }

    /// <summary>Second formant endpoint frequency in Hz.</summary>
    public double F2 { get; }

    /// <summary>Third formant endpoint frequency in Hz.</summary>
    public double F3 { get; }
}

/// <summary>Phoneme-level synthesis parameters resolved from an <see cref="IPhonemeBank"/>.</summary>
public readonly struct PhonemeParams
{
    /// <summary>Formant frequencies, bandwidths, and amplitudes, plus voicing and stop/glide behavior.</summary>
    public PhonemeParams(
        double f1,
        double f2,
        double f3,
        double bw1,
        double bw2,
        double bw3,
        double a1,
        double a2,
        double a3,
        double voicing,
        GlideTo? glideTo = null,
        bool isStop = false)
    {
        F1 = f1;
        F2 = f2;
        F3 = f3;
        BW1 = bw1;
        BW2 = bw2;
        BW3 = bw3;
        A1 = a1;
        A2 = a2;
        A3 = a3;
        Voicing = voicing;
        GlideTo = glideTo;
        IsStop = isStop;
    }

    /// <summary>First formant center frequency in Hz.</summary>
    public double F1 { get; }

    /// <summary>Second formant center frequency in Hz.</summary>
    public double F2 { get; }

    /// <summary>Third formant center frequency in Hz.</summary>
    public double F3 { get; }

    /// <summary>First formant bandwidth in Hz.</summary>
    public double BW1 { get; }

    /// <summary>Second formant bandwidth in Hz.</summary>
    public double BW2 { get; }

    /// <summary>Third formant bandwidth in Hz.</summary>
    public double BW3 { get; }

    /// <summary>First formant amplitude (0..1).</summary>
    public double A1 { get; }

    /// <summary>Second formant amplitude (0..1).</summary>
    public double A2 { get; }

    /// <summary>Third formant amplitude (0..1).</summary>
    public double A3 { get; }

    /// <summary>
    /// Voicing (0..1): sonorants 1.0, voiceless fricatives 0.0, voiced
    /// fricatives ~0.45, voiced plosives ~0.6.
    /// </summary>
    public double Voicing { get; }

    /// <summary>Diphthong offglide endpoint, if any.</summary>
    public GlideTo? GlideTo { get; }

    /// <summary>Plosive / affricate: render as a brief silence followed by a burst.</summary>
    public bool IsStop { get; }
}
