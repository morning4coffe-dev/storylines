using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Storylines.ViewModels.Dialogue;
using Storylines.Models.Dialogue;
using Windows.UI;

namespace Storylines.Views.Controls.DialogueEditor
{
    public sealed partial class BranchingDialogueEditor : UserControl
    {
        public BranchingDialogueEditorViewModel ViewModel { get; } = new BranchingDialogueEditorViewModel();
        private readonly Storylines.Services.WindowContext _windowContext;

        private string _currentSearchQuery = string.Empty;
        private int _mentionStartIndex = -1;

        public BranchingDialogueEditor()
        {
            this.InitializeComponent();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            try { _windowContext = App.GetService<Storylines.Services.WindowContext>(); } catch {}
            if (_windowContext != null) _windowContext.ChapterText = this;
        }



        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadContentForSelectedNode();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BranchingDialogueEditorViewModel.SelectedNode))
            {
                LoadContentForSelectedNode();
            }
        }

        private void LoadContentForSelectedNode()
        {
            if (ViewModel.SelectedNode == null)
            {
                EditorBox.Document.SetText(TextSetOptions.None, string.Empty);
                return;
            }

            if (!string.IsNullOrEmpty(ViewModel.SelectedNode.Node.ContentRtf))
            {
                EditorBox.Document.SetText(TextSetOptions.FormatRtf, ViewModel.SelectedNode.Node.ContentRtf);
            }
            else
            {
                EditorBox.Document.SetText(TextSetOptions.None, ViewModel.SelectedNode.Node.ContentPlainText ?? string.Empty);
            }
        }

        private void SaveContentToSelectedNode()
        {
            if (ViewModel.SelectedNode == null) return;

            EditorBox.Document.GetText(TextGetOptions.FormatRtf, out string rtf);
            EditorBox.Document.GetText(TextGetOptions.None, out string plain);

            ViewModel.SelectedNode.Node.ContentRtf = rtf;
            ViewModel.SelectedNode.Node.ContentPlainText = plain;
        }

        private void EditorBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveContentToSelectedNode();
        }

        private void EditorBox_TextChanging(RichEditBox sender, RichEditBoxTextChangingEventArgs args)
        {
            sender.Document.Selection.GetText(TextGetOptions.None, out string selectionText);

            // basic check for "@"
            sender.Document.GetText(TextGetOptions.None, out string fullText);
            int caretPos = sender.Document.Selection.StartPosition;

            if (caretPos > 0)
            {
                string textBeforeCaret = fullText.Substring(0, caretPos);
                int lastAt = textBeforeCaret.LastIndexOf('@');
                int lastSpace = textBeforeCaret.LastIndexOf(' ');

                if (lastAt >= 0 && lastAt >= lastSpace)
                {
                    _mentionStartIndex = lastAt;
                    _currentSearchQuery = textBeforeCaret.Substring(lastAt + 1);

                    // Show popup
                    MentionsPopup.IsOpen = true;
                    // Optional: filter MentionsList based on _currentSearchQuery
                }
                else
                {
                    MentionsPopup.IsOpen = false;
                    _mentionStartIndex = -1;
                }
            }
            else
            {
                MentionsPopup.IsOpen = false;
            }
        }

        private void MentionsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TagItem tagItem && _mentionStartIndex != -1)
            {
                var selection = EditorBox.Document.Selection;
                int currentPos = selection.StartPosition;

                // Select the @... text
                selection.SetRange(_mentionStartIndex, currentPos);

                // Replace it with formatted tag
                selection.Text = $"@{tagItem.Name} ";

                // Apply formatting to the inserted tag
                var formatRange = EditorBox.Document.GetRange(_mentionStartIndex, _mentionStartIndex + tagItem.Name.Length + 1);
                var charFormat = formatRange.CharacterFormat;

                // Use a highlighted accent color (e.g. Blue or Theme accent) and underline
                charFormat.ForegroundColor = Color.FromArgb(255, 0, 120, 215); // Default blue accent
                charFormat.Underline = UnderlineType.Single;

                // Reset formatting for next input
                selection.SetRange(_mentionStartIndex + tagItem.Name.Length + 2, _mentionStartIndex + tagItem.Name.Length + 2);
                var resetFormat = selection.CharacterFormat;
                resetFormat.ForegroundColor = Color.FromArgb(255, 0, 0, 0); // Need to get actual theme text color later
                resetFormat.Underline = UnderlineType.None;

                MentionsPopup.IsOpen = false;
                _mentionStartIndex = -1;
                EditorBox.Focus(FocusState.Programmatic);

                SaveContentToSelectedNode();
            }
        }

        private void PreviewJson_Click(object sender, RoutedEventArgs e)
        {
            SaveContentToSelectedNode();
            var json = ViewModel.GetExportJson();
            ShowPreviewDialog("JSON Export", json);
        }

        private void PreviewText_Click(object sender, RoutedEventArgs e)
        {
            SaveContentToSelectedNode();
            var text = ViewModel.GetExportPlainText();
            ShowPreviewDialog("Plain Text Export", text);
        }

        private async void ShowPreviewDialog(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new ScrollViewer
                {
                    Content = new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap }
                },
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        // Required API matches for replacing ChapterTextBox functionality
        public void BoldChapterTextBox()
        {
            EditorBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
        }

        public void ItalicChapterTextBox()
        {
            EditorBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
        }

        public void UnderlineChapterTextBox()
        {
            EditorBox.Document.Selection.CharacterFormat.Underline =
                EditorBox.Document.Selection.CharacterFormat.Underline == UnderlineType.Single ? UnderlineType.None : UnderlineType.Single;
        }

        public void StrikethroughChapterTextBox()
        {
            EditorBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;
        }


        public static readonly DependencyProperty FormattingBarVisibilityProperty =
            DependencyProperty.Register("FormattingBarVisibility", typeof(Visibility), typeof(BranchingDialogueEditor), new PropertyMetadata(Visibility.Collapsed));

        public Visibility FormattingBarVisibility
        {
            get => (Visibility)GetValue(FormattingBarVisibilityProperty);
            set => SetValue(FormattingBarVisibilityProperty, value);
        }


        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(BranchingDialogueEditor), new PropertyMetadata(false));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

                public void SetSelectedChapterIndex(int index) { }

        public RichEditBox textBox => EditorBox;

    }
}
