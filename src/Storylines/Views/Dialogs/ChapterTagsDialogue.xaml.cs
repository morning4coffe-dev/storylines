using Microsoft.Toolkit.Uwp.UI.Controls;
using Storylines.Services;
using Storylines.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Storylines.Helpers;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ChapterTagsDialogue : ContentDialog
    {
        public static ChapterTagsDialogue current;

        private Chapter _chapter;

        public ChapterTagsDialogue()
        {
            InitializeComponent();
            current = this;
            RequestedTheme = AppView.current.ActualTheme;
            AppView.currentlyOpenedDialogue = this;
            InitializeClickOutToClose();
        }

        public static void Open(Chapter chapter)
        {
            AppView.currentlyOpenedDialogue?.Hide();

            var dlg = new ChapterTagsDialogue();
            dlg._chapter = chapter;
            _ = dlg.ShowAsync();
        }

        // ─── Lifecycle ────────────────────────────────────────────────

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            chapterNameText.Text = _chapter?.Name ?? string.Empty;

            // Populate existing tags
            tagsTokenBox.Items.Clear();
            if (_chapter?.Tags != null)
                foreach (var tag in _chapter.Tags)
                    tagsTokenBox.Items.Add(tag);

            // Populate suggestion pills — presets minus already-added tags
            RefreshSuggestions();
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            Window.Current.CoreWindow.PointerPressed -= OnWindowPointerPressed;
            AppView.currentlyOpenedDialogue = null;
            current = null;
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
        }

        private void OnTokenItem_Removing(TokenizingTextBox sender, TokenItemRemovingEventArgs args)
        {
            RefreshSuggestions();
        }

        // ─── Action buttons ───────────────────────────────────────────

        private void OnSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chapter == null) return;

            var newTags = GetCurrentTags()
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            _chapter.Tags = newTags;
            TimeTravelSystem.SomethingChanged();

            Hide();
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e) => Hide();

        // ─── Click-outside-to-close ───────────────────────────────────

        private bool _isHide = true;

        private void InitializeClickOutToClose()
        {
            Window.Current.CoreWindow.PointerPressed += OnWindowPointerPressed;
            PointerExited += (s, e) => _isHide = true;
            PointerEntered += (s, e) => _isHide = false;
        }

        private void OnWindowPointerPressed(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (_isHide)
                Hide();
        }
    }
}
