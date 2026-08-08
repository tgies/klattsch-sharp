namespace KlattschSharp.Text;

/// <summary>The category of a lexed <see cref="Token"/>.</summary>
public enum TokenKind
{
    /// <summary>An ARPABET phoneme code, with optional stress and pitch delta.</summary>
    Phoneme,

    /// <summary>A voice-state directive (compact letter, bracket, or note form).</summary>
    Directive,

    /// <summary>A punctuation pause (<c>,</c> <c>;</c> <c>.</c>).</summary>
    Pause,

    /// <summary>Opening <c>(</c> of a syllable group.</summary>
    SyllableOpen,

    /// <summary>Closing <c>)</c> of a syllable group.</summary>
    SyllableClose,

    /// <summary>Unrecognized input, reported as a compile warning.</summary>
    Unknown,

    /// <summary><c>[bank=NAME]</c>: switch the active phoneme bank.</summary>
    BankSwitch,

    /// <summary><c>[bank]</c>: restore the bank the section started with.</summary>
    BankReset,

    /// <summary><c>[engine=NAME]</c>: name the engine this section targets.</summary>
    EngineSwitch,

    /// <summary><c>[engine]</c>: restore the engine the section started with.</summary>
    EngineReset,

    /// <summary>A bracket directive outside the engine's own parameters, e.g. <c>[OQ=0.65]</c> or <c>[OQ]</c>.</summary>
    ExtendedDirective,

    /// <summary><c>[voice=N]</c>: start a new voice section. Sections are positional; N is ignored.</summary>
    VoiceMarker,
}

/// <summary>Which synthesis parameter a directive controls.</summary>
public enum DirectiveKey
{
    /// <summary>Base pitch in Hz. Letter <c>b</c>.</summary>
    Base,

    /// <summary>Per-phoneme duration in ms. Letter <c>r</c>.</summary>
    Rate,

    /// <summary>Pause duration in ms (only valid when a value is supplied). Letter <c>p</c>.</summary>
    Pause,

    /// <summary>Formant frequency / bandwidth scale factor. Letter <c>s</c>.</summary>
    Scale,

    /// <summary>Vibrato depth in Hz. Letter <c>v</c>.</summary>
    Vibrato,

    /// <summary>Vibrato rate in Hz. Letter <c>w</c>.</summary>
    VibratoRate,

    /// <summary>Tremolo depth (0..1). Letter <c>m</c>.</summary>
    Tremolo,

    /// <summary>Tremolo rate in Hz. Letter <c>n</c>.</summary>
    TremoloRate,

    /// <summary>Aspiration / breathiness (0..1). Letter <c>h</c>.</summary>
    Aspiration,

    /// <summary>Spectral tilt (-0.95..0.95). Letter <c>t</c>.</summary>
    Tilt,

    /// <summary>Glottal effort (0..1, lax..tense). Letter <c>g</c>.</summary>
    Effort,
}

/// <summary>
/// A token lexed from phoneme-string source. Which fields are meaningful
/// depends on <see cref="Kind"/>.
/// </summary>
public sealed class Token
{
    private Token(TokenKind kind)
    {
        Kind = kind;
        Code = string.Empty;
        Text = string.Empty;
        Name = string.Empty;
    }

    /// <summary>The token's category.</summary>
    public TokenKind Kind { get; }

    // Phoneme
    /// <summary>Phoneme code (e.g. <c>"AH"</c>). Empty for non-phoneme tokens.</summary>
    public string Code { get; private set; }

    /// <summary>Phoneme stress mark (<c>!</c> or <c>'</c>).</summary>
    public bool Stressed { get; private set; }

    /// <summary>Phoneme pitch delta in Hz.</summary>
    public double PitchDelta { get; private set; }

    /// <summary><c>true</c> for the per-phoneme <c>(+30)</c> form, <c>false</c> for the sticky <c>+30</c>.</summary>
    public bool Transient { get; private set; }

    // Directive
    /// <summary>Directive target. Meaningful only for <see cref="TokenKind.Directive"/>.</summary>
    public DirectiveKey Key { get; private set; }

    /// <summary>Directive value. Meaningful only when <see cref="Reset"/> is <c>false</c>.</summary>
    public double Value { get; private set; }

    /// <summary><c>true</c> for the relative <c>g+0.1</c> form, <c>false</c> for <c>g=0.5</c> or <c>g0.5</c>.</summary>
    public bool Relative { get; private set; }

    /// <summary><c>true</c> for a bare directive letter (e.g. <c>g</c>): reset to initial.</summary>
    public bool Reset { get; private set; }

    // Pause
    /// <summary>Pause duration in ms.</summary>
    public double PauseMs { get; private set; }

    // Unknown
    /// <summary>Raw text of an unrecognized token.</summary>
    public string Text { get; private set; }

    // Bank / engine / extended directive
    /// <summary>
    /// Bank, engine, or extended-directive name. Empty outside
    /// <see cref="TokenKind.BankSwitch"/>, <see cref="TokenKind.EngineSwitch"/>,
    /// and <see cref="TokenKind.ExtendedDirective"/>.
    /// </summary>
    public string Name { get; private set; }

    internal static Token Phoneme(string code, bool stressed, double pitchDelta, bool transient)
    {
        return new Token(TokenKind.Phoneme)
        {
            Code = code,
            Stressed = stressed,
            PitchDelta = pitchDelta,
            Transient = transient,
        };
    }

    internal static Token Directive(DirectiveKey key, double value, bool relative, bool reset)
    {
        return new Token(TokenKind.Directive)
        {
            Key = key,
            Value = value,
            Relative = relative,
            Reset = reset,
        };
    }

    internal static Token MakePause(double ms)
    {
        return new Token(TokenKind.Pause)
        {
            PauseMs = ms,
        };
    }

    internal static Token SyllableOpen() => new Token(TokenKind.SyllableOpen);

    internal static Token SyllableClose() => new Token(TokenKind.SyllableClose);

    internal static Token BankSwitch(string name) =>
        new Token(TokenKind.BankSwitch) { Name = name };

    internal static Token BankReset() => new Token(TokenKind.BankReset);

    internal static Token EngineSwitch(string name) =>
        new Token(TokenKind.EngineSwitch) { Name = name };

    internal static Token EngineReset() => new Token(TokenKind.EngineReset);

    internal static Token VoiceMarker() => new Token(TokenKind.VoiceMarker);

    internal static Token ExtendedDirective(string name, double value, bool reset)
    {
        return new Token(TokenKind.ExtendedDirective)
        {
            Name = name,
            Value = value,
            Reset = reset,
        };
    }

    internal static Token MakeUnknown(string text)
    {
        return new Token(TokenKind.Unknown)
        {
            Text = text,
        };
    }
}
