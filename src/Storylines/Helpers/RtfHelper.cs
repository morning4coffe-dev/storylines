using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Storylines.Models;
using System;
using System.Collections.Generic;

namespace Storylines.Helpers
{
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
            string all = string.Empty;
            foreach (var chapter in chapters)
                if (!string.IsNullOrEmpty(chapter.Text))
                    all += ConvertToPlainText(chapter.Text);

            return all.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
