using Storylines.Views.Controls;
using Storylines.Views.Pages;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Storylines.Helpers.Modes
{
    public class ReadMode
    {
        private CommandBar CommandBar;

        public RichTextBlock txtBox;

        public static void Switch()
        {
            MainPage.ReadMode = new ReadMode();
            MainPage.ReadMode.PrivateSwitch();
        }

        private void PrivateSwitch()
        {
            MainPage.ChapterText.Visibility = Visibility.Collapsed;
            MainPage.Current.OpenOrCloseChapterList(false, true);

            // ── Fix: extract plain text from the RichEditBox, not raw RTF ──
            MainPage.ChapterText.textBox.Document.GetText(TextGetOptions.None, out var plainText);

            txtBox = new RichTextBlock()
            {
                Margin = new Thickness(60, 40, 60, 40),
                FontSize = 15,
                LineHeight = 26,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                SelectionHighlightColor = new SolidColorBrush(
                    (Color)Application.Current.Resources["SystemAccentColor"]),
            };

            // Build one Paragraph per line break so formatting is clean
            foreach (var line in (plainText ?? string.Empty).Split('\r'))
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run { Text = line });
                txtBox.Blocks.Add(paragraph);
            }

            MainPage.Current.mainGrid.Children.Add(txtBox);
            Grid.SetRow(txtBox, 1);

            MainCommandBar mainCommandBarInstance = new MainCommandBar();
            CommandBar = ModesShared.NewCommandBar();

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.undoButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.undoButton);

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.redoButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.redoButton);

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.saveButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.saveButton);

            CommandBar.PrimaryCommands.Add(new AppBarSeparator());

            _ = mainCommandBarInstance.commandBarHelp.PrimaryCommands.Remove(mainCommandBarInstance.readAloudButton);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.readAloudButton);

            _ = mainCommandBarInstance.commandBarHelp.PrimaryCommands.Remove(mainCommandBarInstance.readAloudControllHolder);
            CommandBar.PrimaryCommands.Add(mainCommandBarInstance.readAloudControllHolder);

            ModesShared.RemoveChapterTextCommandBar();
            AppView.current.BackButtonCheck();
        }

        public void Leave()
        {
            MainPage.ChapterText.Visibility = Visibility.Visible;
            MainPage.Current.mainGrid.Children.Remove(txtBox);
            MainPage.ReadMode = null;
            AppView.current.BackButtonCheck();
        }
    }
}
