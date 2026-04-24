using Storylines.Views.Controls.Modes;

namespace Storylines.Services.Modes.Impl
{
    public sealed class ReadOnlyMode : IEditorMode
    {
        public static ReadOnlyMode Instance { get; } = new ReadOnlyMode();

        private ReadOnlyMode() { }

        private ReadOnlyModeOverlay _overlay;

        public string Id => "readonly";
        public string DisplayNameKey => "modeReadOnly";
        public string DescriptionKey => "modeReadOnlyDescription";
        public string IconGlyph => "\uE8A5";

        // Keep the default command bar so users can still save/navigate; only
        // formatting bar is hidden and the editor is locked.
        public ModeChromeConfig Chrome => new ModeChromeConfig(
            showDefaultCommandBar: true,
            showChapterList: true,
            showChapterTextFormattingBar: false,
            showDownBarStats: true,
            showDownBarFocusText: false,
            isTextReadOnly: true,
            allowsEditingShortcuts: false,
            allowsSettingsShortcut: true,
            overlayContent: _overlay);

        public bool CanLeave => true;

        public void Enter()
        {
            _overlay ??= new ReadOnlyModeOverlay();
        }

        public void Leave() { }

        public void OnTextChanged() { }
    }
}
