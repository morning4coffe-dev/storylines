using Storylines.Pages;
using System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class ProjectStatsDialogue : ContentDialog
    {
        public static ProjectStatsDialogue textBoxStats;

        public SolidColorBrush appColor = new SolidColorBrush(SettingsPage.appColorEnabled ? SettingsPage.appColor : new Windows.UI.ViewManagement.UISettings().GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent));

        public ProjectStatsDialogue()
        {
            InitializeComponent();
            textBoxStats = this;

            MainPage.currentlyOpenedDialogue = textBoxStats;
            textBoxStats.RequestedTheme = MainPage.mainPage.RequestedTheme;
        }

        public static void Open()
        {
            _ = new ProjectStatsDialogue().ShowAsync();

            textBoxStats.DisplayStats();
        }

        public void DisplayStats()
        {
            RichEditBox textBox = MainPage.chapterText.textBox;

            textBox.Document.GetText(TextGetOptions.None, out string txt);

            int charactersCount = Characters.characters.Count;

            string txtWithoutSpace = txt.Replace(" ", "");

            string[] words = txt.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries);

            string[] paragraphs = txt.Split((char)13, StringSplitOptions.None);

            string selectedLetters = textBox.Document.Selection.Text.Length != 0 ? $"{textBox.Document.Selection.Text.Length}" : "0";

            string storyCharacterCount = "";

            foreach (Chapter chapter in MainPage.chapterList.chapters)
            {
                RichEditBox richTxt = new RichEditBox();
                richTxt.Document.SetText(TextSetOptions.FormatRtf, chapter.text);
                richTxt.Document.GetText(TextGetOptions.None, out string wordC);
                storyCharacterCount += wordC;
            }

            storyRun.Text = $"Characters: {(storyCharacterCount.Length > 1 ? storyCharacterCount.Length - 2 : storyCharacterCount.Length)}\nWords: {storyCharacterCount.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length}\nEstimated page count: {(storyCharacterCount.Length / 3800) + 1}";
            charactersRun.Text = $"Characters: {charactersCount}";
            chaptersRun.Text = $"Chapters: {MainPage.chapterList.chapters.Count}";
            textRun.Text = $"Characters (with / without spaces): {txt.Length - 1} / {txtWithoutSpace.Length - 1}\nWords: {words.Length}\nParagraphs: {paragraphs.Length - 1}\nSelected characters: {selectedLetters}";
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            MainPage.currentlyOpenedDialogue = null;
        }

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void OnStoryHyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {

        }
    }
}
