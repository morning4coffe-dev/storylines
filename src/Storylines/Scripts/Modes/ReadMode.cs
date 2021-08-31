using Storylines.Components;
using Storylines.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace Storylines.Scripts.Modes
{
    public class ReadMode
    {
        private CommandBar commandBar;

        public RichTextBlock txtBox;

        public static void Switch()
        {
            MainPage.readMode = new ReadMode();

            MainPage.readMode.PrivateSwitch();
        }

        private void PrivateSwitch()
        {
            MainPage.chapterText.Visibility = Visibility.Collapsed;

            MainPage.current.OpenOrCloseChapterList(false, true);
            //if edit is disabled
            MainPage.chapterText.textBox.Document.GetText(Windows.UI.Text.TextGetOptions.FormatRtf, out var txt);
            txtBox = new RichTextBlock()
            {
                Margin = new Thickness(40),
                SelectionHighlightColor = new Windows.UI.Xaml.Media.SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
            };

            Run run = new Run() { Text = txt };
            Paragraph paragraph = new Paragraph();
            paragraph.Inlines.Add(run);
            txtBox.Blocks.Add(paragraph);

            MainPage.current.mainGrid.Children.Add(txtBox);
            Grid.SetRow(txtBox, 1);

            MainCommandBar mainCommandBarInstance = new MainCommandBar();
            commandBar = ModesShared.NewCommandBar();

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.undoButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.undoButton);

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.redoButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.redoButton);

            _ = mainCommandBarInstance.commandBarFile.PrimaryCommands.Remove(mainCommandBarInstance.saveButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.saveButton);

            commandBar.PrimaryCommands.Add(new AppBarSeparator());

            _ = mainCommandBarInstance.commandBarHelp.PrimaryCommands.Remove(mainCommandBarInstance.readAloudButton);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.readAloudButton);

            _ = mainCommandBarInstance.commandBarHelp.PrimaryCommands.Remove(mainCommandBarInstance.readAloudControllHolder);
            commandBar.PrimaryCommands.Add(mainCommandBarInstance.readAloudControllHolder);

            ModesShared.RemoveChapterTextCommandBar();

            AppView.current.BackButtonCheck();
        }

        public void Leave()
        {
            MainPage.readMode = new ReadMode();

            MainPage.chapterText.Visibility = Visibility.Visible;
            MainPage.current.mainGrid.Children.Remove(txtBox);
        }
    }
}
