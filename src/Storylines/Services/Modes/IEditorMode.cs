namespace Storylines.Services.Modes;

public interface IEditorMode
{
    string Id { get; }
    string DisplayNameKey { get; }
    string DescriptionKey { get; }
    string IconGlyph { get; }

    ModeChromeConfig Chrome { get; }

    bool CanLeave { get; }

    void Enter();
    void Leave();
    void OnTextChanged();
}
