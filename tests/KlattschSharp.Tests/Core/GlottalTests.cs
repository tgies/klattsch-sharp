using KlattschSharp.Dsp;

namespace KlattschSharp.Tests.Core;

public sealed class GlottalTests
{
    [Fact]
    public void ClosedPhaseIsExactlyZero()
    {
        // effort=0.5 -> Tp=0.4, Tn=0.165 -> closed phase begins at 0.565.
        Assert.Equal(0.0, Glottal.Pulse(0.7, 0.5));
        Assert.Equal(0.0, Glottal.Pulse(0.99, 0.5));
    }

    [Fact]
    public void EffortIsClamped()
    {
        Assert.Equal(Glottal.Pulse(0.1, 0.0), Glottal.Pulse(0.1, -1.0));
        Assert.Equal(Glottal.Pulse(0.1, 1.0), Glottal.Pulse(0.1, 2.0));
    }
}
