namespace KlattschSharp.Wav;

/// <summary>Optional metadata for a WAV's <c>LIST/INFO</c> chunk.</summary>
public sealed class WavMetadata
{
    /// <summary>Software identifier, written as the <c>ISFT</c> sub-chunk.</summary>
    public string? Software { get; set; }

    /// <summary>Free-form comment, written as the <c>ICMT</c> sub-chunk.</summary>
    public string? Comment { get; set; }
}
