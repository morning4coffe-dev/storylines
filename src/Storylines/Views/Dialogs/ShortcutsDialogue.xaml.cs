
namespace Storylines.Views.Dialogs;

public sealed partial class ShortcutsDialogue : AppContentDialog
{
    public List<ShortcutDefinition> GlobalShortcuts { get; }
    public List<ShortcutDefinition> MainPageShortcuts { get; }
    public List<ShortcutDefinition> CharactersPageShortcuts { get; }

    public ShortcutsDialogue()
    {
        GlobalShortcuts = ShortcutManager.GetShortcuts(ShortcutScope.Global).ToList();
        MainPageShortcuts = ShortcutManager.GetShortcuts(ShortcutScope.MainPage).ToList();
        CharactersPageShortcuts = ShortcutManager.GetShortcuts(ShortcutScope.CharactersPage).ToList();

        InitializeComponent();
        CloseOnOutsideTap = true;
    }

    public static void Open()
    {
        _ = App.GetService<IDialogService>().ShowAsync(new ShortcutsDialogue());
    }

    private void OnCloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
