namespace KlattschSharp.Text;

/// <summary>
/// One <c>[voice=N]</c> section. Sections are simultaneous: each starts at time
/// zero and renders into its own buffer, which the caller sums.
/// </summary>
public sealed class VoiceResult
{
    /// <summary>Section with its own schedule, length, and engine name.</summary>
    public VoiceResult(Schedule schedule, double totalMs, string? engine)
    {
        Schedule = schedule;
        TotalMs = totalMs;
        Engine = engine;
    }

    /// <summary>This section's schedule, starting at time zero.</summary>
    public Schedule Schedule { get; }

    /// <summary>This section's length in ms, including the trailing fade-out.</summary>
    public double TotalMs { get; }

    /// <summary>Engine named by this section, or the compile option's engine.</summary>
    public string? Engine { get; }
}
