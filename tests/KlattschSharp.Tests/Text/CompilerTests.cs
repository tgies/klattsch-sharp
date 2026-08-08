using System;
using System.Collections.Generic;
using System.Linq;
using KlattschSharp;
using KlattschSharp.Text;

namespace KlattschSharp.Tests.Text;

public sealed class CompilerTests
{
    private static CompileResult Compile(string source)
    {
        return Compiler.Compile(source, new CompileOptions());
    }

    [Fact]
    public void EmptyInputProducesOnlyTrailingFade()
    {
        CompileResult r = Compile("");
        // SENTENCE_FINAL_HOLD_MS (0) + TRAIL_OFF_MS (150). FADE_OUT_MS (100) is
        // the fade event's transition duration, not elapsed time.
        Assert.Equal(150.0, r.TotalMs, 1e-3);
        Assert.Single(r.Schedule.Events);
    }

    [Fact]
    public void HelloCompilesToSomeEvents()
    {
        CompileResult r = Compile("HH AH L OW");
        Assert.True(r.Schedule.Events.Count >= 4);
        Assert.Empty(r.Warnings);
        Assert.True(r.TotalMs > 400.0);
    }

    [Fact]
    public void UnknownPhonemeWarns()
    {
        CompileResult r = Compile("HH XYZZY OW");
        Assert.Single(r.Warnings);
        Assert.Contains("XYZZY", r.Warnings[0]);
    }

    [Fact]
    public void UnmatchedCloseParenWarnsWithoutThrowing()
    {
        CompileResult r = Compile("AH ) AH");
        Assert.Contains(r.Warnings, w => w.Contains("unmatched"));
        // Two plain vowels emit one onset event each, plus the trailing fade.
        Assert.True(r.Schedule.Events.Count >= 3);
        Assert.Equal(2, r.Schedule.Events.Count(e => e.Target.F0.HasValue));
    }

    [Fact]
    public void UnclosedOpenParenWarnsAndFlushes()
    {
        CompileResult r = Compile("AH ( HH AH");
        Assert.Contains(r.Warnings, w => w.Contains("unclosed"));
        // AH and HH are plain, one onset event each; the grouped pair flushes at
        // one rate slot.
        Assert.Equal(3, r.Schedule.Events.Count(e => e.Target.F0.HasValue));
    }

    [Fact]
    public void BaseDirectiveSetsAbsolutePitch()
    {
        CompileResult r = Compile("AH b220 AH");
        Assert.Contains(r.Schedule.Events, e => e.Target.F0 is { } f0 && Math.Abs(f0 - 220.0) < 0.01);
    }

    [Fact]
    public void BaseDirectiveRelativeFromDefault()
    {
        CompileResult r = Compile("AH b+30 AH");
        Assert.Contains(r.Schedule.Events, e => e.Target.F0 is { } f0 && Math.Abs(f0 - 150.0) < 0.01);
    }

    [Fact]
    public void SyllableGroupCompressesToOneSlot()
    {
        CompileResult single = Compile("AH");
        CompileResult group = Compile("( HH AH )");
        Assert.True(
            Math.Abs(single.TotalMs - group.TotalMs) < 1.0,
            $"{single.TotalMs} vs {group.TotalMs}");
    }

    [Fact]
    public void NoteDirectiveSetsPitch()
    {
        CompileResult r = Compile("bA4 AH");
        Assert.Contains(r.Schedule.Events, e => e.Target.F0 is { } f0 && Math.Abs(f0 - 440.0) < 0.5);
    }

    [Fact]
    public void StressLengthensPhoneme()
    {
        CompileResult plain = Compile("AH");
        CompileResult stressed = Compile("AH'");
        // phoneRate = rate * 1.5: at the default rate of 110, a stressed phoneme
        // takes rate*0.5 (55ms) longer than a plain one.
        Assert.Equal(55.0, stressed.TotalMs - plain.TotalMs, 1e-3);
    }

    [Fact]
    public void StressLiftsStartF0()
    {
        CompileResult r = Compile("AH'");
        // Stress lifts startF0 by STRESS_F0_LIFT (8) above the base 120.
        Assert.Contains(r.Schedule.Events, e => e.Target.F0 is { } f0 && Math.Abs(f0 - 128.0) < 0.01);
    }

    // ---- [bank=NAME] ----

    [Fact]
    public void BankSwitchResolvesLaterPhonemes()
    {
        // A and I exist only in the Japanese banks.
        CompileResult r = Compile("AH [bank=ja-mokhtari-2000] A I");
        Assert.Empty(r.Warnings);
    }

