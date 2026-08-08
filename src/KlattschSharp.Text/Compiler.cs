using System;
using System.Collections.Generic;

namespace KlattschSharp.Text;

/// <summary>
/// Compiles a token stream or source string into a <see cref="CompileResult"/>.
/// Malformed input warns rather than throwing.
/// </summary>
public static class Compiler
{
    private const double StressDurationFactor = 1.5;
    private const double StressF0Lift = 8.0;
    private const double StopBurstMs = 25.0;
    private const double SentenceFinalHoldMs = 0.0;
    private const double FadeOutMs = 100.0;
    private const double TrailOffMs = 150.0;
    private const double SilenceTransitionMs = 30.0;

    /// <summary>Tokenize <paramref name="source"/>, then compile.</summary>
    public static CompileResult Compile(string source, CompileOptions? opts)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        return Compile(Tokenizer.Tokenize(source), opts);
    }

    /// <summary>Compile a token stream into a schedule plus warnings.</summary>
    public static CompileResult Compile(IEnumerable<Token> tokens, CompileOptions? opts)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }
        opts ??= new CompileOptions();

        // Partition at each [voice=N] marker. Sections are positional: the
        // marker's number is ignored, anything before the first marker is voice
        // 0, and every section compiles from the same options.
        var sections = new List<List<Token>> { new List<Token>() };
        foreach (Token tok in tokens)
        {
            if (tok.Kind == TokenKind.VoiceMarker)
            {
                sections.Add(new List<Token>());
                continue;
            }
            sections[sections.Count - 1].Add(tok);
        }

        List<string> warnings = new List<string>();
        var voices = new List<VoiceResult>(sections.Count);
        double totalMs = 0.0;
        foreach (var section in sections)
        {
            VoiceResult voice = CompileSection(section, opts, warnings);
            voices.Add(voice);
            if (voice.TotalMs > totalMs)
            {
                totalMs = voice.TotalMs;
            }
        }

        return new CompileResult(voices[0].Schedule, totalMs, warnings, voices[0].Engine, voices);
    }

    private static VoiceResult CompileSection(List<Token> tokens, CompileOptions opts, List<string> warnings)
    {
        State state = new State(opts);
        List<MsEvent> events = new List<MsEvent>();

        bool inSyllable = false;
        List<Token> syllableQueue = new List<Token>();

        foreach (Token tok in tokens)
        {
            switch (tok.Kind)
            {
                case TokenKind.Unknown:
                    warnings.Add($"unknown token: {tok.Text}");
                    break;

                case TokenKind.SyllableOpen:
                    if (inSyllable)
                    {
                        warnings.Add("nested ( ignored");
                        break;
                    }
                    inSyllable = true;
                    syllableQueue.Clear();
                    break;

                case TokenKind.SyllableClose:
                    if (!inSyllable)
                    {
                        warnings.Add("unmatched )");
                        break;
                    }
                    FlushSyllable(state, events, warnings, syllableQueue, opts);
                    inSyllable = false;
                    break;

                case TokenKind.Directive:
                    ApplyDirective(state, tok, events, opts);
                    break;

                case TokenKind.BankSwitch:
                    // An unregistered name warns; a broken extends chain throws.
                    if (opts.Registry.TryResolve(tok.Name, out IPhonemeBank? switched))
                    {
                        state.Bank = switched;
                    }
                    else
                    {
                        warnings.Add($"unknown bank: {tok.Name}");
                    }
                    break;

                case TokenKind.BankReset:
                    state.Bank = state.InitialBank;
                    break;

                case TokenKind.EngineSwitch:
                    state.Engine = tok.Name;
                    break;

                case TokenKind.EngineReset:
                    state.Engine = opts.Engine;
                    break;

                case TokenKind.ExtendedDirective:
                    if (IsExtendedName(tok.Name))
                    {
                        // A bare reset deletes the override; it does not
                        // restore a value seeded through CompileOptions.
                        if (tok.Reset)
                        {
                            state.Extras.Remove(tok.Name);
                        }
                        else
                        {
                            state.Extras[tok.Name] = tok.Value;
                        }
                    }
                    else
                    {
                        warnings.Add($"unknown directive: {tok.Name}");
                    }
                    break;

                case TokenKind.Pause:
                    PushSilence(events, state);
                    state.TimeMs += tok.PauseMs;
                    break;

                case TokenKind.Phoneme:
                    if (inSyllable)
                    {
                        syllableQueue.Add(tok);
                        break;
                    }
                    RenderOnePhoneme(tok, state, events, warnings, opts);
                    break;
            }
        }

        if (inSyllable)
        {
            warnings.Add("unclosed (");
            FlushSyllable(state, events, warnings, syllableQueue, opts);
        }

        state.TimeMs += SentenceFinalHoldMs;
        PushSilenceWithTransition(events, state, FadeOutMs);
        state.TimeMs += TrailOffMs;

        Schedule schedule = Schedule.FromMsEvents(opts.SampleRate, events);
        return new VoiceResult(schedule, state.TimeMs, state.Engine);
    }

    private static void RenderOnePhoneme(
        Token tok,
        State state,
        List<MsEvent> events,
        List<string> warnings,
        CompileOptions opts)
    {
        double phoneRate = tok.Stressed ? state.Rate * StressDurationFactor : state.Rate;
        RenderPhonemeInner(
            tok.Code,
            tok.Stressed,
            tok.PitchDelta,
            phoneRate,
            state,
            events,
            warnings,
            opts);
        if (!tok.Transient)
        {
            state.F0 += tok.PitchDelta;
        }
    }

    private static void FlushSyllable(
        State state,
        List<MsEvent> events,
        List<string> warnings,
        List<Token> queue,
        CompileOptions opts)
    {
        if (queue.Count == 0)
        {
            return;
        }
        double slot = state.Rate / queue.Count;
        foreach (Token tok in queue)
        {
            if (tok.Kind != TokenKind.Phoneme)
            {
                continue;
            }
            RenderPhonemeInner(
                tok.Code,
                tok.Stressed,
                tok.PitchDelta,
                slot,
                state,
                events,
                warnings,
                opts);
            if (!tok.Transient)
            {
                state.F0 += tok.PitchDelta;
            }
        }
        queue.Clear();
    }

    private static void RenderPhonemeInner(
        string code,
        bool stressed,
        double pitchDelta,
        double slotMs,
        State state,
        List<MsEvent> events,
        List<string> warnings,
        CompileOptions opts)
    {
        if (!state.Bank.TryGet(code, out PhonemeParams p))
        {
            warnings.Add($"unknown phoneme: {code}");
            return;
        }

        double startF0 = stressed ? state.F0 + StressF0Lift : state.F0;
        double endF0 = startF0 + pitchDelta;

        if (p.IsStop)
        {
            double burstMs = Math.Min(StopBurstMs, slotMs * 0.3);
            double silenceMs = slotMs - burstMs;
            PushSilenceWithTransition(events, state, Math.Min(20.0, silenceMs * 0.4));
            state.TimeMs += silenceMs;
            PushPhoneme(events, state, p, startF0, null, Math.Min(5.0, burstMs * 0.2));
            state.TimeMs += burstMs;
        }
        else if (p.GlideTo is { } g)
        {
            double onset = slotMs * 0.25;
            double glide = slotMs * 0.50;
            double offset = slotMs * 0.25;
            PushPhoneme(events, state, p, startF0, null, Math.Min(20.0, onset));
            state.TimeMs += onset;
            PushPhoneme(events, state, p, endF0, g, glide);
            state.TimeMs += glide + offset;
        }
        else if (pitchDelta != 0.0)
        {
            PushPhoneme(events, state, p, startF0, null, Math.Min(25.0, slotMs * 0.25));
            state.TimeMs += slotMs * 0.25;
            PushPhoneme(events, state, p, endF0, null, slotMs * 0.6);
            state.TimeMs += slotMs * 0.75;
        }
        else
        {
            double trans = Math.Min(opts.DefaultTransitionMs, slotMs * 0.4);
            PushPhoneme(events, state, p, startF0, null, trans);
            state.TimeMs += slotMs;
        }
    }

    private static void PushPhoneme(
        List<MsEvent> events,
        State state,
        PhonemeParams p,
        double f0,
        GlideTo? glideTo,
        double transitionMs)
    {
        double f1;
        double f2;
        double f3;
        if (glideTo is { } g)
        {
            f1 = g.F1;
            f2 = g.F2;
            f3 = g.F3;
        }
        else
        {
            f1 = p.F1;
            f2 = p.F2;
            f3 = p.F3;
        }

        ParamUpdate target = new ParamUpdate
        {
            F0 = f0,
            F1 = f1 * state.Scale,
            F2 = f2 * state.Scale,
            F3 = f3 * state.Scale,
            BW1 = p.BW1 * state.Scale,
            BW2 = p.BW2 * state.Scale,
            BW3 = p.BW3 * state.Scale,
            A1 = p.A1,
            A2 = p.A2,
            A3 = p.A3,
            Voicing = p.Voicing,
            VibratoDepth = state.VibratoDepth,
            VibratoRate = state.VibratoRate,
            TremoloDepth = state.TremoloDepth,
            TremoloRate = state.TremoloRate,
            Aspiration = state.Aspiration,
            Tilt = state.Tilt,
            Effort = state.Effort,
            Gain = null,
        };
        events.Add(new MsEvent(state.TimeMs, WithExtras(target, state.Extras), transitionMs));
    }

    private static void PushSilence(List<MsEvent> events, State state)
    {
        PushSilenceWithTransition(events, state, SilenceTransitionMs);
    }

    private static void PushSilenceWithTransition(List<MsEvent> events, State state, double transitionMs)
    {
        ParamUpdate target = new ParamUpdate
        {
            A1 = 0.0,
            A2 = 0.0,
            A3 = 0.0,
            VibratoDepth = state.VibratoDepth,
            VibratoRate = state.VibratoRate,
            TremoloDepth = state.TremoloDepth,
            TremoloRate = state.TremoloRate,
            Aspiration = state.Aspiration,
            Tilt = state.Tilt,
            Effort = state.Effort,
        };
        events.Add(new MsEvent(state.TimeMs, WithExtras(target, state.Extras), transitionMs));
    }

    private static void ApplyDirective(State state, Token tok, List<MsEvent> events, CompileOptions opts)
    {
        switch (tok.Key)
        {
            case DirectiveKey.Base:
                state.F0 = Resolve(state.F0, opts.BaseF0, tok);
                break;
            case DirectiveKey.Rate:
                state.Rate = Resolve(state.Rate, opts.Rate, tok);
                break;
            case DirectiveKey.Scale:
                state.Scale = Resolve(state.Scale, opts.Scale, tok);
                break;
            case DirectiveKey.Vibrato:
                state.VibratoDepth = Resolve(state.VibratoDepth, opts.VibratoDepth, tok);
                break;
            case DirectiveKey.VibratoRate:
                state.VibratoRate = Resolve(state.VibratoRate, opts.VibratoRate, tok);
                break;
            case DirectiveKey.Tremolo:
                state.TremoloDepth = Resolve(state.TremoloDepth, opts.TremoloDepth, tok);
                break;
            case DirectiveKey.TremoloRate:
                state.TremoloRate = Resolve(state.TremoloRate, opts.TremoloRate, tok);
                break;
            case DirectiveKey.Aspiration:
                state.Aspiration = Resolve(state.Aspiration, opts.Aspiration, tok);
                break;
            case DirectiveKey.Tilt:
                state.Tilt = Resolve(state.Tilt, opts.Tilt, tok);
                break;
            case DirectiveKey.Effort:
                state.Effort = Resolve(state.Effort, opts.Effort, tok);
                break;
            case DirectiveKey.Pause:
                PushSilence(events, state);
                state.TimeMs += Math.Abs(tok.Value);
                break;
        }
    }

    // Engine state carries an uppercase initial; anything else is a misspelled
    // parameter name.
    private static bool IsExtendedName(string name)
    {
        return name.Length > 0 && name[0] >= 'A' && name[0] <= 'Z';
    }

    // Layer the active extended directives over a target. A name matching one
    // of the engine's uppercase parameters overrides the computed value; any
    // other name is dropped here.
    private static ParamUpdate WithExtras(in ParamUpdate t, Dictionary<string, double> extras)
    {
        if (extras.Count == 0)
        {
            return t;
        }
        return new ParamUpdate
        {
            F0 = Override(extras, "F0", t.F0),
            Voicing = t.Voicing,
            F1 = Override(extras, "F1", t.F1),
            BW1 = Override(extras, "BW1", t.BW1),
            A1 = Override(extras, "A1", t.A1),
            F2 = Override(extras, "F2", t.F2),
            BW2 = Override(extras, "BW2", t.BW2),
            A2 = Override(extras, "A2", t.A2),
            F3 = Override(extras, "F3", t.F3),
            BW3 = Override(extras, "BW3", t.BW3),
            A3 = Override(extras, "A3", t.A3),
            Gain = t.Gain,
            VibratoDepth = t.VibratoDepth,
            VibratoRate = t.VibratoRate,
            TremoloDepth = t.TremoloDepth,
            TremoloRate = t.TremoloRate,
            Aspiration = t.Aspiration,
            Tilt = t.Tilt,
            Effort = t.Effort,
        };
    }

    private static double? Override(Dictionary<string, double> extras, string name, double? current)
    {
        return extras.TryGetValue(name, out double v) ? v : current;
    }

    private static double Resolve(double current, double initial, Token tok)
    {
        if (tok.Reset)
        {
            return initial;
        }
        if (tok.Relative)
        {
            return current + tok.Value;
        }
        return tok.Value;
    }

    private sealed class State
    {
        public State(CompileOptions opts)
        {
            InitialBank = opts.Bank;
            Bank = opts.Bank;
            Engine = opts.Engine;
            Extras = new Dictionary<string, double>(StringComparer.Ordinal);
            if (opts.Extras != null)
            {
                foreach (var kvp in opts.Extras)
                {
                    Extras[kvp.Key] = kvp.Value;
                }
            }
            TimeMs = 0.0;
            F0 = opts.BaseF0;
            Rate = opts.Rate;
            Scale = opts.Scale;
            VibratoDepth = opts.VibratoDepth;
            VibratoRate = opts.VibratoRate;
            TremoloDepth = opts.TremoloDepth;
            TremoloRate = opts.TremoloRate;
            Aspiration = opts.Aspiration;
            Tilt = opts.Tilt;
            Effort = opts.Effort;
        }

        // Bank the section started with; [bank] restores it.
        public IPhonemeBank InitialBank { get; }

        public IPhonemeBank Bank { get; set; }
        public string? Engine { get; set; }

        // Extended directives in force, applied to every later target.
        public Dictionary<string, double> Extras { get; }

        public double TimeMs { get; set; }
        public double F0 { get; set; }
        public double Rate { get; set; }
        public double Scale { get; set; }
        public double VibratoDepth { get; set; }
        public double VibratoRate { get; set; }
        public double TremoloDepth { get; set; }
        public double TremoloRate { get; set; }
        public double Aspiration { get; set; }
        public double Tilt { get; set; }
        public double Effort { get; set; }
    }
}
