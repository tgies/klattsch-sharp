namespace KlattschSharp;

/// <summary>Sparse parameter override. Non-null fields are applied.</summary>
public readonly struct ParamUpdate
{
    /// <summary>Override for <see cref="Params.F0"/>.</summary>
    public double? F0 { get; init; }

    /// <summary>Override for <see cref="Params.Voicing"/>.</summary>
    public double? Voicing { get; init; }

    /// <summary>Override for <see cref="Params.F1"/>.</summary>
    public double? F1 { get; init; }

    /// <summary>Override for <see cref="Params.BW1"/>.</summary>
    public double? BW1 { get; init; }

    /// <summary>Override for <see cref="Params.A1"/>.</summary>
    public double? A1 { get; init; }

    /// <summary>Override for <see cref="Params.F2"/>.</summary>
    public double? F2 { get; init; }

    /// <summary>Override for <see cref="Params.BW2"/>.</summary>
    public double? BW2 { get; init; }

    /// <summary>Override for <see cref="Params.A2"/>.</summary>
    public double? A2 { get; init; }

    /// <summary>Override for <see cref="Params.F3"/>.</summary>
    public double? F3 { get; init; }

    /// <summary>Override for <see cref="Params.BW3"/>.</summary>
    public double? BW3 { get; init; }

    /// <summary>Override for <see cref="Params.A3"/>.</summary>
    public double? A3 { get; init; }

    /// <summary>Override for <see cref="Params.Gain"/>.</summary>
    public double? Gain { get; init; }

    /// <summary>Override for <see cref="Params.VibratoDepth"/>.</summary>
    public double? VibratoDepth { get; init; }

    /// <summary>Override for <see cref="Params.VibratoRate"/>.</summary>
    public double? VibratoRate { get; init; }

    /// <summary>Override for <see cref="Params.TremoloDepth"/>.</summary>
    public double? TremoloDepth { get; init; }

    /// <summary>Override for <see cref="Params.TremoloRate"/>.</summary>
    public double? TremoloRate { get; init; }

    /// <summary>Override for <see cref="Params.Aspiration"/>.</summary>
    public double? Aspiration { get; init; }

    /// <summary>Override for <see cref="Params.Tilt"/>.</summary>
    public double? Tilt { get; init; }

    /// <summary>Override for <see cref="Params.Effort"/>.</summary>
    public double? Effort { get; init; }

    /// <summary>Assign every non-null field into <paramref name="p"/>.</summary>
    public void ApplyTo(ref Params p)
    {
        if (F0.HasValue) p.F0 = F0.Value;
        if (Voicing.HasValue) p.Voicing = Voicing.Value;
        if (F1.HasValue) p.F1 = F1.Value;
        if (BW1.HasValue) p.BW1 = BW1.Value;
        if (A1.HasValue) p.A1 = A1.Value;
        if (F2.HasValue) p.F2 = F2.Value;
        if (BW2.HasValue) p.BW2 = BW2.Value;
        if (A2.HasValue) p.A2 = A2.Value;
        if (F3.HasValue) p.F3 = F3.Value;
        if (BW3.HasValue) p.BW3 = BW3.Value;
        if (A3.HasValue) p.A3 = A3.Value;
        if (Gain.HasValue) p.Gain = Gain.Value;
        if (VibratoDepth.HasValue) p.VibratoDepth = VibratoDepth.Value;
        if (VibratoRate.HasValue) p.VibratoRate = VibratoRate.Value;
        if (TremoloDepth.HasValue) p.TremoloDepth = TremoloDepth.Value;
        if (TremoloRate.HasValue) p.TremoloRate = TremoloRate.Value;
        if (Aspiration.HasValue) p.Aspiration = Aspiration.Value;
        if (Tilt.HasValue) p.Tilt = Tilt.Value;
        if (Effort.HasValue) p.Effort = Effort.Value;
    }
}
