using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class ShortcutsDialogue : ContentDialog
    {
        public static ShortcutsDialogue textBoxStats;

        public ShortcutsDialogue()
        {
            InitializeComponent();
            textBoxStats = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = textBoxStats;
            textBoxStats.RequestedTheme = AppView.current.RequestedTheme;
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
