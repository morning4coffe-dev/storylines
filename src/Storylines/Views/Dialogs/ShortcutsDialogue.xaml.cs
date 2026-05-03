using System.Collections.Generic;
using System.Linq;
using Storylines.Helpers;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
            DialogHelper.EnsureXamlRoot(this);
            textBoxStats = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = textBoxStats;
            textBoxStats.RequestedTheme = App.GetService<WindowContext>().AppView.ActualTheme;
        }

        public static void Open()
        {
            _ = new ShortcutsDialogue().ShowAsync();
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            App.GetService<WindowContext>().RootElement.PointerPressed -= OnWindowPointerPressed;
            AppView.currentlyOpenedDialogue = null;

            if (ReferenceEquals(textBoxStats, this))
                textBoxStats = null;
        }

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        bool isHide = true;
        private void InitializeClickOutToClose()
        {
            App.GetService<WindowContext>().RootElement.PointerPressed += OnWindowPointerPressed;

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }

        private void OnWindowPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (isHide)
                Hide();
        }
    }
}
