using KlattschSharp.Text;

namespace KlattschSharp.Tests.Text;

public sealed class JapaneseBankTests
{
    private static IPhonemeBank Bank => PhonemeBanks.Get("ja-mokhtari-2000");

    [Fact]
    public void BankHasFortySixNames()
    {
        Assert.Equal(46, Bank.Names.Count);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("E")]
    [InlineData("I")]
    [InlineData("O")]
    [InlineData("U")]
    [InlineData("DX")]
    public void JapaneseKeysPresent(string code)
    {
        Assert.Contains(code, Bank.Names);
    }

    [Theory]
    [InlineData("AH")]
    [InlineData("K")]
    [InlineData("P")]
    public void InheritedEnglishKeysPresent(string code)
    {
        Assert.Contains(code, Bank.Names);
    }

    [Fact]
    public void AVowelMatchesJsBank()
    {
        Assert.True(Bank.TryGet("A", out PhonemeParams p));
        Assert.Equal(744.0, p.F1);
        Assert.Equal(1240.0, p.F2);
        Assert.Equal(2426.0, p.F3);
        Assert.Equal(1.0, p.Voicing);
        Assert.False(p.IsStop);
        Assert.Null(p.GlideTo);
    }

    [Fact]
    public void DXMatchesJsBank()
    {
        Assert.True(Bank.TryGet("DX", out PhonemeParams p));
        Assert.Equal(255.0, p.F1);
        Assert.Equal(1325.0, p.F2);
        Assert.Equal(2740.0, p.F3);
        Assert.Equal(0.6, p.Voicing);
        Assert.True(p.IsStop);
    }

    [Fact]
    public void KInheritsFromEnglish()
    {
        Assert.True(Bank.TryGet("K", out PhonemeParams p));
        Assert.Equal(300.0, p.F1);
        Assert.True(p.IsStop);
    }

    [Fact]
    public void AllBanksInNames()
    {
        Assert.Contains("klatt1980-en", PhonemeBanks.Names);
        Assert.Contains("ja-mokhtari-2000", PhonemeBanks.Names);
        Assert.Contains("ja-hecko-2026", PhonemeBanks.Names);
    }
}

public sealed class JapaneseHeckoBankTests
{
    private static IPhonemeBank Bank => PhonemeBanks.Get("ja-hecko-2026");

    [Theory]
    [InlineData("A")]
    [InlineData("E")]
    [InlineData("I")]
    [InlineData("O")]
    [InlineData("U")]
    [InlineData("DX")]
    public void JapaneseKeysPresent(string code)
    {
        Assert.Contains(code, Bank.Names);
    }

    [Fact]
    public void AVowelMatchesJsBank()
    {
        Assert.True(Bank.TryGet("A", out PhonemeParams p));
        Assert.Equal(750.0, p.F1);
        Assert.Equal(1250.0, p.F2);
        Assert.Equal(2250.0, p.F3);
        Assert.Equal(1.0, p.Voicing);
        Assert.False(p.IsStop);
        Assert.Null(p.GlideTo);
    }

    [Fact]
    public void UVowelMatchesJsBank()
    {
        Assert.True(Bank.TryGet("U", out PhonemeParams p));
        Assert.Equal(450.0, p.F1);
        Assert.Equal(1250.0, p.F2);
        Assert.Equal(2150.0, p.F3);
    }

    [Fact]
    public void DXMatchesJsBank()
    {
        Assert.True(Bank.TryGet("DX", out PhonemeParams p));
        Assert.Equal(255.0, p.F1);
        Assert.Equal(0.6, p.Voicing);
        Assert.True(p.IsStop);
    }

    [Fact]
    public void KInheritsFromEnglish()
    {
        Assert.True(Bank.TryGet("K", out PhonemeParams p));
        Assert.Equal(300.0, p.F1);
        Assert.True(p.IsStop);
    }
}
