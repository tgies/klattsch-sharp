namespace KlattschSharp;

/// <summary>Full synthesis parameter set.</summary>
public struct Params
{
    /// <summary>Fundamental frequency in Hz.</summary>
    public double F0;

    /// <summary>Voicing mix (0..1). 1 is glottal pulse, 0 is noise excitation.</summary>
    public double Voicing;

    /// <summary>First formant center frequency in Hz.</summary>
    public double F1;

    /// <summary>First formant bandwidth in Hz.</summary>
    public double BW1;

    /// <summary>First formant amplitude (0..1).</summary>
    public double A1;

    /// <summary>Second formant center frequency in Hz.</summary>
    public double F2;

    /// <summary>Second formant bandwidth in Hz.</summary>
    public double BW2;

    /// <summary>Second formant amplitude (0..1).</summary>
    public double A2;

    /// <summary>Third formant center frequency in Hz.</summary>
    public double F3;

    /// <summary>Third formant bandwidth in Hz.</summary>
    public double BW3;

    /// <summary>Third formant amplitude (0..1).</summary>
    public double A3;

    /// <summary>Output gain multiplier.</summary>
    public double Gain;

    /// <summary>Vibrato depth: peak F0 deviation in Hz.</summary>
    public double VibratoDepth;

    /// <summary>Vibrato rate in Hz.</summary>
    public double VibratoRate;

    /// <summary>Tremolo depth: amplitude modulation amount (0..1).</summary>
    public double TremoloDepth;

    /// <summary>Tremolo rate in Hz.</summary>
    public double TremoloRate;

    /// <summary>Aspiration / breath noise (0..1).</summary>
    public double Aspiration;

    /// <summary>Spectral tilt (-0.95..0.95).</summary>
    public double Tilt;

    /// <summary>Glottal effort (0..1).</summary>
    public double Effort;

    /// <summary>The engine's initial parameter state.</summary>
    public static Params Default => new()
    {
        F0 = 120.0,
        Voicing = 0.0,
        F1 = 500.0,
        BW1 = 80.0,
        A1 = 0.0,
        F2 = 1500.0,
        BW2 = 120.0,
        A2 = 0.0,
        F3 = 2500.0,
        BW3 = 160.0,
        A3 = 0.0,
        Gain = 3.5,
        VibratoDepth = 0.0,
        VibratoRate = 5.0,
        TremoloDepth = 0.0,
        TremoloRate = 5.0,
        Aspiration = 0.0,
        Tilt = 0.0,
        Effort = 0.5,
    };

    /// <summary>
    /// Per-field linear increment <c>(target - current) / n</c> for an
    /// <paramref name="nSamples"/>-long ramp. Below 1 is treated as 1.
    /// </summary>
    public static Params ComputeIncrement(in Params target, in Params current, int nSamples)
    {
        double n = nSamples < 1 ? 1.0 : nSamples;
        return new Params
        {
            F0 = (target.F0 - current.F0) / n,
            Voicing = (target.Voicing - current.Voicing) / n,
            F1 = (target.F1 - current.F1) / n,
            BW1 = (target.BW1 - current.BW1) / n,
            A1 = (target.A1 - current.A1) / n,
            F2 = (target.F2 - current.F2) / n,
            BW2 = (target.BW2 - current.BW2) / n,
            A2 = (target.A2 - current.A2) / n,
            F3 = (target.F3 - current.F3) / n,
            BW3 = (target.BW3 - current.BW3) / n,
            A3 = (target.A3 - current.A3) / n,
            Gain = (target.Gain - current.Gain) / n,
            VibratoDepth = (target.VibratoDepth - current.VibratoDepth) / n,
            VibratoRate = (target.VibratoRate - current.VibratoRate) / n,
            TremoloDepth = (target.TremoloDepth - current.TremoloDepth) / n,
            TremoloRate = (target.TremoloRate - current.TremoloRate) / n,
            Aspiration = (target.Aspiration - current.Aspiration) / n,
            Tilt = (target.Tilt - current.Tilt) / n,
            Effort = (target.Effort - current.Effort) / n,
        };
    }

    /// <summary>Add <paramref name="increment"/> to every field in place.</summary>
    public void StepBy(in Params increment)
    {
        F0 += increment.F0;
        Voicing += increment.Voicing;
        F1 += increment.F1;
        BW1 += increment.BW1;
        A1 += increment.A1;
        F2 += increment.F2;
        BW2 += increment.BW2;
        A2 += increment.A2;
        F3 += increment.F3;
        BW3 += increment.BW3;
        A3 += increment.A3;
        Gain += increment.Gain;
        VibratoDepth += increment.VibratoDepth;
        VibratoRate += increment.VibratoRate;
        TremoloDepth += increment.TremoloDepth;
        TremoloRate += increment.TremoloRate;
        Aspiration += increment.Aspiration;
        Tilt += increment.Tilt;
        Effort += increment.Effort;
    }
}
