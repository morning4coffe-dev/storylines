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
using Windows.ApplicationModel.Resources;

namespace Storylines.Views.Dialogs
{
    public sealed partial class GlobalSearchDialogue : StorylinesContentDialog
    {
        private readonly List<GlobalSearchResult> _results = new List<GlobalSearchResult>();
        private readonly INavigationService _navigation;
        private readonly ProjectState _projectState;
        private readonly List<GlobalSearchResult> _quickActions;
        private readonly string _initialQuery;
        private readonly WindowContext _windowContext;

        public GlobalSearchDialogue(string initialQuery = null)
        {
            InitializeComponent();
            _navigation = App.GetService<INavigationService>();
            _projectState = App.GetService<ProjectState>();
            _windowContext = App.GetService<WindowContext>();
            _initialQuery = initialQuery?.Trim();

            var resources = ResourceLoader.GetForViewIndependentUse();
            Title = resources.GetString("shortcutSearch");
            searchBox.PlaceholderText = resources.GetString("searchBox.PlaceholderText");
            _quickActions = BuildQuickActions(resources).ToList();

            Loaded += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(_initialQuery))
                {
                    searchBox.Text = _initialQuery;
                    searchBox.SelectionStart = searchBox.Text.Length;
                }

                searchBox.Focus(FocusState.Programmatic);
                RefreshResults();
            };
        }

        public static async Task OpenAsync(string initialQuery = null)
        {
            try
            {
                var dialog = new GlobalSearchDialogue(initialQuery);
                await App.GetService<IDialogService>().ShowAsync(dialog);
            }
            catch (Exception ex)
            {
                App.TryGetService<ILogger>()?.Warning($"Failed to open global search dialog: {ex.Message}");
            }
        }

        private void OnSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshResults();
        }

        private void OnResultItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is GlobalSearchResult result)
            {
                result.Execute?.Invoke();
            }
        }

        private void RefreshResults()
        {
            var query = searchBox.Text?.Trim();
            _results.Clear();

            _results.AddRange(GetQuickActions(query));

            if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
                _results.AddRange(GetChapterResults(query));

            resultsListView.ItemsSource = _results.ToList();
            noResultsText.Visibility = _results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private IEnumerable<GlobalSearchResult> GetQuickActions(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _quickActions;

            return _quickActions.Where(result => result.Matches(query));
        }

        private IEnumerable<GlobalSearchResult> GetChapterResults(string query)
        {
            for (int i = 0; i < _projectState.Chapters.Count; i++)
            {
                var chapter = _projectState.Chapters[i];
                string chapterTitle = BuildChapterTitle(i, chapter.Name);
                string plainText = ConvertToPlainText(chapter.Text);

                int matchIndex = plainText.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
                if (matchIndex >= 0)
                {
                    int start = Math.Max(0, matchIndex - 40);
                    int end = Math.Min(plainText.Length, matchIndex + query.Length + 80);
                    string preview = (start > 0 ? "…" : "") + plainText.Substring(start, end - start).Trim() + (end < plainText.Length ? "…" : "");

                    yield return new GlobalSearchResult(
                        chapterTitle,
                        preview,
                        () => NavigateToChapter(i));
                }

                if (chapter.Notes?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    yield return new GlobalSearchResult(
                        chapterTitle,
                        chapter.Notes.Substring(0, Math.Min(120, chapter.Notes.Length)),
                        () => NavigateToChapter(i));
                }

                if (chapter.Synopsis?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    yield return new GlobalSearchResult(
                        chapterTitle,
                        chapter.Synopsis.Substring(0, Math.Min(120, chapter.Synopsis.Length)),
                        () => NavigateToChapter(i));
                }
            }
        }

        private IEnumerable<GlobalSearchResult> BuildQuickActions(ResourceLoader resources)
        {
            yield return new GlobalSearchResult(
                resources.GetString("storyText.Text"),
                string.Empty,
                () => NavigateToPage(NavigationTarget.MainPage));

            yield return new GlobalSearchResult(
                resources.GetString("charactersStory"),
                string.Empty,
                () => NavigateToPage(NavigationTarget.Characters));

            yield return new GlobalSearchResult(
                resources.GetString("shortcutOpenSettings"),
                string.Empty,
                () => NavigateToPage(NavigationTarget.Settings));
        }

        private static string BuildChapterTitle(int index, string chapterName)
        {
            string number = (index + 1).ToString();
            return string.IsNullOrWhiteSpace(chapterName) ? number : $"{number}. {chapterName}";
        }

        private void NavigateToPage(NavigationTarget target)
        {
            Hide();
            _navigation.NavigateTo(target);
        }

        private void NavigateToChapter(int index)
        {
            if (index < 0 || index >= _projectState.Chapters.Count)
                return;

            Hide();

            var chapter = _projectState.Chapters[index];
            if (_windowContext?.AppView?.pagesView?.Content is Pages.MainPage && _windowContext.ChapterList?.ViewModel != null)
            {
                var textEditor = App.GetService<ITextEditorService>();
                textEditor.SelectedChapterIndex = index;
                _windowContext.ChapterList.ViewModel.SelectedIndex = index;

                if (_windowContext.ChapterList.listView != null)
                    _windowContext.ChapterList.listView.SelectedIndex = index;

                return;
            }

            _navigation.NavigateTo(NavigationTarget.MainPage, chapter.Token);
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

    public sealed class GlobalSearchResult
    {
        public GlobalSearchResult(string title, string description, Action execute)
        {
            Title = title;
            Description = description;
            Execute = execute;
        }

        public string Title { get; }

        public string Description { get; }

        public Action Execute { get; }

        public Visibility DescriptionVisibility => string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;

        public double RowOpacity => Execute == null ? 0.68 : 1.0;

        public bool Matches(string query)
        {
            return Title?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0
                || Description?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
