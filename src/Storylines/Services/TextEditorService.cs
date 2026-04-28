using Storylines.Views.Controls;
using Storylines.Views.Pages;
using Storylines.Services.Interfaces;
using Storylines.Models;

namespace Storylines.Services
{
    /// <summary>
    /// Bridges the ITextEditorService interface to the actual RichEditBox
    /// and ListView UI controls. Registered after UI is ready.
    /// </summary>
    public class TextEditorService : ITextEditorService
    {
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

            var textBox = MainPage.ChapterText?.textBox;
            if (textBox == null) return;

            // Load the chapter's RTF content into the editor using the proper API.
            // Never manipulate RTF strings directly — the RichEditBox handles
            // paragraph marks (\par, \pard) correctly through its document model.
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