    [Fact]
    public void BankResetRestoresTheInitialBank()
    {
        CompileResult r = Compile("[bank=ja-mokhtari-2000] A [bank] A");
        // The second A falls outside the English bank the compile started with.
        Assert.Single(r.Warnings);
        Assert.Contains("unknown phoneme: A", r.Warnings[0]);
    }

    [Fact]
    public void BankResetGoesToTheOptionBankNotThePreviousOne()
    {
        var opts = new CompileOptions { Bank = PhonemeBanks.Get("ja-hecko-2026") };
        CompileResult r = Compiler.Compile("[bank=ja-mokhtari-2000] A [bank] A DX", opts);
        Assert.Empty(r.Warnings);
    }

    [Fact]
    public void UnknownBankWarnsAndKeepsTheCurrentBank()
    {
        CompileResult r = Compile("AH [bank=no-such-bank] IY");
        Assert.Single(r.Warnings);
        Assert.Equal("unknown bank: no-such-bank", r.Warnings[0]);
        // IY still resolves against the bank in force before the failed switch.
        Assert.DoesNotContain(r.Warnings, w => w.Contains("unknown phoneme"));
    }

    [Fact]
    public void BankWithBrokenExtendsThrows()
    {
        var registry = new PhonemeBankRegistry();
        registry.Register(new PhonemeBankRegistry.BankDefinition(
            name: "orphan",
            entries: new Dictionary<string, PhonemeParams>(),
            extends: "no-such-parent"));
        var opts = new CompileOptions { Registry = registry };

        Assert.Throws<KeyNotFoundException>(() => Compiler.Compile("AH [bank=orphan] AH", opts));
    }

    // ---- [engine=NAME] ----

    [Fact]
    public void EngineDefaultsToNull()
    {
        Assert.Null(Compile("AH").Engine);
    }

    [Fact]
    public void EngineSwitchIsRecordedAndLastWriteWins()
    {
        CompileResult r = Compile("[engine=rodetage] AH [engine=klattschjr] IY");
        Assert.Equal("klattschjr", r.Engine);
    }

    [Fact]
    public void EngineResetRestoresTheCompileOption()
    {
        var opts = new CompileOptions { Engine = "klatt" };
        CompileResult r = Compiler.Compile("[engine=rodetage] AH [engine] IY", opts);
        Assert.Equal("klatt", r.Engine);
    }

    [Fact]
    public void EngineDoesNotAffectTheSchedule()
    {
        CompileResult plain = Compile("AH IY");
        CompileResult tagged = Compile("[engine=rodetage] AH IY");
        Assert.Equal(plain.Schedule.Events.Count, tagged.Schedule.Events.Count);
        Assert.Equal(plain.TotalMs, tagged.TotalMs, 1e-9);
    }

    // ---- uppercase extended directives ----

    [Fact]
    public void OpaqueExtendedDirectiveIsAcceptedAndInaudible()
    {
        CompileResult r = Compile("AH [OQ=0.65] IY");
        Assert.Empty(r.Warnings);

        CompileResult plain = Compile("AH IY");
        Assert.Equal(plain.Schedule.Events.Count, r.Schedule.Events.Count);
        for (int i = 0; i < plain.Schedule.Events.Count; i++)
        {
            Assert.Equal(plain.Schedule.Events[i].Target.F1, r.Schedule.Events[i].Target.F1);
        }
    }

    [Fact]
    public void LowercaseBracketNameWarnsAsAnUnknownDirective()
    {
        CompileResult r = Compile("AH [oq=1] IY");
        Assert.Single(r.Warnings);
        Assert.Equal("unknown directive: oq", r.Warnings[0]);
    }

    [Fact]
    public void ExtendedDirectiveNamingAnEngineParameterOverridesIt()
    {
        CompileResult r = Compile("AH [F1=900] IY");
        // AH's own F1 is 620; the override pins every later target at 900.
        Assert.Contains(r.Schedule.Events, e => e.Target.F1 is { } f1 && Math.Abs(f1 - 620.0) < 0.01);
        Assert.Contains(r.Schedule.Events, e => e.Target.F1 is { } f1 && Math.Abs(f1 - 900.0) < 0.01);
        // IY's own F1 (310) is shadowed.
        Assert.DoesNotContain(r.Schedule.Events, e => e.Target.F1 is { } f1 && Math.Abs(f1 - 310.0) < 0.01);
    }

