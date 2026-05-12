using Xunit;

namespace Storylines.Tests.Services;

public class TextEditorServiceTests
{
    [Fact]
    public void NormalizeLoadedChapterText_EmptyChapterWithWhitespaceDocument_ReturnsEmptyString()
    {
        var result = ChapterTextNormalization.NormalizeLoadedChapterText(
            string.Empty,
            "\r",
            @"{\rtf1\ansi\pard\par}");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void NormalizeLoadedChapterText_NonEmptyChapter_ReturnsNormalizedRtf()
    {
        const string normalizedRtf = @"{\rtf1\ansi\pard Hello\par}";

        var result = ChapterTextNormalization.NormalizeLoadedChapterText(
            @"{\rtf1\ansi Hello}",
            "Hello\r",
            normalizedRtf);

        Assert.Equal(normalizedRtf, result);
    }

    [Fact]
    public void NormalizeLoadedChapterText_WhitespaceDocumentWithExistingFormatting_KeepsNormalizedRtf()
    {
        const string normalizedRtf = @"{\rtf1\ansi\pard\b0\par}";

        var result = ChapterTextNormalization.NormalizeLoadedChapterText(
            @"{\rtf1\ansi\b\par}",
            "\r",
            normalizedRtf);

        Assert.Equal(normalizedRtf, result);
    }
}