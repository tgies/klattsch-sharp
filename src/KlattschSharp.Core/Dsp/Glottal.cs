using System;

namespace KlattschSharp.Dsp;

/// <summary>Rosenberg glottal pulse.</summary>
internal static class Glottal
{
    /// <summary>
    /// Derivative of the Rosenberg pulse. <paramref name="phase"/> is normalized
    /// to [0, 1); <paramref name="effort"/> (0..1) runs lax to tense.
    /// </summary>
    public static double Pulse(double phase, double effort)
    {
        double e = effort < 0.0 ? 0.0 : (effort > 1.0 ? 1.0 : effort);
        double tp = 0.5 - (e * 0.2);
        double tn = 0.25 - (e * 0.17);
        const double Norm = 0.1;
        double result;
        if (phase < tp)
        {
            result = Norm * 0.5 * (Math.PI / tp) * Math.Sin(Math.PI * phase / tp);
        }
        else if (phase < tp + tn)
        {
            result = -Norm * (Math.PI / (2.0 * tn)) * Math.Sin(Math.PI * (phase - tp) / (2.0 * tn));
        }
        else
        {
            result = 0.0;
        }

        return result;
    }
}
