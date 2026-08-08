namespace KlattschSharp.Dsp;

/// <summary>Soft clipper.</summary>
internal static class SoftClip
{
    /// <summary>
    /// Soft-clip <paramref name="x"/>: identity in [-0.85, 0.85], smooth knee
    /// outside, output bounded in (-1, 1).
    /// </summary>
    public static double Apply(double x)
    {
        const double T = 0.85;
        double a = x < 0.0 ? -x : x;
        if (a <= T)
        {
            return x;
        }

        double sign = x < 0.0 ? -1.0 : 1.0;
        double excess = a - T;
        return sign * (T + ((1.0 - T) * excess / (excess + 1.0)));
    }
}
