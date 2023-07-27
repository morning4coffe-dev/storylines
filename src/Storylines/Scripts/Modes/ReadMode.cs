using Storylines.Components;
using Storylines.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace Storylines.Scripts.Modes
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
            //if edit is disabled
            MainPage.ChapterText.textBox.Document.GetText(Windows.UI.Text.TextGetOptions.FormatRtf, out var txt);
            txtBox = new RichTextBlock()
            {
                Margin = new Thickness(40),
                SelectionHighlightColor = new Windows.UI.Xaml.Media.SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
            };

            Run run = new Run() { Text = txt };
            Paragraph paragraph = new Paragraph();
            paragraph.Inlines.Add(run);
            txtBox.Blocks.Add(paragraph);

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
            MainPage.ReadMode = new ReadMode();

            MainPage.ChapterText.Visibility = Visibility.Visible;
            MainPage.Current.mainGrid.Children.Remove(txtBox);
        }
    }
}
