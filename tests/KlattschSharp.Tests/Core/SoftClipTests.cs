using KlattschSharp.Dsp;

namespace KlattschSharp.Tests.Core;

public sealed class SoftClipTests
{
    [Theory]
    [InlineData(-0.5)]
    [InlineData(-0.85)]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.85)]
    public void IsIdentityInTheLinearRegion(double x)
        => Assert.Equal(x, SoftClip.Apply(x));

    [Theory]
    [InlineData(0.9)]
    [InlineData(1.0)]
    [InlineData(5.0)]
    [InlineData(100.0)]
    public void IsBoundedAbovePositiveThreshold(double x)
    {
        double y = SoftClip.Apply(x);
        Assert.True(y < 1.0);
        Assert.True(y > 0.85);
    }

    [Theory]
    [InlineData(-0.9)]
    [InlineData(-1.0)]
    [InlineData(-5.0)]
    [InlineData(-100.0)]
    public void IsBoundedBelowNegativeThreshold(double x)
    {
        double y = SoftClip.Apply(x);
        Assert.True(y > -1.0);
        Assert.True(y < -0.85);
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(5.0)]
    [InlineData(100.0)]
    public void KneeIsOddSymmetric(double x)
        => Assert.Equal(-SoftClip.Apply(x), SoftClip.Apply(-x));
}
