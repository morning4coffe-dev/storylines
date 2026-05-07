using CommunityToolkit.WinUI.Controls;
using Storylines.Services;
using Storylines.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Storylines.Helpers;
using Storylines.Services.Interfaces;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ChapterTagsDialogue : StorylinesContentDialog
    {
        private readonly Chapter _chapter;

        public ChapterTagsDialogue(Chapter chapter)
        {
            InitializeComponent();
            CloseOnOutsideTap = true;
            _chapter = chapter;
        }

        public static void Open(Chapter chapter)
        {
            _ = OpenAsync(chapter);
        }

        public static Task<ContentDialogResult> OpenAsync(Chapter chapter)
        {
            return App.GetService<IDialogService>().ShowAsync(new ChapterTagsDialogue(chapter));
        }

        // ─── Lifecycle ────────────────────────────────────────────────

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            chapterNameText.Text = _chapter?.Name ?? string.Empty;

            // Populate existing tags
            tagsTokenBox.Items.Clear();
            if (_chapter?.Tags is not null)
                foreach (var tag in _chapter.Tags)
                    tagsTokenBox.Items.Add(tag);

            // Populate suggestion pills — presets minus already-added tags
            RefreshSuggestions();
            RefreshSavedPresets();
        }

        // ─── Suggestions ──────────────────────────────────────────────

        private void RefreshSuggestions()
        {
            var current = GetCurrentTags();
            var suggestions = ChapterTagsService
                .GetAllSuggestions(App.GetService<ProjectState>().Chapters)
                .Where(s => !current.Contains(s, StringComparer.CurrentCultureIgnoreCase))
                .Take(12)
                .ToList();

            suggestionPills.ItemsSource = suggestions;
        }

        private void RefreshSavedPresets()
        {
            var presets = ChapterTagsService
                .GetPresets()
                .OrderBy(preset => preset, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            savedPresetsList.ItemsSource = presets;
            savedPresetsEmptyText.Visibility = presets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private List<string> GetCurrentTags()
        {
            var list = new List<string>();
            foreach (var item in tagsTokenBox.Items)
                list.Add(item?.ToString() ?? string.Empty);
            return list;
        }

        private void OnSuggestionPill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string tag)
            {
                if (!GetCurrentTags().Contains(tag, StringComparer.CurrentCultureIgnoreCase))
                    tagsTokenBox.Items.Add(tag);

                RefreshSuggestions();
            }
        }

        // ─── Token events ─────────────────────────────────────────────

        private void OnTagsTokenBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text.Trim();
                var allSuggestions = ChapterTagsService.GetAllSuggestions(App.GetService<ProjectState>().Chapters);
                sender.ItemsSource = string.IsNullOrWhiteSpace(query)
                    ? allSuggestions
                    : allSuggestions.Where(s => s.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }
        }

        private void OnTokenItem_Added(TokenizingTextBox sender, object args)
        {
            // Persist new tags to presets
            if (args is string tag && !string.IsNullOrWhiteSpace(tag))
                ChapterTagsService.AddPreset(tag.Trim());

            RefreshSuggestions();
            RefreshSavedPresets();
        }

        private void OnTokenItem_Removing(TokenizingTextBox sender, TokenItemRemovingEventArgs args)
        {
            RefreshSuggestions();
        }

        private void OnRemovePreset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string preset && !string.IsNullOrWhiteSpace(preset))
            {
                ChapterTagsService.RemovePreset(preset);
                RefreshSuggestions();
                RefreshSavedPresets();
            }
        }

        // ─── Action buttons ───────────────────────────────────────────

        private void OnSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chapter is null) return;

            var newTags = GetCurrentTags()
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            _chapter.Tags = newTags;
            TimeTravelSystem.SomethingChanged();

            Hide();
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e) => Hide();
    }
}
