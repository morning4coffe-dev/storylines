using Storylines.Components;
using Storylines.Pages;
using Storylines.Scripts.Services.Interfaces;

namespace Storylines.Scripts.Services
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
            get => MainPage.ChapterList?.listView?.SelectedIndex ?? -1;
            set
            {
                if (MainPage.ChapterList?.listView != null)
                    MainPage.ChapterList.listView.SelectedIndex = value;
            }
        }

        public void SetText(string rtfText)
        {
            SetText(TextFormat.Rtf, rtfText);
        }

        public void Focus()
        {
            MainPage.ChapterText?.textBox?.Focus(Windows.UI.Xaml.FocusState.Keyboard);
        }
    }
}
