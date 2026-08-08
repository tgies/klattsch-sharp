namespace KlattschSharp;

/// <summary>A millisecond-keyed parameter event.</summary>
public readonly struct MsEvent
{
    /// <summary>Time in milliseconds at which the ramp begins.</summary>
    public double AtMs { get; }

    /// <summary>Sparse parameter overrides to ramp toward.</summary>
    public ParamUpdate Target { get; }

    /// <summary>Linear ramp length in milliseconds.</summary>
    public double TransitionMs { get; }

    /// <summary>Event applying <paramref name="target"/> at <paramref name="atMs"/> ms over a <paramref name="transitionMs"/>-ms ramp.</summary>
    public MsEvent(double atMs, ParamUpdate target, double transitionMs)
    {
        AtMs = atMs;
        Target = target;
        TransitionMs = transitionMs;
    }
}
