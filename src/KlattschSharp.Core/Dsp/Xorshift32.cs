namespace KlattschSharp.Dsp;

/// <summary>
/// 32-bit xorshift LFSR.
/// </summary>
internal sealed class Xorshift32
{
    /// <summary>Reference implementation's default seed.</summary>
    public const uint DefaultSeed = 0xACE1ACE1u;

    private int _state;

    /// <summary>Initializes a new instance with <see cref="DefaultSeed"/>.</summary>
    public Xorshift32() : this(DefaultSeed)
    {
    }

    /// <summary>Seeds the LFSR state with <paramref name="seed"/> reinterpreted as a signed 32-bit word.</summary>
    public Xorshift32(uint seed)
    {
        _state = unchecked((int)seed);
    }

    /// <summary>Advance the LFSR and return a sample in [-1, 1).</summary>
    public double NextSample()
    {
        int x = _state;
        x ^= x << 13;
        x = (int)((uint)x >> 17) ^ x;
        x ^= x << 5;
        _state = x;
        return x / 2147483648.0;
    }

    /// <summary>Reset the LFSR to the given seed.</summary>
    public void Reset(uint seed)
    {
        _state = unchecked((int)seed);
    }
}
