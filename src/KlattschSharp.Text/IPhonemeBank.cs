using System.Collections.Generic;

namespace KlattschSharp.Text;

/// <summary>
/// A source of phoneme parameters, mapping an uppercase phoneme code
/// (e.g. <c>"AH"</c>) to its <see cref="PhonemeParams"/>.
/// </summary>
public interface IPhonemeBank
{
    /// <summary>Resolve a phoneme code. Lookup is case-sensitive.</summary>
    /// <returns><c>true</c> if <paramref name="code"/> is known.</returns>
    bool TryGet(string code, out PhonemeParams p);

    /// <summary>
    /// All phoneme codes in bank order: the stable index space callers address
    /// phonemes by. An extending bank lists the parent's names first, in the
    /// parent's order, then its own.
    /// </summary>
    IReadOnlyList<string> Names { get; }

    /// <summary>The registry name of this bank (e.g. <c>"klatt1980-en"</c>).</summary>
    string Name { get; }

    /// <summary>BCP-47 language tag for this bank, or <c>null</c> if not specified.</summary>
    string? Language { get; }
}
