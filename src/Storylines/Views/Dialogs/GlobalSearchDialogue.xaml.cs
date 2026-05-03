using Storylines.Services;
using Storylines.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Storylines.Services.Interfaces;
using Storylines.Helpers;

namespace Storylines.Views.Dialogs
{
    public sealed partial class GlobalSearchDialogue : ContentDialog
    {
        private readonly List<GlobalSearchResult> _results = new List<GlobalSearchResult>();
        private Action<int> _navigateToChapter;

        public GlobalSearchDialogue()
        {
            InitializeComponent();
            DialogHelper.EnsureXamlRoot(this);
        }

        public static async Task OpenAsync()
        {
            try
            {
                var dialog = new GlobalSearchDialogue();
                var navigation = App.GetService<INavigationService>();
                var textEditor = App.GetService<ITextEditorService>();
                dialog._navigateToChapter = (index) =>
                {
                    dialog.Hide();
                    navigation.GoBack();
                    textEditor.SelectedChapterIndex = index;
                    if (Pages.MainPage.ChapterList?.listView != null)
                        Pages.MainPage.ChapterList.listView.SelectedIndex = index;
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                App.TryGetService<ILogger>()?.Warning($"Failed to open global search dialog: {ex.Message}");
            }
        }

        private void OnSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = searchBox.Text?.Trim();
            _results.Clear();

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                resultsListView.ItemsSource = null;
                resultsCountText.Text = "";
                noResultsText.Visibility = Visibility.Collapsed;
                return;
            }

            var chapters = App.GetService<ProjectState>().Chapters;
            for (int i = 0; i < chapters.Count; i++)
            {
                var chapter = chapters[i];
                string plainText = ConvertToPlainText(chapter.Text);

                int matchIndex = plainText.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
                if (matchIndex >= 0)
                {
                    // Build preview around the match
                    int start = Math.Max(0, matchIndex - 40);
                    int end = Math.Min(plainText.Length, matchIndex + query.Length + 80);
                    string preview = (start > 0 ? "…" : "") + plainText.Substring(start, end - start).Trim() + (end < plainText.Length ? "…" : "");

                    _results.Add(new GlobalSearchResult
                    {
                        ChapterName = $"Ch. {i + 1}: {chapter.Name}",
                        MatchPreview = preview,
                        ChapterIndex = i
                    });
                }

                // Also search notes and synopsis
                if (chapter.Notes?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    _results.Add(new GlobalSearchResult
                    {
                        ChapterName = $"Ch. {i + 1}: {chapter.Name} (Notes)",
                        MatchPreview = chapter.Notes.Substring(0, Math.Min(120, chapter.Notes.Length)),
                        ChapterIndex = i
                    });
                }

                if (chapter.Synopsis?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    _results.Add(new GlobalSearchResult
                    {
                        ChapterName = $"Ch. {i + 1}: {chapter.Name} (Synopsis)",
                        MatchPreview = chapter.Synopsis.Substring(0, Math.Min(120, chapter.Synopsis.Length)),
                        ChapterIndex = i
                    });
                }
            }

            resultsListView.ItemsSource = _results.ToList();
            resultsCountText.Text = $"{_results.Count} result{(_results.Count == 1 ? "" : "s")}";
            noResultsText.Visibility = _results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnResultItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is GlobalSearchResult result)
            {
                _navigateToChapter?.Invoke(result.ChapterIndex);
            }
        }

        private static string ConvertToPlainText(string chapterText)
        {
            if (string.IsNullOrWhiteSpace(chapterText))
                return string.Empty;

            var box = new RichEditBox();
            box.Document.SetText(TextSetOptions.FormatRtf, chapterText);
            box.Document.GetText(TextGetOptions.None, out string plainText);
            return plainText ?? string.Empty;
        }
    }

    public class GlobalSearchResult
    {
        public string ChapterName { get; set; }
        public string MatchPreview { get; set; }
        public int ChapterIndex { get; set; }
    }
}
