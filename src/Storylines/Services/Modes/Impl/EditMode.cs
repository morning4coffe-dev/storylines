namespace Storylines.Services.Modes.Impl
{
    public sealed class EditMode : IEditorMode
    {
        public static EditMode Instance { get; } = new EditMode();

        private EditMode() { }

        public string Id => "edit";
        public string DisplayNameKey => "modeEdit";
        public string DescriptionKey => "modeEditDescription";
        public string IconGlyph => "";

        public ModeChromeConfig Chrome { get; } = new ModeChromeConfig();

        public bool CanLeave => true;

        public void Enter() { }
        public void Leave() { }
        public void OnTextChanged() { }
    }
}
