namespace Storylines.Services.Modes
{
    public sealed class ModeChromeConfig
    {
        public bool ShowDefaultCommandBar { get; }
        public bool ShowChapterList { get; }
        public bool ShowChapterTextFormattingBar { get; }
        public bool ShowDownBarStats { get; }
        public bool ShowDownBarFocusText { get; }
        public bool IsTextReadOnly { get; }
        public bool AllowsEditingShortcuts { get; }
        public bool AllowsSettingsShortcut { get; }
        public object OverlayContent { get; }

        public ModeChromeConfig(
            bool showDefaultCommandBar = true,
            bool showChapterList = true,
            bool showChapterTextFormattingBar = true,
            bool showDownBarStats = true,
            bool showDownBarFocusText = false,
            bool isTextReadOnly = false,
            bool allowsEditingShortcuts = true,
            bool allowsSettingsShortcut = true,
            object overlayContent = null)
        {
            ShowDefaultCommandBar = showDefaultCommandBar;
            ShowChapterList = showChapterList;
            ShowChapterTextFormattingBar = showChapterTextFormattingBar;
            ShowDownBarStats = showDownBarStats;
            ShowDownBarFocusText = showDownBarFocusText;
            IsTextReadOnly = isTextReadOnly;
            AllowsEditingShortcuts = allowsEditingShortcuts;
            AllowsSettingsShortcut = allowsSettingsShortcut;
            OverlayContent = overlayContent;
        }
    }
}
