using System;
using System.Collections.Generic;
using System.Linq;
using KlattschSharp.Text;

namespace KlattschSharp.Tests.Text;

public sealed class PhonemeBankRegistryTests
{
    // ---- helpers ----

    private static PhonemeParams P(double f1) =>
        new PhonemeParams(f1, 1000, 2000, 50, 100, 200, 1, 0.5, 0.3, 1);

    private static IReadOnlyList<KeyValuePair<string, PhonemeParams>> Entries(
        params (string code, PhonemeParams p)[] pairs)
    {
        var entries = new KeyValuePair<string, PhonemeParams>[pairs.Length];
        for (int i = 0; i < pairs.Length; i++)
        {
            entries[i] = new KeyValuePair<string, PhonemeParams>(pairs[i].code, pairs[i].p);
        }
        return entries;
    }

    // ---- PhonemeBanks static surface ----

    [Fact]
    public void EnglishHasFortyNamesViaGet()
    {
        Assert.Equal(40, PhonemeBanks.Get("klatt1980-en").Names.Count);
    }

    [Fact]
    public void EnglishPropertyAndGetAreEquivalent()
    {
        IPhonemeBank byGet = PhonemeBanks.Get("klatt1980-en");
        IPhonemeBank byProp = PhonemeBanks.English;

        Assert.True(byProp.TryGet("AH", out PhonemeParams p));
        Assert.Equal(620.0, p.F1);
        Assert.True(byGet.TryGet("AH", out PhonemeParams p2));
        Assert.Equal(620.0, p2.F1);
    }

    [Fact]
    public void EnglishPIsStop()
    {
        Assert.True(PhonemeBanks.English.TryGet("P", out PhonemeParams p));
        Assert.True(p.IsStop);
    }

    [Fact]
    public void NamesIncludesEnglishBank()
    {
        Assert.Contains("klatt1980-en", PhonemeBanks.Names);
    }

    [Fact]
    public void EnglishBankNameProperty()
    {
        Assert.Equal("klatt1980-en", PhonemeBanks.English.Name);
    }

    [Fact]
    public void EnglishBankLanguageProperty()
    {
        Assert.Equal("en-US", PhonemeBanks.English.Language);
    }

    // ---- extends layering ----

