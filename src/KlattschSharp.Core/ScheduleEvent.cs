namespace KlattschSharp;

/// <summary>A sample-quantized parameter event.</summary>
public readonly struct ScheduleEvent
{
    /// <summary>Absolute sample index at which the ramp begins.</summary>
    public ulong AtSample { get; }

    /// <summary>Sparse parameter overrides to ramp toward.</summary>
    public ParamUpdate Target { get; }

    /// <summary>Linear ramp length in samples (clamped to at least 1 by the synth).</summary>
    public uint TransitionSamples { get; }

    /// <summary>Event applying <paramref name="target"/> at <paramref name="atSample"/> over a <paramref name="transitionSamples"/>-long ramp.</summary>
    public ScheduleEvent(ulong atSample, ParamUpdate target, uint transitionSamples)
    {
        AtSample = atSample;
        Target = target;
        TransitionSamples = transitionSamples;
    }
}
