using KlattschSharp;

namespace KlattschSharp.Tests.Core;

public sealed class ParamsTests
{
    [Fact]
    public void DefaultsMatchTheJsConstants()
    {
        var p = Params.Default;
        Assert.Equal(120.0, p.F0);
        Assert.Equal(0.0, p.Voicing);
        Assert.Equal(500.0, p.F1);
        Assert.Equal(80.0, p.BW1);
        Assert.Equal(0.0, p.A1);
        Assert.Equal(1500.0, p.F2);
        Assert.Equal(120.0, p.BW2);
        Assert.Equal(0.0, p.A2);
        Assert.Equal(2500.0, p.F3);
        Assert.Equal(160.0, p.BW3);
        Assert.Equal(0.0, p.A3);
        Assert.Equal(3.5, p.Gain);
        Assert.Equal(0.0, p.VibratoDepth);
        Assert.Equal(5.0, p.VibratoRate);
        Assert.Equal(0.0, p.TremoloDepth);
        Assert.Equal(5.0, p.TremoloRate);
        Assert.Equal(0.0, p.Aspiration);
        Assert.Equal(0.0, p.Tilt);
        Assert.Equal(0.5, p.Effort);
    }

    [Fact]
    public void ApplyToOverridesOnlyTheFieldsThatAreSet()
    {
        var p = Params.Default;
        var u = new ParamUpdate { F0 = 220.0, A1 = 1.0 };
        u.ApplyTo(ref p);
        Assert.Equal(220.0, p.F0);
        Assert.Equal(1.0, p.A1);
        Assert.Equal(500.0, p.F1); // untouched
        Assert.Equal(3.5, p.Gain); // untouched
    }

}
