using System.Globalization;
using System.Text;

namespace KlattschSharp.Text;

/// <summary>
/// Input normalization for pasted text: NFKC, zero-width strip, and
/// Latin-lookalike fold.
/// </summary>
public static class Normalizer
{
    /// <summary>Apply NFKC, strip zero-width characters, and fold Greek/Cyrillic homoglyphs to Latin.</summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input ?? string.Empty;
        }

        string nfkc = input.Normalize(NormalizationForm.FormKC);
        var sb = new StringBuilder(nfkc.Length);
        foreach (char c in nfkc)
        {
            if (IsZeroWidth(c))
            {
                continue;
            }
            sb.Append(FoldHomoglyph(c));
        }
        return sb.ToString();
    }

    // Escaped rather than literal: these are invisible in an editor.
    private static bool IsZeroWidth(char c)
    {
        switch (c)
        {
            case '\u200B': // zero-width space
            case '\u200C': // zero-width non-joiner
            case '\u200D': // zero-width joiner
            case '\u2060': // word joiner
            case '\uFEFF': // zero-width no-break space, also the BOM
                return true;
            default:
                return false;
        }
    }

    private static char FoldHomoglyph(char c)
    {
        switch (c)
        {
            // Greek uppercase
            case 'Α': return 'A';
            case 'Β': return 'B';
            case 'Ε': return 'E';
            case 'Η': return 'H';
            case 'Ι': return 'I';
            case 'Κ': return 'K';
            case 'Μ': return 'M';
            case 'Ν': return 'N';
            case 'Ο': return 'O';
            case 'Ρ': return 'P';
            case 'Τ': return 'T';
            case 'Υ': return 'Y';
            case 'Ζ': return 'Z';

            // Cyrillic uppercase
            case 'А': return 'A';
            case 'В': return 'B';
            case 'С': return 'C';
            case 'Е': return 'E';
            case 'Н': return 'H';
            case 'К': return 'K';
            case 'М': return 'M';
            case 'О': return 'O';
            case 'Р': return 'P';
            case 'Т': return 'T';

            // Cyrillic lowercase
            case 'а': return 'a';
            case 'с': return 'c';
            case 'е': return 'e';
            case 'о': return 'o';
            case 'р': return 'p';

            default:
                return c;
        }
    }
}
