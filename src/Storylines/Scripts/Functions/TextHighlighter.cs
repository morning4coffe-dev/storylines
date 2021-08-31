using Windows.UI;

namespace Storylines.DialogueWindows
{
    class TextHighlighter
    {
        public enum Tool { None, White, Yellow, Red, Green, Blue }
        public static Tool selectedTool;

        public static Tool lastTool = Tool.Yellow;

        public static Color ChangeColor(Tool tool)
        {
            selectedTool = tool;
            switch (tool)
            {
                case Tool.White:
                    return Color.FromArgb(255, 255, 255, 255);
                case Tool.Yellow:
                    return Color.FromArgb(255, 229, 193, 38);
                case Tool.Red:
                    return Color.FromArgb(255, 214, 21, 21);
                case Tool.Green:
                    return Color.FromArgb(255, 71, 205, 61);
                case Tool.Blue:
                    return Color.FromArgb(255, 26, 65, 246);
                default:
                    return Color.FromArgb(0, 0, 0, 0);
            }
        }
    }
}
