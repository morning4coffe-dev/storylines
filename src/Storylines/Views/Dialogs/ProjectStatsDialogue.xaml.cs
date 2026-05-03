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
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static System.Net.Mime.MediaTypeNames;
using Storylines.Services;
using Storylines.Services.Interfaces;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ProjectStatsDialogue : ContentDialog
    {
        private static ProjectState ProjectState => App.GetService<ProjectState>();

        public static ProjectStatsDialogue textBoxStats;

        public ProjectStatsDialogue()
        {
            InitializeComponent();
            DialogHelper.EnsureXamlRoot(this);
            textBoxStats = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = textBoxStats;
            textBoxStats.RequestedTheme = AppView.current.ActualTheme;
        }

        public static void Open(bool fromDownBar)
        {
            var dialog = new ProjectStatsDialogue();

            _ = dialog.ShowAsync();

            App.TryGetService<ITelemetryService>()?.TrackProjectStatsOpened(fromDownBar);
            dialog.DisplayStats();
        }

        public void DisplayStats()
        {
            var resourceLoader = ResourceLoader.GetForViewIndependentUse();

            RichEditBox textBox = MainPage.ChapterText.textBox;

            textBox.Document.GetText(TextGetOptions.None, out string txt);

            txt = txt.ToLower();

            int charactersCount = ProjectState.Characters.Count;

            string txtWithoutSpace = txt.Replace(" ", "");

            int wordCount = txt.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;

            int paragraphCount = Regex.Matches(txt, @"[^\r\n]*[^ \r\n]+[^\r\n]*((\r|\n|\r\n)[^\r\n]*[^ \r\n]+[^\r\n]*)*").Count;

            string storyText = GetTextFromAllChapters();
            int storyCharCount = storyText.Length > 1 ? storyText.Length - 2 : storyText.Length;
            int storyWords = storyText.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;
            int readMinutes = Math.Max(1, (int)Math.Ceiling(storyWords / 200.0));
            int chapterCount = ProjectState.Chapters.Count;
            int draftCount = ProjectState.Chapters.Count(chapter => chapter.Status == ChapterStatus.Draft);
            int writingCount = ProjectState.Chapters.Count(chapter => chapter.Status == ChapterStatus.Writing);
            int revisionCount = ProjectState.Chapters.Count(chapter => chapter.Status == ChapterStatus.Revision);
            int doneCount = ProjectState.Chapters.Count(chapter => chapter.Status == ChapterStatus.Final);

            storyRun.Text = $"{resourceLoader.GetString("charactersStory")}: {storyCharCount}\n{resourceLoader.GetString("words")}: {storyWords}\n{resourceLoader.GetString("estimatedReadTime")}: {readMinutes} {resourceLoader.GetString("min")}\n{resourceLoader.GetString("estimatedPageCount")}: {storyCharCount / 3838}";
            charactersRun.Text = $"{resourceLoader.GetString("characters")}: {charactersCount}";
            chaptersRun.Text = $"{resourceLoader.GetString("chapters")}: {chapterCount}\n{resourceLoader.GetString("done")}: {doneCount}\n{resourceLoader.GetString("projectStatsWritingLabel")}: {writingCount}\n{resourceLoader.GetString("projectStatsRevisionLabel")}: {revisionCount}\n{resourceLoader.GetString("projectStatsDraftLabel")}: {draftCount}";
            textRun.Text = $"{resourceLoader.GetString("charactersStory")} ({resourceLoader.GetString("withoutSpaces")}): {txt.Length - 1}\n{resourceLoader.GetString("charactersStory")} ({resourceLoader.GetString("withSpaces")}): {txtWithoutSpace.Length - 1}\n{resourceLoader.GetString("paragraphs")}: {paragraphCount}\n{resourceLoader.GetString("words")}: {wordCount}";

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
            chapterChartCanvas.Children.Clear();

            var chapters = ProjectState.Chapters;
            if (chapters == null || chapters.Count == 0)
                return;

            // Calculate word counts per chapter
            var stats = new System.Collections.Generic.List<(string name, int words)>();
            int maxWords = 1;
            foreach (var chapter in chapters)
            {
                var rb = new RichEditBox();
                rb.Document.SetText(Microsoft.UI.Text.TextSetOptions.FormatRtf, chapter.Text);
                rb.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string plain);
                int words = plain.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;
                stats.Add((chapter.Name, words));
                if (words > maxWords) maxWords = words;
            }

            const double barHeight = 22;
            const double barSpacing = 4;
            const double labelWidth = 140;
            const double chartWidth = 300;
            double y = 0;

            var accentBrush = new SolidColorBrush((Windows.UI.Color)Microsoft.UI.Xaml.Application.Current.Resources["SystemAccentColor"]);

            foreach (var (name, words) in stats)
            {
                double barWidth = Math.Max(2, (double)words / maxWords * chartWidth);

                // Chapter label
                var label = new TextBlock
                {
                    Text = name,
                    FontSize = 11,
                    Opacity = 0.8,
                    TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis,
                    MaxWidth = labelWidth - 8,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y + 2);
                chapterChartCanvas.Children.Add(label);

                // Bar
                var bar = new Microsoft.UI.Xaml.Shapes.Rectangle
                {
                    Width = barWidth,
                    Height = barHeight - 6,
                    RadiusX = 3,
                    RadiusY = 3,
                    Fill = accentBrush,
                    Opacity = 0.7
                };
                Canvas.SetLeft(bar, labelWidth);
                Canvas.SetTop(bar, y + 3);
                chapterChartCanvas.Children.Add(bar);

                // Word count label
                var countLabel = new TextBlock
                {
                    Text = $"{words}w",
                    FontSize = 10,
                    Opacity = 0.55,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(countLabel, labelWidth + barWidth + 6);
                Canvas.SetTop(countLabel, y + 3);
                chapterChartCanvas.Children.Add(countLabel);

                y += barHeight + barSpacing;
            }

            chapterChartCanvas.Width = labelWidth + chartWidth + 60;
            chapterChartCanvas.Height = y;
        }

        public static string GetTextFromAllChapters()
        { 
            string storyCharacterCount = "";
            foreach (Chapter chapter in ProjectState.Chapters)
            {
                RichEditBox richTxt = new RichEditBox();
                richTxt.Document.SetText(TextSetOptions.FormatRtf, chapter.Text);
                richTxt.Document.GetText(TextGetOptions.None, out string wordC);
                storyCharacterCount += wordC;
            }
            return storyCharacterCount;
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            App.MainWindow.Content.PointerPressed -= OnWindowPointerPressed;
            AppView.currentlyOpenedDialogue = null;
        }

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        bool isHide = true;
        private void InitializeClickOutToClose()
        {
            App.MainWindow.Content.PointerPressed += OnWindowPointerPressed;

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
