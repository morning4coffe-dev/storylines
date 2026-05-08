using Windows.UI;

namespace Storylines.Helpers
{
    public class TextHighlighter
    {
        public enum Tool { None, White, Yellow, Red, Green, Blue }
        public Tool SelectedTool;
        public Tool LastTool = Tool.Yellow;

        private static readonly Color HighlightWhite = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color HighlightYellow = Color.FromArgb(255, 229, 193, 38);
        private static readonly Color HighlightRed = Color.FromArgb(255, 214, 21, 21);
        private static readonly Color HighlightGreen = Color.FromArgb(255, 71, 205, 61);
        private static readonly Color HighlightBlue = Color.FromArgb(255, 26, 65, 246);
        private static readonly Color HighlightTransparent = Color.FromArgb(0, 0, 0, 0);

        public Color ChangeColor(Tool tool)
        {
            SelectedTool = tool;
            switch (tool)
            {
                case Tool.White:
                    return HighlightWhite;
                case Tool.Yellow:
                    return HighlightYellow;
                case Tool.Red:
                    return HighlightRed;
                case Tool.Green:
                    return HighlightGreen;
                case Tool.Blue:
                    return HighlightBlue;
                default:
                    return HighlightTransparent;
            }
        }
    }
}
