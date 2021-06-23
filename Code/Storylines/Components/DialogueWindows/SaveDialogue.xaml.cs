using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class SaveDialogue : ContentDialog
    {
        public static SaveDialogue saveDialogue;

        public SaveDialogue()
        {
            this.InitializeComponent();
            saveDialogue = this;

            saveDialogue.RequestedTheme = MainPage.mainPage.RequestedTheme;
            MainPage.currentlyOpenedDialogue = saveDialogue;
        }

        public static void Open()
        {
            try
            {
                _ = new SaveDialogue().ShowAsync();
            }
            catch
            {
                MainPage.currentlyOpenedDialogue.Hide();
            }
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (saveFileNameBox.Text == " " || saveFileNameBox.Text.Contains("/") || saveFileNameBox.Text.Contains(@"\") || saveFileNameBox.Text.Contains(@"""".FirstOrDefault().ToString()) || saveFileNameBox.Text.Contains(":") || saveFileNameBox.Text.Contains("<") || saveFileNameBox.Text.Contains(">") || saveFileNameBox.Text.Contains("|") || saveFileNameBox.Text == null)
            {
                saveFileNameBox.PlaceholderForeground = new SolidColorBrush(new Color() { A = 255, R = 252, B = 3, G = 40 });
                saveFileNameBox.Foreground = new SolidColorBrush(new Color() { A = 255, R = 252, B = 3, G = 40 });
            }
            else
            {
                MainPage.saveSystem.OpenFileExplorerSaveAsync(saveFileNameBox.Text);
                saveDialogue.Hide();
            }
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            saveDialogue.Hide();
        }

        private void ContentDialog_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                OnSubmitButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            MainPage.currentlyOpenedDialogue = null;
        }
    }
}
