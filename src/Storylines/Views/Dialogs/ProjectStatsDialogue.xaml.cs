using Newtonsoft.Json.Linq;
using Storylines.Views.Controls;
using Storylines.Views.Pages;
using Storylines.Helpers;
using Storylines.Models;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static System.Net.Mime.MediaTypeNames;
using Storylines.Services;

namespace Storylines.Views.Dialogs
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

        public static void Open(bool fromDownBar)
        {
            _ = new ProjectStatsDialogue().ShowAsync();

            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData("TextStatsOpenedFromDownBar", fromDownBar.ToString());
            textBoxStats.DisplayStats();
        }

        public void DisplayStats()
        {
            RichEditBox textBox = MainPage.ChapterText.textBox;

            textBox.Document.GetText(TextGetOptions.None, out string txt);

            txt = txt.ToLower();

            int charactersCount = ServiceLocator.ProjectState.Characters.Count;

            string txtWithoutSpace = txt.Replace(" ", "");

            int wordCount = txt.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;

            int paragraphCount = Regex.Matches(txt, @"[^\r\n]*[^ \r\n]+[^\r\n]*((\r|\n|\r\n)[^\r\n]*[^ \r\n]+[^\r\n]*)*").Count;

            string storyText = GetTextFromAllChapters();
            int storyCharCount = storyText.Length > 1 ? storyText.Length - 2 : storyText.Length;
            int storyWords = storyText.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;
            int readMinutes = Math.Max(1, (int)Math.Ceiling(storyWords / 200.0));

            storyRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("charactersStory")}: {storyCharCount}\n{ResourceLoader.GetForCurrentView().GetString("words")}: {storyWords}\n{ResourceLoader.GetForCurrentView().GetString("estimatedReadTime")}: {readMinutes} {ResourceLoader.GetForCurrentView().GetString("min")}\n{ResourceLoader.GetForCurrentView().GetString("estimatedPageCount")}: {storyCharCount / 3838}";
            charactersRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("characters")}: {charactersCount}";
            chaptersRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("chapters")}: {ServiceLocator.ProjectState.Chapters.Count}";
            textRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("charactersStory")} ({ResourceLoader.GetForCurrentView().GetString("withoutSpaces")}): {txt.Length - 1}\n{ResourceLoader.GetForCurrentView().GetString("charactersStory")} ({ResourceLoader.GetForCurrentView().GetString("withSpaces")}): {txtWithoutSpace.Length - 1}\n{ResourceLoader.GetForCurrentView().GetString("paragraphs")}: {paragraphCount}\n{ResourceLoader.GetForCurrentView().GetString("words")}: {wordCount}";

            var stringBuilder = new StringBuilder();
            IOrderedEnumerable<IGrouping<string, Match>> wordFrequency
                = Regex.Matches(txt, @"\b[\w]*\b")
                .Where(m => m.Length > 0)
                .GroupBy(m => m.Value)
                .OrderByDescending(m => m.Count())
                .ThenBy(m => m.Key);
            foreach (IGrouping<string, Match> item in wordFrequency)
            {
                if (item != null)
                {
                    stringBuilder.AppendLine($"{item.Key}: {item.Count()}");
                }
            }

            if (stringBuilder.Length > 0)
                wordDistributionTextBox.Text = stringBuilder.ToString();

            PopulateChapterBars();
        }

        private void PopulateChapterBars()
        {
            chapterBarsPanel.Children.Clear();

            var chapters = ServiceLocator.ProjectState.Chapters;
            if (chapters == null || chapters.Count == 0)
                return;

            // Calculate word counts per chapter
            var stats = new System.Collections.Generic.List<(string name, int words)>();
            int maxWords = 1;
            foreach (var chapter in chapters)
            {
                var rb = new RichEditBox();
                rb.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, chapter.Text);
                rb.Document.GetText(Windows.UI.Text.TextGetOptions.None, out string plain);
                int words = plain.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;
                stats.Add((chapter.Name, words));
                if (words > maxWords) maxWords = words;
            }

            foreach (var (name, words) in stats)
            {
                var container = new StackPanel { Spacing = 2 };
                container.Children.Add(new TextBlock
                {
                    Text = $"{name}  ({words}w)",
                    FontSize = 11,
                    Opacity = 0.75,
                    TextTrimming = Windows.UI.Xaml.TextTrimming.CharacterEllipsis,
                    MaxWidth = 180
                });
                container.Children.Add(new ProgressBar
                {
                    Value = (double)words / maxWords * 100,
                    Maximum = 100,
                    Height = 6,
                    MinWidth = 160,
                    CornerRadius = new Windows.UI.Xaml.CornerRadius(3)
                });
                chapterBarsPanel.Children.Add(container);
            }
        }

        public static string GetTextFromAllChapters()
        { 
            string storyCharacterCount = "";
            foreach (Chapter chapter in ServiceLocator.ProjectState.Chapters)
            {
                RichEditBox richTxt = new RichEditBox();
                richTxt.Document.SetText(TextSetOptions.FormatRtf, chapter.Text);
                richTxt.Document.GetText(TextGetOptions.None, out string wordC);
                storyCharacterCount += wordC;
            }
            return storyCharacterCount;
        }

        public static void UpdateDownBar()
        {
            RichEditBox textBox = MainPage.ChapterText.textBox;

            textBox.Document.GetText(TextGetOptions.None, out string txt);

            int charCount = txt.Length > 0 ? txt.Length - 1 : 0;
            int wordCount = txt.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;

            string selectedPrefix = textBox.Document.Selection.Text.Length != 0 ? $"{textBox.Document.Selection.Text.Length} / " : "";

            int readMinutes = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));

            MainPage.Current.downBarWordsText.Text = $"{ResourceLoader.GetForCurrentView().GetString("words")}: {wordCount}";
            MainPage.Current.downBarCharsText.Text = $"{ResourceLoader.GetForCurrentView().GetString("charactersStory")}: {selectedPrefix}{charCount}";
            MainPage.Current.downBarReadTimeText.Text = $"~{readMinutes} {ResourceLoader.GetForCurrentView().GetString("readTimeMinRead")}";

            // Update chapter name if available
            var currentChapter = ServiceLocator.ProjectState.Chapters?.Count > 0 && ChaptersList.selectedIndex >= 0 && ChaptersList.selectedIndex < ServiceLocator.ProjectState.Chapters.Count ? ServiceLocator.ProjectState.Chapters[ChaptersList.selectedIndex] : null;
            if (currentChapter != null)
                MainPage.Current.downBarChapterName.Text = currentChapter.Name;

            // Keep legacy text for compatibility
            int paragraphCount = Regex.Matches(txt, @"[^\r\n]*[^ \r\n]+[^\r\n]*((\r|\n|\r\n)[^\r\n]*[^ \r\n]+[^\r\n]*)*").Count;
            MainPage.Current.downBarText.Text = $"{ResourceLoader.GetForCurrentView().GetString("charactersStory")}: {selectedPrefix}{charCount}   {ResourceLoader.GetForCurrentView().GetString("words")}: {wordCount}   {ResourceLoader.GetForCurrentView().GetString("paragraphs")}: {paragraphCount}";
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
