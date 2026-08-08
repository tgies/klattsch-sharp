using System;

using KlattschSharp.Dsp;

namespace KlattschSharp;

/// <summary>
/// Klatt-style parallel-formant speech synthesizer. The render path allocates
/// nothing and takes no locks, but is not thread-safe: <see cref="SetTarget"/>,
/// <see cref="QueueSchedule"/>, and <see cref="Reset"/> must not race
/// <see cref="Process(float[], int, int)"/>.
/// </summary>
public sealed class FormantSynth
{
    private const double Tau = 2.0 * Math.PI;

    private readonly double _sr;
    private readonly uint _noiseSeed;

    private Params _current;
    private Params _target;
    private Params _increment;
    private int _transitionSamples;

    private double _glottalPhase;
    private double _vibratoPhase;
    private double _tremoloPhase;
    private double _tiltPrev;

    private readonly BandpassBiquad _bp1 = new();
    private readonly BandpassBiquad _bp2 = new();
    private readonly BandpassBiquad _bp3 = new();

    private readonly Xorshift32 _noise;

    private ScheduleEvent[] _schedule = Array.Empty<ScheduleEvent>();
    private int _scheduleIdx;
    private long _sampleClock;

    /// <summary>Construct at <paramref name="sampleRate"/> with the default noise seed.</summary>
    public FormantSynth(int sampleRate) : this(sampleRate, Xorshift32.DefaultSeed)
    {
    }

    /// <summary>
    /// Construct with an explicit noise LFSR seed. Parallel voices need
    /// distinct seeds or their unvoiced sounds are identical
    /// </summary>
    public FormantSynth(int sampleRate, uint noiseSeed)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "sampleRate must be positive");
        }

        _sr = sampleRate;
        _noiseSeed = noiseSeed;
        _current = Params.Default;
        _target = Params.Default;
        _increment = default;
        _noise = new Xorshift32(noiseSeed);
    }

    /// <summary>Sample rate the synth was constructed at.</summary>
    public int SampleRate => (int)_sr;

    /// <summary>Snapshot of the current (post-ramp) parameter state.</summary>
    public Params CurrentSnapshot() => _current;

    /// <summary>
    /// Stage a sparse parameter update with a <paramref name="transitionSamples"/>-long
    /// linear ramp from current values. A value below 1 is treated as 1.
    /// </summary>
    public void SetTarget(in ParamUpdate update, int transitionSamples)
    {
        _transitionSamples = transitionSamples < 1 ? 1 : transitionSamples;
        update.ApplyTo(ref _target);
        _increment = Params.ComputeIncrement(_target, _current, _transitionSamples);
    }

    /// <summary>
    /// Reset all internal state. <paramref name="initial"/> becomes current and
    /// target, the noise LFSR returns to its constructed seed, the schedule
    /// clears, and the sample clock rewinds to 0.
    /// </summary>
    public void Reset(in Params initial)
    {
        _current = initial;
        _target = initial;
        _increment = default;
        _transitionSamples = 0;
        _glottalPhase = 0.0;
        _vibratoPhase = 0.0;
        _tremoloPhase = 0.0;
        _tiltPrev = 0.0;
        _bp1.Reset();
        _bp2.Reset();
        _bp3.Reset();
        _noise.Reset(_noiseSeed);
        _schedule = Array.Empty<ScheduleEvent>();
        _scheduleIdx = 0;
        _sampleClock = 0;
    }

    /// <summary>
    /// Replace any pending schedule and preset the sample clock to
    /// <paramref name="startCounter"/>. A negative value delays the first event
    /// by that many samples, positive drains events already behind it.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// If <paramref name="schedule"/>'s sample rate does not match this synth's.
    /// </exception>
    public void QueueSchedule(Schedule schedule, long startCounter = 0)
    {
        if (schedule is null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        if (schedule.SampleRate != (uint)_sr)
        {
            throw new ArgumentException(
                $"Schedule sample rate {schedule.SampleRate} must match FormantSynth sample rate {(uint)_sr}.",
                nameof(schedule));
        }

        _schedule = schedule.EventsArray;
        _scheduleIdx = 0;
        _sampleClock = startCounter;
    }

    /// <summary>Render <paramref name="count"/> samples of mono PCM into <paramref name="output"/>.</summary>
    public void Process(float[] output, int offset, int count)
    {
        if (output is null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset must be non-negative");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "count must be non-negative");
        }

        if (offset + count > output.Length)
        {
            throw new ArgumentException("offset + count exceeds output length.", nameof(count));
        }

        int end = offset + count;
        for (int i = offset; i < end; i++)
        {
            output[i] = (float)RenderSample();
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NET
    /// <summary>Render <c>output.Length</c> samples of mono PCM into <paramref name="output"/>.</summary>
    public void Process(Span<float> output)
    {
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = (float)RenderSample();
        }
    }
#endif

    // Advance all state by one sample
    private double RenderSample()
    {
        double sr64 = _sr;

        while (_scheduleIdx < _schedule.Length
               && (long)_schedule[_scheduleIdx].AtSample <= _sampleClock)
        {
            ScheduleEvent evt = _schedule[_scheduleIdx++];
            _transitionSamples = evt.TransitionSamples < 1u ? 1 : (int)evt.TransitionSamples;
            evt.Target.ApplyTo(ref _target);
            _increment = Params.ComputeIncrement(_target, _current, _transitionSamples);
        }
        _sampleClock++;

        if (_transitionSamples > 0)
        {
            _current.StepBy(_increment);
            _transitionSamples--;
            if (_transitionSamples == 0)
            {
                _current = _target;
            }
        }

        _vibratoPhase += Tau * _current.VibratoRate / sr64;
        _vibratoPhase -= Tau * Math.Floor(_vibratoPhase / Tau);
        double effF0 = _current.F0 + _current.VibratoDepth * Math.Sin(_vibratoPhase);

        _tremoloPhase += Tau * _current.TremoloRate / sr64;
        _tremoloPhase -= Tau * Math.Floor(_tremoloPhase / Tau);
        double tremoloMod = 1.0 - _current.TremoloDepth * (0.5 + 0.5 * Math.Sin(_tremoloPhase));

        double v = _current.Voicing < 0.0 ? 0.0 : (_current.Voicing > 1.0 ? 1.0 : _current.Voicing);
        double noiseSample = _noise.NextSample();
        double pulseVal = Glottal.Pulse(_glottalPhase, _current.Effort);
        double voicedGain = 1.0 - _current.Aspiration * 0.85;
        double exc = v * pulseVal * voicedGain
                     + (1.0 - v) * noiseSample * 0.35
                     + _current.Aspiration * noiseSample * 0.5;
        _glottalPhase += effF0 / sr64;
        _glottalPhase -= Math.Floor(_glottalPhase);

        _bp1.SetFreq(_current.F1, _current.BW1, _sr);
        _bp2.SetFreq(_current.F2, _current.BW2, _sr);
        _bp3.SetFreq(_current.F3, _current.BW3, _sr);

        double y = (_bp1.Process(exc) * _current.A1
                    + _bp2.Process(exc) * _current.A2
                    + _bp3.Process(exc) * _current.A3)
                   * _current.Gain * tremoloMod;

        double tilted = y - _current.Tilt * _tiltPrev;
        _tiltPrev = y;
        return SoftClip.Apply(tilted);
    }
}
