using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KlattschSharp.Text;

/// <summary>
/// Lexes whitespace-separated phoneme-string source into <see cref="Token"/>s.
/// Supports <c>#</c> line comments (at a token boundary) and <c>/* */</c> blocks.
/// </summary>
public static class Tokenizer
{
    private const double PauseCommaMs = 100.0;
    private const double PauseSemiMs = 200.0;
    private const double PausePeriodMs = 300.0;

    /// <summary>Lex <paramref name="input"/> into a list of tokens.</summary>
    public static IReadOnlyList<Token> Tokenize(string input)
    {
        string source = Normalizer.Normalize(input);
        int len = source.Length;
        var tokens = new List<Token>();
        int i = 0;

        while (i < len)
        {
            char c = source[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            // `#` opens a line comment only at a token boundary.
            bool prevIsWs = i == 0 || char.IsWhiteSpace(source[i - 1]);
            if (c == '#' && prevIsWs)
            {
                while (i < len && source[i] != '\n')
                {
                    i++;
                }
                continue;
            }

            if (c == '/' && i + 1 < len && source[i + 1] == '*')
            {
                i = FindBlockEnd(source, i, len);
                continue;
            }

            var part = new StringBuilder();
            while (i < len && !char.IsWhiteSpace(source[i]))
            {
                // An embedded block comment terminates the token.
                if (source[i] == '/' && i + 1 < len && source[i + 1] == '*')
                {
                    i = FindBlockEnd(source, i, len);
                    continue;
                }
                part.Append(source[i]);
                i++;
            }

            if (part.Length == 0)
            {
                continue;
            }

            string partStr = part.ToString();

            // A stress mark attaches to the most recent phoneme token.
            if (partStr == "!" || partStr == "'")
            {
                for (int j = tokens.Count - 1; j >= 0; j--)
                {
                    if (tokens[j].Kind == TokenKind.Phoneme)
                    {
                        tokens[j] = Token.Phoneme(
                            tokens[j].Code,
                            stressed: true,
                            tokens[j].PitchDelta,
                            tokens[j].Transient);
                        break;
                    }
                }
                continue;
            }

            Token? tok = Classify(partStr);
            if (tok != null)
            {
                tokens.Add(tok);
            }
            // Bare `p` classifies to null and is silently dropped
        }

        return tokens;
    }

    // start points at the opening `/` of a `/*`.
    private static int FindBlockEnd(string source, int start, int len)
    {
        int off = source.IndexOf("*/", start + 2, StringComparison.Ordinal);
        if (off < 0)
        {
            return len; // unterminated -> consume to EOF
        }
        return off + 2;
    }

    private static Token? Classify(string part)
    {
        switch (part)
        {
            case "(":
                return Token.SyllableOpen();
            case ")":
                return Token.SyllableClose();
            case ",":
                return Token.MakePause(PauseCommaMs);
            case ";":
                return Token.MakePause(PauseSemiMs);
            case ".":
                return Token.MakePause(PausePeriodMs);
        }

        // Bracket forms: [bank=NAME], [bank], [engine=NAME], [engine], and the
        // generic [name=value]. Keywords match case-sensitively
        if (part.Length >= 2 && part[0] == '[')
        {
            if (part[part.Length - 1] == ']')
            {
                string inner = part.Substring(1, part.Length - 2);

                if (inner == "bank")
                {
                    return Token.BankReset();
                }
                if (inner == "engine")
                {
                    return Token.EngineReset();
                }

                int eq = inner.IndexOf('=');
                if (eq >= 0)
                {
                    string name = inner.Substring(0, eq);
                    string valueStr = inner.Substring(eq + 1);

                    if (IsBankOrEngineName(valueStr))
                    {
                        if (name == "bank")
                        {
                            return Token.BankSwitch(valueStr);
                        }
                        if (name == "engine")
                        {
                            return Token.EngineSwitch(valueStr);
                        }
                    }

                    if (IsWordName(name) && TryParseStrictNumber(valueStr, allowPlus: false, out double value))
                    {
                        if (name == "voice")
                        {
                            return Token.VoiceMarker();
                        }
                        if (TryDirectiveFromName(name, out DirectiveKey nkey))
                        {
                            return Token.Directive(nkey, value, relative: false, reset: false);
                        }
                        // Any other name is an extended directive
                        return Token.ExtendedDirective(name, value, reset: false);
                    }
                }
                else if (IsUppercaseWordName(inner))
                {
                    return Token.ExtendedDirective(inner, 0.0, reset: true);
                }
                return Token.MakeUnknown(part);
            }
        }

        // Note-form pitch directive: `bC4`, `b=Bb-1`
        if (part.Length >= 2 && part[0] == 'b')
        {
            int afterB = part[1] == '=' ? 2 : 1;
            if (part.Length > afterB)
            {
                string noteStr = part.Substring(afterB);
                double? hz = NoteToHz(noteStr);
                if (hz.HasValue)
                {
                    return Token.Directive(DirectiveKey.Base, hz.Value, relative: false, reset: false);
                }
            }
        }

        // Compact directive: a lowercase letter optionally followed by `=value`
        // or a signed/unsigned number. Bare letter = reset.
        if (part.Length > 0)
        {
            char first = part[0];
            if (first >= 'a' && first <= 'z')
            {
                if (TryDirectiveFromLetter(first, out DirectiveKey key))
                {
                    if (part.Length == 1)
                    {
                        // Bare letter: drop bare `p`, otherwise mark reset.
                        if (key == DirectiveKey.Pause)
                        {
                            return null;
                        }
                        return Token.Directive(key, 0.0, relative: false, reset: true);
                    }

                    int idx = 1;
                    bool explicitEq = part[idx] == '=';
                    if (explicitEq)
                    {
                        idx++;
                    }
                    string rest = part.Substring(idx);
                    if (TryParseStrictNumber(rest, allowPlus: true, out double value))
                    {
                        // rest parsed, so it is non-empty and part[idx] exists.
                        char leading = part[idx];
                        bool signed = leading == '+' || leading == '-';
                        bool relative = !explicitEq && signed;
                        return Token.Directive(key, value, relative, reset: false);
                    }
                }
            }
        }

        // Phoneme: ARPABET letters, optional stress mark, optional pitch delta
        // (transient `(+30)` or sticky `+30`).
        if (part.Length > 0 && part[0] >= 'A' && part[0] <= 'Z')
        {
            int idx = 0;
            while (idx < part.Length && part[idx] >= 'A' && part[idx] <= 'Z')
            {
                idx++;
            }
            string code = part.Substring(0, idx);

            bool stressed = false;
            if (idx < part.Length && (part[idx] == '\'' || part[idx] == '!'))
            {
                stressed = true;
                idx++;
            }

            double pitchDelta = 0.0;
            bool transient = false;
            if (idx < part.Length)
            {
                char c = part[idx];
                if (c == '(')
                {
                    int close = part.IndexOf(')', idx + 1);
                    if (close >= 0)
                    {
                        string inner = part.Substring(idx + 1, close - (idx + 1));
                        if (inner.Length > 0 && (inner[0] == '+' || inner[0] == '-'))
                        {
                            if (TryParseStrictNumber(inner, allowPlus: true, out double v))
                            {
                                pitchDelta = v;
                                transient = true;
                                idx = close + 1;
                            }
                        }
                    }
                }
                else if (c == '+' || c == '-')
                {
                    string rest = part.Substring(idx);
                    if (TryParseStrictNumber(rest, allowPlus: true, out double v))
                    {
                        pitchDelta = v;
                        transient = false;
                        idx = part.Length;
                    }
                }
            }

            if (idx == part.Length)
            {
                return Token.Phoneme(code, stressed, pitchDelta, transient);
            }
        }

        return Token.MakeUnknown(part);
    }

    private static bool TryDirectiveFromLetter(char c, out DirectiveKey key)
    {
        switch (c)
        {
            case 'b': key = DirectiveKey.Base; return true;
            case 'r': key = DirectiveKey.Rate; return true;
            case 'p': key = DirectiveKey.Pause; return true;
            case 's': key = DirectiveKey.Scale; return true;
            case 'v': key = DirectiveKey.Vibrato; return true;
            case 'w': key = DirectiveKey.VibratoRate; return true;
            case 'm': key = DirectiveKey.Tremolo; return true;
            case 'n': key = DirectiveKey.TremoloRate; return true;
            case 'h': key = DirectiveKey.Aspiration; return true;
            case 't': key = DirectiveKey.Tilt; return true;
            case 'g': key = DirectiveKey.Effort; return true;
            default: key = default; return false;
        }
    }

    private static bool TryDirectiveFromName(string name, out DirectiveKey key)
    {
        switch (name)
        {
            case "base":
            case "pitch": key = DirectiveKey.Base; return true;
            case "rate": key = DirectiveKey.Rate; return true;
            case "pause": key = DirectiveKey.Pause; return true;
            case "scale": key = DirectiveKey.Scale; return true;
            case "vibrato": key = DirectiveKey.Vibrato; return true;
            case "vibratoRate": key = DirectiveKey.VibratoRate; return true;
            case "tremolo": key = DirectiveKey.Tremolo; return true;
            case "tremoloRate": key = DirectiveKey.TremoloRate; return true;
            case "aspiration": key = DirectiveKey.Aspiration; return true;
            case "tilt": key = DirectiveKey.Tilt; return true;
            case "effort": key = DirectiveKey.Effort; return true;
            default: key = default; return false;
        }
    }

    // Bracket directive names are word characters only
    private static bool IsWordName(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }
        foreach (char c in s)
        {
            bool ok = (c >= 'A' && c <= 'Z')
                      || (c >= 'a' && c <= 'z')
                      || (c >= '0' && c <= '9')
                      || c == '_';
            if (!ok)
            {
                return false;
            }
        }
        return true;
    }

