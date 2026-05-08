using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Storylines.Models.DialogueScript;
using Windows.System;

namespace Storylines.Views.Controls.DialogueTextEditor
{
    /// <summary>
    /// Plain-TextBox dialogue script editor with a suggestion popup.
    /// Trigger detection is delegated to <see cref="SuggestionPopupManager"/> (pure logic, separately tested).
    /// The host page supplies suggestion items via the <see cref="SuggestionRequested"/> event, so the
    /// control itself stays free of plugin types and project-specific state.
    /// </summary>
    public sealed partial class DialogueTextEditor : UserControl
    {
        private readonly SuggestionPopupManager _suggestions = new SuggestionPopupManager();
        private SuggestionPopupResult _activeTrigger = SuggestionPopupResult.None;
        private bool _isUpdatingText;

        public DialogueTextEditor()
        {
            InitializeComponent();

            PART_TextBox.TextChanged += OnTextChanged;
            PART_TextBox.SelectionChanged += OnSelectionChanged;
            PART_TextBox.KeyDown += OnTextBoxKeyDown;
            PART_SuggestionList.ItemClick += OnSuggestionItemClick;
            PART_SuggestionList.IsItemClickEnabled = true;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(DialogueTextEditor),
            new PropertyMetadata(string.Empty, OnTextDpChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>Raised when the caret enters a context that could show suggestions.
        /// Listeners populate <see cref="SuggestionRequestedEventArgs.Items"/> with filtered candidates.</summary>
        public event EventHandler<SuggestionRequestedEventArgs> SuggestionRequested;

        // -------------------------------------------------------------------------
        // Text-binding plumbing
        // -------------------------------------------------------------------------

        private static void OnTextDpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DialogueTextEditor self
                && !self._isUpdatingText
                && e.NewValue is string newText
                && self.PART_TextBox.Text != newText)
            {
                self._isUpdatingText = true;
                try { self.PART_TextBox.Text = newText ?? string.Empty; }
                finally { self._isUpdatingText = false; }
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText) return;

            _isUpdatingText = true;
            try { Text = PART_TextBox.Text; }
            finally { _isUpdatingText = false; }

            EvaluateSuggestionTrigger();
        }

        private void OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            EvaluateSuggestionTrigger();
        }

        // -------------------------------------------------------------------------
        // Suggestion popup
        // -------------------------------------------------------------------------

        private void EvaluateSuggestionTrigger()
        {
            // Only show when selection is collapsed (caret only).
            if (PART_TextBox.SelectionLength != 0)
            {
                HidePopup();
                return;
            }

            var caret = PART_TextBox.SelectionStart;
            var result = _suggestions.Analyze(PART_TextBox.Text ?? string.Empty, caret);

            if (!result.ShouldShow)
            {
                HidePopup();
                return;
            }

            _activeTrigger = result;

            var args = new SuggestionRequestedEventArgs(result.TriggerType, result.FilterText);
            SuggestionRequested?.Invoke(this, args);

            if (args.Items == null || args.Items.Count == 0)
            {
                HidePopup();
                return;
            }

            ShowPopup(args.Items);
        }

        private void ShowPopup(IList<string> items)
        {
            PART_SuggestionList.ItemsSource = items;
            if (items.Count > 0)
                PART_SuggestionList.SelectedIndex = 0;

            PART_Popup.IsOpen = true;
        }

        private void HidePopup()
        {
            if (PART_Popup.IsOpen)
                PART_Popup.IsOpen = false;
            _activeTrigger = SuggestionPopupResult.None;
        }

        private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!PART_Popup.IsOpen)
                return;

            switch (e.Key)
            {
                case VirtualKey.Escape:
                    HidePopup();
                    e.Handled = true;
                    break;

                case VirtualKey.Down:
                    if (PART_SuggestionList.SelectedIndex < PART_SuggestionList.Items.Count - 1)
                        PART_SuggestionList.SelectedIndex++;
                    e.Handled = true;
                    break;

                case VirtualKey.Up:
                    if (PART_SuggestionList.SelectedIndex > 0)
                        PART_SuggestionList.SelectedIndex--;
                    e.Handled = true;
                    break;

                case VirtualKey.Enter:
                case VirtualKey.Tab:
                    CommitSelection();
                    e.Handled = true;
                    break;
            }
        }

        private void OnSuggestionItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is string s)
            {
                PART_SuggestionList.SelectedItem = s;
                CommitSelection();
            }
        }

        private void CommitSelection()
        {
            if (_activeTrigger == null
                || !_activeTrigger.ShouldShow
                || PART_SuggestionList.SelectedItem is not string selected)
            {
                HidePopup();
                return;
            }

            var text = PART_TextBox.Text ?? string.Empty;
            var start = _activeTrigger.TriggerStart;
            var end = _activeTrigger.TriggerEnd;

            if (start < 0 || end > text.Length || start > end)
            {
                HidePopup();
                return;
            }

            var newText = text.Substring(0, start) + selected + text.Substring(end);
            var newCaret = start + selected.Length;

            _isUpdatingText = true;
            try
            {
                PART_TextBox.Text = newText;
                Text = newText;
                PART_TextBox.SelectionStart = newCaret;
                PART_TextBox.SelectionLength = 0;
            }
            finally { _isUpdatingText = false; }

            HidePopup();
        }
    }

    public sealed class SuggestionRequestedEventArgs : EventArgs
    {
        public SuggestionRequestedEventArgs(SuggestionTriggerType triggerType, string filterText)
        {
            TriggerType = triggerType;
            FilterText = filterText ?? string.Empty;
        }

        public SuggestionTriggerType TriggerType { get; }
        public string FilterText { get; }

        /// <summary>Listeners assign the filtered list of suggestion strings to display.</summary>
        public IList<string> Items { get; set; }
    }
}
