using System;

using KlattschSharp;

namespace KlattschSharp.Tests.Core;

public sealed class FormantSynthTests
{
    private const int Sr = 48_000;

    private static ParamUpdate VowelTarget() => new()
    {
        F0 = 220.0,
        Voicing = 1.0,
        A1 = 1.0,
        A2 = 0.9,
        A3 = 0.7,
    };

    private static float Peak(ReadOnlySpan<float> buf)
    {
        float p = 0f;
        foreach (float s in buf)
        {
            float a = Math.Abs(s);
            if (a > p)
            {
                p = a;
            }
        }

        return p;
    }

    [Fact]
    public void DefaultStateIsSilentForOneSecond()
    {
        var synth = new FormantSynth(Sr);
        var buf = new float[Sr];
        synth.Process(buf, 0, buf.Length);
        foreach (float s in buf)
        {
            Assert.Equal(0f, s);
        }
    }

    [Fact]
    public void VoicedConfigProducesAudibleOutput()
    {
        var synth = new FormantSynth(Sr);
        synth.SetTarget(VowelTarget(), 1);
        var buf = new float[4800];
        synth.Process(buf, 0, buf.Length);
        Assert.True(Peak(buf) > 0.01f, $"expected audible output, peak {Peak(buf)}");
    }

    [Fact]
    public void OneShotChunkedAndSingleSampleRendersAreBitIdentical()
    {
        const int n = 48_000;

        var oneShot = new FormantSynth(Sr);
        oneShot.SetTarget(VowelTarget(), 1);
        var bufOneShot = new float[n];
        oneShot.Process(bufOneShot, 0, n);

        var chunked = new FormantSynth(Sr);
        chunked.SetTarget(VowelTarget(), 1);
        var bufChunked = new float[n];
        for (int off = 0; off < n; off += 256)
        {
            int count = Math.Min(256, n - off);
            chunked.Process(bufChunked, off, count);
        }

        var single = new FormantSynth(Sr);
        single.SetTarget(VowelTarget(), 1);
        var bufSingle = new float[n];
        for (int i = 0; i < n; i++)
        {
            single.Process(bufSingle, i, 1);
        }

        for (int i = 0; i < n; i++)
        {
            Assert.True(bufOneShot[i] == bufChunked[i], $"one-shot vs 256-chunk diverged at {i}: {bufOneShot[i]} vs {bufChunked[i]}");
            Assert.True(bufOneShot[i] == bufSingle[i], $"one-shot vs 1-sample diverged at {i}: {bufOneShot[i]} vs {bufSingle[i]}");
        }

        var spanSynth = new FormantSynth(Sr);
        spanSynth.SetTarget(VowelTarget(), 1);
        var bufSpan = new float[n];
        spanSynth.Process(bufSpan.AsSpan());
        for (int i = 0; i < n; i++)
        {
            Assert.True(bufOneShot[i] == bufSpan[i], $"array vs Span diverged at {i}: {bufOneShot[i]} vs {bufSpan[i]}");
        }
    }

    [Fact]
    public void DistinctSeedsDecorrelateUnvoicedOutput()
    {
        var unvoiced = new ParamUpdate { Voicing = 0.0, Aspiration = 1.0, A1 = 1.0 };

        var a = new FormantSynth(Sr, 0xABCD_1234u);
        var b = new FormantSynth(Sr, 0x9876_5432u);
        a.SetTarget(unvoiced, 1);
        b.SetTarget(unvoiced, 1);

        var bufA = new float[1024];
        var bufB = new float[1024];
        a.Process(bufA, 0, bufA.Length);
        b.Process(bufB, 0, bufB.Length);

        int diff = 0;
        for (int i = 0; i < bufA.Length; i++)
        {
            if (Math.Abs(bufA[i] - bufB[i]) > 1e-6f)
            {
                diff++;
            }
        }

        Assert.True(diff > 100, $"expected decorrelated output, only {diff} samples differed");
    }

    [Fact]
    public void ScheduledEventAppliesAtItsSample()
    {
        var schedule = Schedule.FromSampleEvents((uint)Sr, new[]
        {
            new ScheduleEvent(100UL, new ParamUpdate { F0 = 220.0 }, 1u),
        });

        var synth = new FormantSynth(Sr);
        synth.QueueSchedule(schedule);

        var buf = new float[101];
        synth.Process(buf, 0, 100);
        Assert.Equal(120.0, synth.CurrentSnapshot().F0);

        synth.Process(buf, 100, 1);
        Assert.Equal(220.0, synth.CurrentSnapshot().F0);
    }

    [Fact]
    public void NegativeStartCounterDelaysTheFirstEvent()
    {
        var schedule = Schedule.FromSampleEvents((uint)Sr, new[]
        {
            new ScheduleEvent(0UL, new ParamUpdate { F0 = 220.0 }, 1u),
        });

        var synth = new FormantSynth(Sr);
        synth.QueueSchedule(schedule, startCounter: -50);

        var buf = new float[51];
        synth.Process(buf, 0, 50);
        Assert.Equal(120.0, synth.CurrentSnapshot().F0);

        synth.Process(buf, 50, 1);
        Assert.Equal(220.0, synth.CurrentSnapshot().F0);
    }

    [Fact]
    public void PositiveStartCounterDrainsPastEventsImmediately()
    {
        var schedule = Schedule.FromSampleEvents((uint)Sr, new[]
        {
            new ScheduleEvent(100UL, new ParamUpdate { F0 = 220.0 }, 1u),
            new ScheduleEvent(500UL, new ParamUpdate { F0 = 330.0 }, 1u),
        });

        var synth = new FormantSynth(Sr);
        synth.QueueSchedule(schedule, startCounter: 200);

        var buf = new float[1];
        synth.Process(buf, 0, 1);
        // The event at 100 is behind the clock; the one at 500 is not.
        Assert.Equal(220.0, synth.CurrentSnapshot().F0);
    }

    [Fact]
    public void SteadyVoicedRenderHoldsFormantStateAtItsDefaults()
    {
        // VowelTarget ramps only F0, voicing, and the amplitudes, so the
        // formant frequencies and bandwidths stay at their defaults. The
        // biquads recompute from them every sample, and drift retunes the
        // filters mid-note.
        var synth = new FormantSynth(Sr);
        synth.SetTarget(VowelTarget(), 1);

        var warmup = new float[256];
        synth.Process(warmup, 0, warmup.Length);
        AssertFormantsMatch(Params.Default, synth.CurrentSnapshot());

        var more = new float[4800];
        synth.Process(more, 0, more.Length);
        AssertFormantsMatch(Params.Default, synth.CurrentSnapshot());
    }

    private static void AssertFormantsMatch(Params expected, Params actual)
    {
        Assert.Equal(expected.F1, actual.F1);
        Assert.Equal(expected.BW1, actual.BW1);
        Assert.Equal(expected.F2, actual.F2);
        Assert.Equal(expected.BW2, actual.BW2);
        Assert.Equal(expected.F3, actual.F3);
        Assert.Equal(expected.BW3, actual.BW3);
    }
}