    [Fact]
    public void BareExtendedResetClearsTheOverride()
    {
        CompileResult r = Compile("[F1=900] AH [F1] IY");
        Assert.Contains(r.Schedule.Events, e => e.Target.F1 is { } f1 && Math.Abs(f1 - 310.0) < 0.01);
    }

    [Fact]
    public void SeededExtraIsDeletedByBareResetRatherThanRestored()
    {
        var opts = new CompileOptions
        {
            Extras = new Dictionary<string, double> { ["F1"] = 700.0 },
        };
        CompileResult r = Compiler.Compile("AH [F1] IY", opts);
        // The seeded 700 applies to AH, then the reset drops it entirely: IY
        // renders with its own F1 rather than returning to 700.
        Assert.Contains(r.Schedule.Events, e => e.Target.F1 is { } f1 && Math.Abs(f1 - 700.0) < 0.01);
        Assert.Contains(r.Schedule.Events, e => e.Target.F1 is { } f1 && Math.Abs(f1 - 310.0) < 0.01);
    }

    [Fact]
    public void ExtendedDirectivesRideOnSilenceTargetsToo()
    {
        CompileResult r = Compile("[A1=0.5] AH , IY");
        // Pauses emit a silence target with A1 zeroed; the override outranks it.
        Assert.DoesNotContain(r.Schedule.Events, e => e.Target.A1 is { } a1 && a1 == 0.0);
    }

    // ---- [voice=N] ----

    [Fact]
    public void SourceWithoutMarkersIsOneVoice()
    {
        CompileResult r = Compile("AH IY");
        Assert.Single(r.Voices);
        Assert.Same(r.Schedule, r.Voices[0].Schedule);
        Assert.Equal(r.TotalMs, r.Voices[0].TotalMs, 1e-9);
    }

    [Fact]
    public void VoiceMarkerSplitsIntoSimultaneousSections()
    {
        CompileResult r = Compile("AH IY [voice=1] OW UW");
        Assert.Equal(2, r.Voices.Count);
        // Both sections hold two phonemes, so they are the same length.
        Assert.Equal(r.Voices[0].TotalMs, r.Voices[1].TotalMs, 1e-9);
        Assert.Equal(r.TotalMs, r.Voices[0].TotalMs, 1e-9);
    }

    [Fact]
    public void EachVoiceStartsFromTheCompileOptions()
    {
        CompileResult r = Compile("b200 AH [voice=1] IY");
        // The first section's base-pitch directive does not leak into the second.
        Assert.Contains(r.Voices[0].Schedule.Events, e => e.Target.F0 is { } f0 && Math.Abs(f0 - 200.0) < 0.01);
        Assert.DoesNotContain(r.Voices[1].Schedule.Events, e => e.Target.F0 is { } f0 && Math.Abs(f0 - 200.0) < 0.01);
        Assert.Contains(r.Voices[1].Schedule.Events, e => e.Target.F0 is { } f0 && Math.Abs(f0 - 120.0) < 0.01);
    }

    [Fact]
    public void VoiceSectionsArePositionalAndTheNumberIsIgnored()
    {
        CompileResult numbered = Compile("AH [voice=7] IY");
        CompileResult sequential = Compile("AH [voice=1] IY");
        Assert.Equal(sequential.Voices.Count, numbered.Voices.Count);
        Assert.Equal(sequential.Voices[1].TotalMs, numbered.Voices[1].TotalMs, 1e-9);
    }

    [Fact]
    public void TotalMsIsTheLongestVoice()
    {
        CompileResult r = Compile("AH [voice=1] IY OW UW");
        Assert.True(r.Voices[1].TotalMs > r.Voices[0].TotalMs);
        Assert.Equal(r.Voices[1].TotalMs, r.TotalMs, 1e-9);
    }

    [Fact]
    public void WarningsFromEveryVoiceAreMerged()
    {
        CompileResult r = Compile("XYZZY [voice=1] PLUGH");
        Assert.Equal(2, r.Warnings.Count);
        Assert.Contains("XYZZY", r.Warnings[0]);
        Assert.Contains("PLUGH", r.Warnings[1]);
    }

    // A bare [voice] has no reset form; it lexes as an unknown token.
    [Fact]
    public void BareVoiceBracketIsAnUnknownToken()
    {
        CompileResult r = Compile("AH [voice] IY");
        Assert.Single(r.Voices);
        Assert.Single(r.Warnings);
        Assert.Equal("unknown token: [voice]", r.Warnings[0]);
    }
}
