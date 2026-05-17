namespace Storylines.Helpers;

internal static class ChapterTextNormalization
{
    public static string NormalizeLoadedChapterText(string sourceText, string plainText, string normalizedRtf)
    {
        if (string.IsNullOrEmpty(sourceText) && string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        return normalizedRtf ?? string.Empty;
    }
}