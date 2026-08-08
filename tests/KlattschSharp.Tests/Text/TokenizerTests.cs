using System.Collections.Generic;
using System.Linq;
using KlattschSharp.Text;

namespace KlattschSharp.Tests.Text;

public sealed class TokenizerTests
{
    [Fact]
    public void SimplePhonemes()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("HH AH L OW");
        Assert.Equal(4, tokens.Count);
        Assert.All(tokens, t => Assert.Equal(TokenKind.Phoneme, t.Kind));
        Assert.Equal(new[] { "HH", "AH", "L", "OW" }, tokens.Select(t => t.Code).ToArray());
    }

    [Fact]
    public void PausePunctuation()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("AH , AH . AH");
        double[] pauses = tokens
            .Where(t => t.Kind == TokenKind.Pause)
            .Select(t => t.PauseMs)
            .ToArray();
        Assert.Equal(new[] { 100.0, 300.0 }, pauses);
    }

    [Fact]
    public void Directives()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("b120 r60 g0.7");
        Assert.Equal(3, tokens.Count);
        Token b = tokens[0];
        Assert.Equal(TokenKind.Directive, b.Kind);
        Assert.Equal(DirectiveKey.Base, b.Key);
        Assert.Equal(120.0, b.Value);
        Assert.False(b.Relative);
        Assert.False(b.Reset);
    }

    [Fact]
    public void RelativeDirective()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("g+0.2");
        Token t = tokens[0];
        Assert.Equal(0.2, t.Value, 6);
        Assert.True(t.Relative);
    }

    [Fact]
    public void ExplicitEqualsDirectiveIsAbsolute()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("g=0.7");
        Token t = tokens[0];
        Assert.Equal(0.7, t.Value);
        Assert.False(t.Relative);
    }

    [Fact]
    public void BareLetterIsReset()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("b");
        Assert.True(tokens[0].Reset);
    }

    [Fact]
    public void BarePIsDropped()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("AH p AH");
        Assert.Equal(2, tokens.Count);
        Assert.All(tokens, t => Assert.Equal(TokenKind.Phoneme, t.Kind));
    }

    [Fact]
    public void NoteDirective()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("bC4");
        Token t = tokens[0];
        Assert.Equal(DirectiveKey.Base, t.Key);
        Assert.Equal(261.62556, t.Value, 2);
    }

    [Fact]
    public void PhonemeStickyDelta()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("AY+30");
        Token t = tokens[0];
        Assert.Equal("AY", t.Code);
        Assert.Equal(30.0, t.PitchDelta);
        Assert.False(t.Transient);
    }

    [Fact]
    public void PhonemeTransientDelta()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("AY(+30)");
        Token t = tokens[0];
        Assert.Equal(30.0, t.PitchDelta);
        Assert.True(t.Transient);
    }

    [Fact]
    public void StressMarkAttachesToPrevious()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("AH ! AH");
        bool[] stressed = tokens
            .Where(t => t.Kind == TokenKind.Phoneme)
            .Select(t => t.Stressed)
            .ToArray();
        Assert.Equal(new[] { true, false }, stressed);
    }

    [Fact]
    public void SkipsLineComment()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("# this is a comment\nHH AH");
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void SkipsBlockComment()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("HH /* note */ AH");
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void SyllableGrouping()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("( HH AH )");
        Assert.Equal(TokenKind.SyllableOpen, tokens[0].Kind);
        Assert.Equal(TokenKind.SyllableClose, tokens[3].Kind);
    }

    [Fact]
    public void UnknownEmitsUnknownToken()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("xyz123");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Unknown, tokens[0].Kind);
    }

    [Fact]
    public void BankSwitchCarriesName()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("[bank=ja-mokhtari-2000]");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.BankSwitch, tokens[0].Kind);
        Assert.Equal("ja-mokhtari-2000", tokens[0].Name);
    }

    [Fact]
    public void EngineSwitchCarriesName()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("[engine=klattschjr]");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.EngineSwitch, tokens[0].Kind);
        Assert.Equal("klattschjr", tokens[0].Name);
    }

    [Theory]
    [InlineData("[bank]", TokenKind.BankReset)]
    [InlineData("[engine]", TokenKind.EngineReset)]
    public void BareBankAndEngineReset(string text, TokenKind expected)
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize(text);
        Assert.Single(tokens);
        Assert.Equal(expected, tokens[0].Kind);
    }

    // Keywords match case-sensitively; names accept only [A-Za-z0-9_.-].
    [Theory]
    [InlineData("[Bank=foo]")]
    [InlineData("[BANK=foo]")]
    [InlineData("[bank=]")]
    [InlineData("[bank=a+b]")]
    [InlineData("[Engine=foo]")]
    public void RejectsMalformedBankAndEngineForms(string text)
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize(text);
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Unknown, tokens[0].Kind);
    }

    [Fact]
    public void ExtendedDirectiveCarriesNameAndValue()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("[OQ=0.65]");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.ExtendedDirective, tokens[0].Kind);
        Assert.Equal("OQ", tokens[0].Name);
        Assert.Equal(0.65, tokens[0].Value, 6);
        Assert.False(tokens[0].Reset);
    }

    [Fact]
    public void BareUppercaseBracketIsAnExtendedReset()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("[FNZ]");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.ExtendedDirective, tokens[0].Kind);
        Assert.Equal("FNZ", tokens[0].Name);
        Assert.True(tokens[0].Reset);
    }

    // A lowercase name lexes as an extended directive and is rejected by the
    // compiler. A bare lowercase bracket has no reset form at all.
    [Fact]
    public void LowercaseBracketNameLexesAsExtendedDirective()
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize("[oq=1]");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.ExtendedDirective, tokens[0].Kind);
        Assert.Equal("oq", tokens[0].Name);
    }

    [Theory]
    [InlineData("[oq]")]
    [InlineData("[OQ-2=1]")]
    [InlineData("[1OQ]")]
    public void RejectsMalformedExtendedForms(string text)
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize(text);
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Unknown, tokens[0].Kind);
    }

    [Fact]
    public void NoteToHzA4()
    {
        Assert.Equal(440.0, Tokenizer.NoteToHz("A4")!.Value, 3);
    }

    [Fact]
    public void NoteToHzMiddleC()
    {
        Assert.Equal(261.62556, Tokenizer.NoteToHz("C4")!.Value, 2);
    }

    [Fact]
    public void NoteToHzAccidentals()
    {
        Assert.True(Tokenizer.NoteToHz("C#4")!.Value > Tokenizer.NoteToHz("C4")!.Value);
        Assert.True(Tokenizer.NoteToHz("Bb4")!.Value < Tokenizer.NoteToHz("B4")!.Value);
    }

    // The number grammar rejects exponents, bare or trailing decimal points,
    // thousands separators, NaN/Infinity, and a leading "+" in brackets
    [Theory]
    [InlineData("[rate=+5]")]
    [InlineData("[rate=1e2]")]
    [InlineData("[rate=.5]")]
    [InlineData("[rate=5.]")]
    [InlineData("[rate=NaN]")]
    [InlineData("[rate=Infinity]")]
    [InlineData("[rate=1,000]")]
    [InlineData("g1e2")]
    [InlineData("gNaN")]
    [InlineData("AY+1e2")]
    [InlineData("bC+4")]
    public void RejectsNonJsNumberForms(string text)
    {
        IReadOnlyList<Token> tokens = Tokenizer.Tokenize(text);
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Unknown, tokens[0].Kind);
    }
}