    [Fact]
    public void ExtendsLayeringLetsAChildOverrideAParentEntry()
    {
        var reg = new PhonemeBankRegistry();
        var parentX = P(100.0);
        var parentY = P(200.0);
        var childX = P(999.0);
        var childZ = P(300.0);

        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "parent",
            entries: Entries(("X", parentX), ("Y", parentY))));
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "child",
            entries: Entries(("X", childX), ("Z", childZ)),
            extends: "parent"));

        IPhonemeBank resolved = reg.Resolve("child");

        // "X" overridden by child
        Assert.True(resolved.TryGet("X", out PhonemeParams rx));
        Assert.Equal(999.0, rx.F1);

        // "Y" inherited from parent
        Assert.True(resolved.TryGet("Y", out PhonemeParams ry));
        Assert.Equal(200.0, ry.F1);

        // "Z" added by child
        Assert.True(resolved.TryGet("Z", out PhonemeParams rz));
        Assert.Equal(300.0, rz.F1);
    }

    [Fact]
    public void ExtendsLayeringMakesNamesTheUnionOfBothBanks()
    {
        var reg = new PhonemeBankRegistry();
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "parent",
            entries: Entries(("X", P(1.0)), ("Y", P(2.0)))));
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "child",
            entries: Entries(("X", P(99.0)), ("Z", P(3.0))),
            extends: "parent"));

        IPhonemeBank resolved = reg.Resolve("child");

        var names = new HashSet<string>(resolved.Names, StringComparer.Ordinal);
        Assert.Contains("X", names);
        Assert.Contains("Y", names);
        Assert.Contains("Z", names);
        Assert.Equal(3, names.Count);
    }

    [Fact]
    public void ExtendsLayeringLeavesTheParentUnaffected()
    {
        var reg = new PhonemeBankRegistry();
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "parent",
            entries: Entries(("X", P(100.0)))));
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "child",
            entries: Entries(("X", P(999.0))),
            extends: "parent"));

        IPhonemeBank parent = reg.Resolve("parent");
        Assert.True(parent.TryGet("X", out PhonemeParams px));
        Assert.Equal(100.0, px.F1);
    }

    // ---- name order ----

    [Fact]
    public void ExtendsLayeringKeepsParentOrderThenAppendsNewNames()
    {
        var reg = new PhonemeBankRegistry();
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "parent",
            entries: Entries(("X", P(1.0)), ("Y", P(2.0)), ("Z", P(3.0)))));
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "child",
            entries: Entries(("Y", P(99.0)), ("W", P(4.0))),
            extends: "parent"));

        // The overridden "Y" holds its inherited position; only "W" is appended.
        Assert.Equal(new[] { "X", "Y", "Z", "W" }, reg.Resolve("child").Names);
    }

    [Fact]
    public void RegistryNamesFollowRegistrationOrder()
    {
        var reg = new PhonemeBankRegistry();
        reg.Register(new PhonemeBankRegistry.BankDefinition("zebra", Entries(("X", P(1.0)))));
        reg.Register(new PhonemeBankRegistry.BankDefinition("alpha", Entries(("Y", P(2.0)))));
        reg.Register(new PhonemeBankRegistry.BankDefinition("zebra", Entries(("X", P(9.0)))));

        Assert.Equal(new[] { "zebra", "alpha" }, reg.Names);
    }

    [Fact]
    public void EnglishBankNamesAreInKlattTableOrder()
    {
        // This order is the index space downstream consumers address phonemes by
        Assert.Equal(
            new[]
            {
                "IY", "IH", "EH", "AE", "AA", "AO", "AH", "UH", "UW", "ER",
                "AY", "AW", "EY", "OW", "OY",
                "W", "Y", "R", "L",
                "M", "N", "NG",
                "F", "TH", "S", "SH",
                "V", "DH", "Z", "ZH",
                "HH",
                "P", "B", "T", "D", "K", "G",
                "CH", "JH",
                "_",
            },
            PhonemeBanks.English.Names);
    }

    [Fact]
    public void JapaneseBanksAppendTheirVowelsAfterTheEnglishNames()
    {
        IReadOnlyList<string> english = PhonemeBanks.English.Names;
        IReadOnlyList<string> ja = PhonemeBanks.Get("ja-mokhtari-2000").Names;

        Assert.Equal(english, ja.Take(english.Count));
        Assert.Equal(new[] { "I", "E", "A", "O", "U", "DX" }, ja.Skip(english.Count));
    }

    // ---- cycle detection ----

    [Fact]
    public void SelfExtendingBankThrows()
    {
        var reg = new PhonemeBankRegistry();
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "a",
            entries: Entries(("X", P(1.0))),
            extends: "a"));

        var ex = Assert.Throws<InvalidOperationException>(() => reg.Resolve("a"));
        Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("a", ex.Message);
    }

    [Fact]
    public void TwoBankExtendsCycleThrows()
    {
        var reg = new PhonemeBankRegistry();
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "a",
            entries: Entries(("X", P(1.0))),
            extends: "b"));
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "b",
            entries: Entries(("Y", P(2.0))),
            extends: "a"));

        var ex = Assert.Throws<InvalidOperationException>(() => reg.Resolve("a"));
        Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("a", ex.Message);
        Assert.Contains("b", ex.Message);
    }

    // ---- unknown bank ----

    [Fact]
    public void ResolveThrowsForAnUnknownBank()
    {
        var reg = new PhonemeBankRegistry();
        Assert.Throws<KeyNotFoundException>(() => reg.Resolve("no-such-bank"));
    }

    [Fact]
    public void PhonemeBanksGetThrowsForAnUnknownBank()
    {
        Assert.Throws<KeyNotFoundException>(() => PhonemeBanks.Get("no-such-bank"));
    }


    [Fact]
    public void TryGetReturnsFalseForANullCode()
    {
        var reg = new PhonemeBankRegistry();
        reg.Register(new PhonemeBankRegistry.BankDefinition(
            name: "a",
            entries: Entries(("X", P(1.0)))));
        IPhonemeBank bank = reg.Resolve("a");
        Assert.False(bank.TryGet(null!, out _));
    }
}
