using Storylines.Pages;
using Storylines.Scripts.Variables;
using System;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Resources;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class ProjectStatsDialogue : ContentDialog
    {
        public static ProjectStatsDialogue textBoxStats;

        public ProjectStatsDialogue()
        {
            InitializeComponent();
            textBoxStats = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = textBoxStats;
            textBoxStats.RequestedTheme = AppView.current.RequestedTheme;
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

            int charactersCount = Character.characters.Count;

            string txtWithoutSpace = txt.Replace(" ", "");

            int wordCount = txt.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;

            int paragraphCount = Regex.Matches(txt, "[^\r\n]+((\r|\n|\r\n)[^\r\n]+)*").Count;

            string storyCharacterCount = GetTextFromAllChapters();

            storyRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("charactersStory")}: {(storyCharacterCount.Length > 1 ? storyCharacterCount.Length - 2 : storyCharacterCount.Length)}\n{ResourceLoader.GetForCurrentView().GetString("words")}: {storyCharacterCount.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length}\n{ResourceLoader.GetForCurrentView().GetString("estimatedPageCount")}: {storyCharacterCount.Length / 3838}";
            charactersRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("characters")}: {charactersCount}";
            chaptersRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("chapters")}: {Chapter.chapters.Count}";
            textRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("charactersStory")} ({ResourceLoader.GetForCurrentView().GetString("withoutSpaces")}): {txt.Length - 1}\n{ResourceLoader.GetForCurrentView().GetString("charactersStory")} ({ResourceLoader.GetForCurrentView().GetString("withSpaces")}): {txtWithoutSpace.Length - 1}\n{ResourceLoader.GetForCurrentView().GetString("words")}: {wordCount}\n{ResourceLoader.GetForCurrentView().GetString("paragraphs")}: {paragraphCount}";/*\n{ResourceLoader.GetForCurrentView().GetString("selectedCharacters")}: {selectedLetters}*/
        }

        public static string GetTextFromAllChapters()
        { 
            string storyCharacterCount = "";
            foreach (Chapter chapter in Chapter.chapters)
            {
                RichEditBox richTxt = new RichEditBox();
                richTxt.Document.SetText(TextSetOptions.FormatRtf, chapter.text);
                richTxt.Document.GetText(TextGetOptions.None, out string wordC);
                storyCharacterCount += wordC;
            }
            return storyCharacterCount;
        }

        public static void UpdateDownBar()
        {
            RichEditBox textBox = MainPage.chapterText.textBox;

            textBox.Document.GetText(TextGetOptions.None, out string txt);
            _ = txt.Replace(" ", "");

            int wordCount = txt.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;

            int paragraphCount = Regex.Matches(txt, "[^\r\n]+((\r|\n|\r\n)[^\r\n]+)*").Count;

            string selectedLetters = textBox.Document.Selection.Text.Length != 0 ? $"{textBox.Document.Selection.Text.Length} / " : "";

            MainPage.current.downBarText.Text = $"{ResourceLoader.GetForCurrentView().GetString("charactersStory")}: {selectedLetters}{txt.Length - 1}   {ResourceLoader.GetForCurrentView().GetString("words")}: {wordCount}   {ResourceLoader.GetForCurrentView().GetString("paragraphs")}: {paragraphCount}";
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
