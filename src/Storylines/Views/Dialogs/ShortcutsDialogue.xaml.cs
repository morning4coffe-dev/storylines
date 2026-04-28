using System.Collections.Generic;
using System.Linq;
using Storylines.Helpers;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ShortcutsDialogue : ContentDialog
    {
        public static ShortcutsDialogue textBoxStats;

        public List<ShortcutDefinition> GlobalShortcuts { get; }
        public List<ShortcutDefinition> MainPageShortcuts { get; }
        public List<ShortcutDefinition> CharactersPageShortcuts { get; }

        public ShortcutsDialogue()
        {
            GlobalShortcuts = ShortcutManager.GetShortcuts(ShortcutScope.Global).ToList();
            MainPageShortcuts = ShortcutManager.GetShortcuts(ShortcutScope.MainPage).ToList();
            CharactersPageShortcuts = ShortcutManager.GetShortcuts(ShortcutScope.CharactersPage).ToList();

            InitializeComponent();
            textBoxStats = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = textBoxStats;
            textBoxStats.RequestedTheme = AppView.current.ActualTheme;
        }

        public static void Open()
        {
            _ = new ShortcutsDialogue().ShowAsync();
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            AppView.currentlyOpenedDialogue = null;
        }

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        bool isHide = true;
        private void InitializeClickOutToClose()
        {
            Window.Current.CoreWindow.PointerPressed += (s, e) =>
            {
                if (isHide)
                    Hide();
            };

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }
    }
}