    // The bare reset form takes an uppercase-initial word name
    private static bool IsUppercaseWordName(string s)
    {
        return s.Length > 0 && s[0] >= 'A' && s[0] <= 'Z' && IsWordName(s);
    }

    // Bank and engine names accept letters, digits, underscore, dot, and hyphen
    private static bool IsBankOrEngineName(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }
        foreach (char c in s)
        {
            bool ok = (c >= 'A' && c <= 'Z')
                      || (c >= 'a' && c <= 'z')
                      || (c >= '0' && c <= '9')
                      || c == '_' || c == '.' || c == '-';
            if (!ok)
            {
                return false;
            }
        }
        return true;
    }

    // Strict number grammar: an optional sign, digits, optionally a decimal
    // point and more digits. Rejects exponents, bare ".5", and NaN/Infinity.
    // Bracket values additionally reject a leading "+"
    private static bool TryParseStrictNumber(string s, bool allowPlus, out double value)
    {
        value = 0;
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }

        int i = 0;
        if (s[i] == '-' || (allowPlus && s[i] == '+'))
        {
            i++;
        }
        int intStart = i;
        while (i < s.Length && s[i] >= '0' && s[i] <= '9')
        {
            i++;
        }
        if (i == intStart)
        {
            return false;
        }
        if (i < s.Length && s[i] == '.')
        {
            i++;
            int fracStart = i;
            while (i < s.Length && s[i] >= '0' && s[i] <= '9')
            {
                i++;
            }
            if (i == fracStart)
            {
                return false;
            }
        }
        if (i != s.Length)
        {
            return false;
        }

        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Convert a note name (e.g. <c>"C4"</c>, <c>"Bb-1"</c>) to Hz:
    /// <c>440 * 2^((midi-69)/12)</c>.
    /// </summary>
    public static double? NoteToHz(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        int letter;
        switch (name[0])
        {
            case 'C': letter = 0; break;
            case 'D': letter = 2; break;
            case 'E': letter = 4; break;
            case 'F': letter = 5; break;
            case 'G': letter = 7; break;
            case 'A': letter = 9; break;
            case 'B': letter = 11; break;
            default: return null;
        }

        int idx = 1;
        int accidental = 0;
        if (idx < name.Length)
        {
            if (name[idx] == '#')
            {
                accidental = 1;
                idx++;
            }
            else if (name[idx] == 'b')
            {
                accidental = -1;
                idx++;
            }
        }

        if (idx >= name.Length)
        {
            return null;
        }

        // Octave grammar is -?\d+
        string octaveStr = name.Substring(idx);
        int oi = 0;
        if (oi < octaveStr.Length && octaveStr[oi] == '-')
        {
            oi++;
        }
        if (oi >= octaveStr.Length)
        {
            return null;
        }
        for (int k = oi; k < octaveStr.Length; k++)
        {
            if (octaveStr[k] < '0' || octaveStr[k] > '9')
            {
                return null;
            }
        }
        if (!int.TryParse(octaveStr, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int octave))
        {
            return null;
        }

        int semi = letter + accidental;
        int midi = (octave + 1) * 12 + semi;
        return 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
    }
}
