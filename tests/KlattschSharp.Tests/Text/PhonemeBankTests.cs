using KlattschSharp.Text;

namespace KlattschSharp.Tests.Text;

public sealed class PhonemeBankTests
{
    [Fact]
    public void EnglishHasExactlyFortyNames()
    {
        Assert.Equal(40, PhonemeBanks.English.Names.Count);
    }

    [Fact]
    public void AhResolvesToExpectedFormants()
    {
        Assert.True(PhonemeBanks.English.TryGet("AH", out PhonemeParams p));
        Assert.Equal(620.0, p.F1);
        Assert.Equal(1220.0, p.F2);
        Assert.False(p.IsStop);
        Assert.Null(p.GlideTo);
    }

    [Fact]
    public void PIsStop()
    {
        Assert.True(PhonemeBanks.English.TryGet("P", out PhonemeParams p));
        Assert.True(p.IsStop);
    }

    [Fact]
    public void IyHasGlide()
    {
        Assert.True(PhonemeBanks.English.TryGet("IY", out PhonemeParams p));
        Assert.NotNull(p.GlideTo);
        Assert.Equal(290.0, p.GlideTo!.Value.F1);
    }

    [Theory]
    [InlineData("V")]
    [InlineData("DH")]
    [InlineData("Z")]
    [InlineData("ZH")]
    public void VoicedFricativesHaveVoicedAmpAndBandwidths(string code)
    {
        Assert.True(PhonemeBanks.English.TryGet(code, out PhonemeParams p));
        Assert.Equal(0.4, p.A1, 6);
        Assert.Equal(0.45, p.Voicing, 6);
        Assert.Equal(80.0, p.BW1);
        Assert.Equal(100.0, p.BW2);
        Assert.Equal(800.0, p.BW3);
    }

    [Fact]
    public void UnknownCodeReturnsFalse()
    {
        Assert.False(PhonemeBanks.English.TryGet("XYZZY", out _));
    }

    [Fact]
    public void EmptyCodeReturnsFalse()
    {
        Assert.False(PhonemeBanks.English.TryGet(string.Empty, out _));
    }

    [Fact]
    public void EveryNameResolves()
    {
        foreach (string name in PhonemeBanks.English.Names)
        {
            Assert.True(PhonemeBanks.English.TryGet(name, out _), $"{name} should resolve");
        }
    }
}
