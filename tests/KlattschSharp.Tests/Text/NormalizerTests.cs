using KlattschSharp.Text;

namespace KlattschSharp.Tests.Text;

public sealed class NormalizerTests
{
    [Fact]
    public void AsciiPassesThrough()
    {
        Assert.Equal("HH AH L OW", Normalizer.Normalize("HH AH L OW"));
    }

    [Fact]
    public void StripsZeroWidthJoiner()
    {
        Assert.Equal("AHAH", Normalizer.Normalize("AH‍AH"));
    }

    [Fact]
    public void FoldsCyrillicLookalikes()
    {
        // Cyrillic А (U+0410) and С (U+0421) fold to Latin A and C.
        Assert.Equal("AC", Normalizer.Normalize("АС"));
    }

    [Fact]
    public void NfkcNormalizesCompatChars()
    {
        // Fullwidth A (U+FF21) NFKC-folds to plain A.
        Assert.Equal("AH", Normalizer.Normalize("ＡH"));
    }
}
