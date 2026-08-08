using System;

namespace KlattschSharp.Dsp;

internal static class MathPolyfill
{
#if NETSTANDARD2_0
    public static double Clamp(double v, double min, double max)
        => v < min ? min : (v > max ? max : v);
#else
    public static double Clamp(double v, double min, double max) => Math.Clamp(v, min, max);
#endif
}
