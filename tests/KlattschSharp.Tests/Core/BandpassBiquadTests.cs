using System;
using KlattschSharp.Dsp;

namespace KlattschSharp.Tests.Core;

public sealed class BandpassBiquadTests
{
    [Fact]
    public void ZeroInputProducesZeroOutput()
    {
        var bp = new BandpassBiquad();
        bp.SetFreq(1000.0, 100.0, 48_000.0);
        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(0.0, bp.Process(0.0));
        }
    }

    [Fact]
    public void SetFreqWithIdenticalArgsLeavesCoefficientsUnchanged()
    {
        var bp = new BandpassBiquad();
        bp.SetFreq(500.0, 100.0, 48_000.0);
        var snapshot = bp.SnapshotCoefficients();
        bp.SetFreq(500.0, 100.0, 48_000.0);
        Assert.Equal(snapshot, bp.SnapshotCoefficients());
    }

    [Fact]
    public void DrivenAtCenterFrequencyPeaksAboveUnity()
    {
        const double sr = 48_000.0;
        const double f = 500.0;
        var bp = new BandpassBiquad();
        bp.SetFreq(f, 50.0, sr);
        double peak = 0.0;
        int n = (int)sr;
        for (int i = 0; i < n; i++)
        {
            double x = Math.Sin(2.0 * Math.PI * f * i / sr);
            double y = bp.Process(x);
            if (i > n / 4) peak = Math.Max(peak, Math.Abs(y));
        }
        Assert.True(peak > 1.0, $"expected resonance above unity, got {peak}");
    }

    [Fact]
    public void ResetZeroesFilterState()
    {
        var bp = new BandpassBiquad();
        bp.SetFreq(500.0, 100.0, 48_000.0);
        for (int i = 0; i < 100; i++) bp.Process(0.5);
        bp.Reset();
        Assert.Equal(0.0, bp.Process(0.0));
    }
}
