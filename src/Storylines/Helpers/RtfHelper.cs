using Microsoft.UI.Text;

namespace Storylines.Helpers;

internal static class RtfHelper
{
    public static string ConvertToPlainText(string rtf)
    {
        if (string.IsNullOrWhiteSpace(rtf))
            return string.Empty;

        var box = new RichEditBox();
        box.Document.SetText(TextSetOptions.FormatRtf, rtf);
        box.Document.GetText(TextGetOptions.None, out string plainText);
        return plainText ?? string.Empty;
    }

    public static int GetTotalWordCount(IEnumerable<Chapter> chapters)
    {
        string all = GetAllChaptersText(chapters);
        return all.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public static string GetAllChaptersText(IEnumerable<Chapter> chapters)
    {
        string all = string.Empty;
        foreach (var chapter in chapters)
            if (!string.IsNullOrEmpty(chapter.Text))
                all += ConvertToPlainText(chapter.Text);
        return all;
    }
}
