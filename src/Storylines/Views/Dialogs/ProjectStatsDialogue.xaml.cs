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
using Windows.UI.Xaml.Media;
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
            textBoxStats = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = textBoxStats;
            textBoxStats.RequestedTheme = AppView.current.ActualTheme;
        }

        public static void Open(bool fromDownBar)
        {
            _ = new ProjectStatsDialogue().ShowAsync();

            App.TryGetService<ITelemetryService>()?.TrackProjectStatsOpened(fromDownBar);
            textBoxStats.DisplayStats();
        }

        public void DisplayStats()
        {
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

            storyRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("charactersStory")}: {storyCharCount}\n{ResourceLoader.GetForCurrentView().GetString("words")}: {storyWords}\n{ResourceLoader.GetForCurrentView().GetString("estimatedReadTime")}: {readMinutes} {ResourceLoader.GetForCurrentView().GetString("min")}\n{ResourceLoader.GetForCurrentView().GetString("estimatedPageCount")}: {storyCharCount / 3838}";
            charactersRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("characters")}: {charactersCount}";
            chaptersRun.Text = $"{ResourceLoader.GetForCurrentView().GetString("chapters")}: {ProjectState.Chapters.Count}";
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
                rb.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, chapter.Text);
                rb.Document.GetText(Windows.UI.Text.TextGetOptions.None, out string plain);
                int words = plain.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;
                stats.Add((chapter.Name, words));
                if (words > maxWords) maxWords = words;
            }

            const double barHeight = 22;
            const double barSpacing = 4;
            const double labelWidth = 140;
            const double chartWidth = 300;
            double y = 0;

            var accentBrush = new SolidColorBrush((Windows.UI.Color)Windows.UI.Xaml.Application.Current.Resources["SystemAccentColor"]);

            foreach (var (name, words) in stats)
            {
                double barWidth = Math.Max(2, (double)words / maxWords * chartWidth);

                // Chapter label
                var label = new TextBlock
                {
                    Text = name,
                    FontSize = 11,
                    Opacity = 0.8,
                    TextTrimming = Windows.UI.Xaml.TextTrimming.CharacterEllipsis,
                    MaxWidth = labelWidth - 8,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y + 2);
                chapterChartCanvas.Children.Add(label);

                // Bar
                var bar = new Windows.UI.Xaml.Shapes.Rectangle
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
