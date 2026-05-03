using Storylines.Helpers;
using Storylines.Views.Controls;
using Storylines.Views.Pages;
using Storylines.Services.Interfaces;
using Storylines.Models;
using System;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Text;

namespace Storylines.Services
{
    /// <summary>
    /// Bridges the ITextEditorService interface to the actual RichEditBox
    /// and ListView UI controls. Registered after UI is ready.
    /// </summary>
    public class TextEditorService : ITextEditorService
    {
        private readonly ProjectState _projectState;

        public TextEditorService(ProjectState projectState)
        {
            _projectState = projectState;
        }

        private int _programmaticChangeDepth;

        public bool IsProgrammaticChangeInProgress => _programmaticChangeDepth > 0;

        public string GetText(TextFormat format)
        {
            var textBox = MainPage.ChapterText?.textBox;
            if (textBox == null) return string.Empty;

            var options = format == TextFormat.Rtf
                ? Windows.UI.Text.TextGetOptions.FormatRtf
                : Windows.UI.Text.TextGetOptions.None;

            textBox.Document.GetText(options, out string text);
            return text;
        }

        public void SetText(TextFormat format, string text)
        {
            var chapterText = MainPage.ChapterText;
            var textBox = chapterText?.textBox;
            if (textBox == null) return;

            var options = format == TextFormat.Rtf
                ? Windows.UI.Text.TextSetOptions.FormatRtf
                : Windows.UI.Text.TextSetOptions.None;

            ApplyEditorText(chapterText, GetSelectedChapter(), options, text ?? string.Empty, restoreChapterLocation: false);
        }

        public void Clear()
        {
            SetText(TextFormat.PlainText, string.Empty);
        }

        public void Undo()
        {
            MainPage.ChapterText?.textBox?.Document?.Undo();
        }

        public void Redo()
        {
            MainPage.ChapterText?.textBox?.Document?.Redo();
        }

        public int SelectedChapterIndex
        {
            get => MainPage.ChapterList?.ViewModel?.SelectedIndex
                ?? MainPage.ChapterList?.listView?.SelectedIndex
                ?? -1;
            set
            {
                if (MainPage.ChapterList?.ViewModel != null)
                    MainPage.ChapterList.ViewModel.SelectedIndex = value;
                else if (MainPage.ChapterList?.listView != null)
                    MainPage.ChapterList.listView.SelectedIndex = value;
            }
        }

        public int SelectedTextLength =>
            MainPage.ChapterText?.textBox?.Document?.Selection?.Text?.Length ?? 0;

        public void SetText(string rtfText)
        {
            SetText(TextFormat.Rtf, rtfText);
        }

        public void Focus()
        {
            MainPage.ChapterText?.textBox?.Focus(Windows.UI.Xaml.FocusState.Keyboard);
        }

        public void LoadChapterContent(Chapter chapter)
        {
            if (chapter == null) return;

            var chapterText = MainPage.ChapterText;
            var textBox = chapterText?.textBox;
            if (textBox == null) return;

            ApplyEditorText(chapterText, chapter, Windows.UI.Text.TextSetOptions.FormatRtf, chapter.Text ?? string.Empty, restoreChapterLocation: true);
        }

        private void ApplyEditorText(ChapterTextBox chapterText, Chapter chapter, TextSetOptions options, string sourceText, bool restoreChapterLocation)
        {
            var textBox = chapterText?.textBox;
            if (textBox == null)
                return;

            Interlocked.Increment(ref _programmaticChangeDepth);

            try
            {
                if (options == Windows.UI.Text.TextSetOptions.FormatRtf && string.IsNullOrEmpty(sourceText))
                {
                    textBox.Document.SetText(Windows.UI.Text.TextSetOptions.None, string.Empty);
                }
                else
                {
                    textBox.Document.SetText(options, sourceText);
                }

                SyncLoadedChapterText(chapter, textBox, sourceText);
            }
            finally
            {
                ScheduleProgrammaticChangeCompletion(textBox, chapterText, chapter, restoreChapterLocation);
            }
        }

        private static void SyncLoadedChapterText(Chapter chapter, MyRichEditBox textBox, string sourceText)
        {
            if (chapter == null || textBox == null)
                return;

            // RichEditBox normalizes paragraph markers as it loads content.
            // Keep the model aligned before programmatic change suppression ends,
            // otherwise the next TextChanged can look like a user edit.
            textBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out string plainText);
            textBox.Document.GetText(Windows.UI.Text.TextGetOptions.FormatRtf, out string normalizedRtf);

            var normalizedChapterText = ChapterTextNormalization.NormalizeLoadedChapterText(sourceText, plainText, normalizedRtf);
            if (chapter.Text != normalizedChapterText)
                chapter.Text = normalizedChapterText;
        }

        private void ScheduleProgrammaticChangeCompletion(MyRichEditBox textBox, ChapterTextBox chapterText, Chapter chapter, bool restoreChapterLocation)
        {
            _ = textBox.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (restoreChapterLocation)
                    RestoreChapterLocation(chapterText, chapter);

                _ = textBox.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    if (_programmaticChangeDepth > 0)
                        Interlocked.Decrement(ref _programmaticChangeDepth);
                });
            });
        }

        private Chapter GetSelectedChapter()
        {
            var selectedIndex = SelectedChapterIndex;

            if (_projectState?.Chapters == null || selectedIndex < 0 || selectedIndex >= _projectState.Chapters.Count)
                return null;

            return _projectState.Chapters[selectedIndex];
        }

        private static void RestoreChapterLocation(ChapterTextBox chapterText, Chapter chapter)
        {
            var textBox = chapterText?.textBox;
            if (textBox == null || chapter == null)
                return;

            var range = textBox.Document.GetRange(0, TextConstants.MaxUnitCount);
            var caretPosition = Math.Max(0, Math.Min(chapter.LastCaretPosition, range.EndPosition));
            textBox.Document.Selection.SetRange(caretPosition, caretPosition);

            var scrollViewer = chapterText.textBoxScrollViewer;
            if (scrollViewer == null)
                return;

            scrollViewer.UpdateLayout();

            var verticalOffset = Math.Max(0, Math.Min(chapter.LastVerticalOffset, scrollViewer.ScrollableHeight));
            _ = scrollViewer.ChangeView(null, verticalOffset, null, true);
        }

        public void SaveChapterContent(Chapter chapter)
        {
            if (chapter == null) return;

            var textBox = MainPage.ChapterText?.textBox;
            if (textBox == null) return;

            textBox.Document.GetText(Windows.UI.Text.TextGetOptions.FormatRtf, out string rtf);
            chapter.Text = rtf;
        }

        public void InsertTextAtCaret(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var textBox = MainPage.ChapterText?.textBox;
            if (textBox == null) return;

            var selection = textBox.Document.Selection;
            if (selection == null) return;

            // Replace the current selection (or insert at caret if collapsed) with the new text.
            selection.SetText(Windows.UI.Text.TextSetOptions.None, text);

            // Move caret to the end of the inserted text.
            selection.SetRange(selection.EndPosition, selection.EndPosition);
        }
    }
}
