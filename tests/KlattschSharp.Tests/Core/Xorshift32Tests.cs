using System;
using KlattschSharp.Dsp;

namespace KlattschSharp.Tests.Core;

public sealed class Xorshift32Tests
{
    [Fact]
    public void DefaultSeedMatchesTheJsBitsForTheFirstThreeSamples()
    {
        var rng = new Xorshift32();
        Assert.Equal(0xbf127469u, BitConverter.SingleToUInt32Bits((float)rng.NextSample()));
        Assert.Equal(0xbd410bebu, BitConverter.SingleToUInt32Bits((float)rng.NextSample()));
        Assert.Equal(0xbf1ce0a7u, BitConverter.SingleToUInt32Bits((float)rng.NextSample()));
    }

    [Fact]
    public void DistinctSeedsDecorrelateOutput()
    {
        var a = new Xorshift32(0xACE1ACE1u);
        var b = new Xorshift32(0xDEADBEEFu);
        bool different = false;
        for (int i = 0; i < 64; i++)
        {
            if (a.NextSample() != b.NextSample()) { different = true; break; }
        }
        Assert.True(different);
    }
}
