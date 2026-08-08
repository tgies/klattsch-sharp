using System;

namespace KlattschSharp.Dsp;

/// <summary>Constant-skirt-gain bandpass biquad (RBJ Audio EQ Cookbook).</summary>
internal sealed class BandpassBiquad
{
    private double _x1, _x2, _y1, _y2;
    private double _b0, _b1, _b2, _a1, _a2;
    private double _lastF = -1.0;
    private double _lastBW = -1.0;

    /// <summary>
    /// Recompute coefficients if (f, bw) differ from the previous call.
    /// Frequency clamps to [40 Hz, sr * 0.45], bandwidth to >= 20 Hz.
    /// </summary>
    public void SetFreq(double f, double bw, double sr)
    {
        if (f == _lastF && bw == _lastBW) return;
        _lastF = f;
        _lastBW = bw;

        double ff = MathPolyfill.Clamp(f, 40.0, sr * 0.45);
        double bbw = Math.Max(bw, 20.0);
        double w0 = 2.0 * Math.PI * ff / sr;
        double cosw0 = Math.Cos(w0);
        double sinw0 = Math.Sin(w0);
        double q = ff / bbw;
        double alpha = sinw0 / (2.0 * q);
        double a0 = 1.0 + alpha;

        _b0 = alpha / a0;
        _b1 = 0.0;
        _b2 = -alpha / a0;
        _a1 = -2.0 * cosw0 / a0;
        _a2 = (1.0 - alpha) / a0;
    }

    public double Process(double input)
    {
        double y = _b0 * input + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
        _x2 = _x1;
        _x1 = input;
        _y2 = _y1;
        _y1 = y;
        return y;
    }

    public void Reset()
    {
        _x1 = 0;
        _x2 = 0;
        _y1 = 0;
        _y2 = 0;
    }

    /// <summary>Coefficient snapshot for testing.</summary>
    public (double B0, double B1, double B2, double A1, double A2) SnapshotCoefficients()
        => (_b0, _b1, _b2, _a1, _a2);
}
