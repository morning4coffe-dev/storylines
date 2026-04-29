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
            var textBox = MainPage.ChapterText?.textBox;
            if (textBox == null) return;

            var options = format == TextFormat.Rtf
                ? Windows.UI.Text.TextSetOptions.FormatRtf
                : Windows.UI.Text.TextSetOptions.None;

            textBox.Document.SetText(options, text ?? string.Empty);
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

            Interlocked.Increment(ref _programmaticChangeDepth);

            // Load the chapter's RTF content into the editor using the proper API.
            // Never manipulate RTF strings directly — the RichEditBox handles
            // paragraph marks (\par, \pard) correctly through its document model.
            try
            {
                var rtf = chapter.Text;
                if (string.IsNullOrEmpty(rtf))
                {
                    textBox.Document.SetText(Windows.UI.Text.TextSetOptions.None, string.Empty);
                }
                else
                {
                    textBox.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, rtf);
                }
            }
            finally
            {
                _ = textBox.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    RestoreChapterLocation(chapterText, chapter);

                    _ = textBox.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        if (_programmaticChangeDepth > 0)
                            Interlocked.Decrement(ref _programmaticChangeDepth);
                    });
                });
            }
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
    }
}
