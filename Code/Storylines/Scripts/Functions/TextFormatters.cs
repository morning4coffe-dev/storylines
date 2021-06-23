using Windows.UI;
using Windows.UI.Text;

namespace Storylines.DialogueWindows
{
    class TextFormatters
    {
        public static void BoldChapterTextBox(bool isBold)
        {
            if (MainPage.chapterList.chaptersListView.SelectedItem != null && MainPage.chapterText.textBox.Document.Selection != null)
            {
                MainPage.chapterText.textBox.Document.Selection.CharacterFormat.Bold = isBold ? FormatEffect.Off : FormatEffect.On;
            }
        }

        public static void ItalicChapterTextBox(bool isItalic)
        {
            if (MainPage.chapterList.chaptersListView.SelectedItem != null && MainPage.chapterText.textBox.Document.Selection != null)
            {
                MainPage.chapterText.textBox.Document.Selection.CharacterFormat.Italic = isItalic ? FormatEffect.Off : FormatEffect.On;
            }
        }

        public static void UnderlineChapterTextBox(bool isUnderlined)
        {
            if (MainPage.chapterList.chaptersListView.SelectedItem != null && MainPage.chapterText.textBox.Document.Selection != null)
            {
                MainPage.chapterText.textBox.Document.Selection.CharacterFormat.Underline = isUnderlined ? UnderlineType.None : UnderlineType.Thin;
            }
        }

        public static void StrikethroughChapterTextBox(bool isStriked)
        {
            if (MainPage.chapterList.chaptersListView.SelectedItem != null && MainPage.chapterText.textBox.Document.Selection != null)
            {
                MainPage.chapterText.textBox.Document.Selection.CharacterFormat.Strikethrough = isStriked ? FormatEffect.Off : FormatEffect.On;
            }
        }

        public static void MarkTextBackground(bool isColored)
        {
            if (MainPage.chapterText.textBox.Document.Selection != null)
            {
                if (MainPage.chapterList.chaptersListView.SelectedItem != null && TextHighlighter.selectedTool != TextHighlighter.Tool.None)
                {
                    MainPage.chapterText.textBox.Document.Selection.CharacterFormat.BackgroundColor = TextHighlighter.color;
                }
            }
        }
    }

    class TextHighlighter
    {
        public enum Tool { None, White, Yellow, Red, Green, Blue }
        public static Tool selectedTool;

        public static Tool lastTool = Tool.Yellow;

        public static Color color = Colors.Transparent;

        public static void ChangeColor(Tool tool)
        {
            selectedTool = tool;
            switch (tool)
            {
                case Tool.None:
                    color = Color.FromArgb(0, 0, 0, 0);
                    break;
                case Tool.White:
                    color = Color.FromArgb(80, 255, 255, 255);
                    break;
                case Tool.Yellow:
                    color = Color.FromArgb(200, 229, 193, 38);
                    break;
                case Tool.Red:
                    color = Color.FromArgb(80, 214, 21, 21);
                    break;
                case Tool.Green:
                    color = Color.FromArgb(80, 71, 205, 61);
                    break;
                case Tool.Blue:
                    color = Color.FromArgb(80, 26, 65, 246);
                    break;
            }
        }
    }
}
