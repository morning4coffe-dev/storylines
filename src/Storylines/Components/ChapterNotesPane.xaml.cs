using Storylines.Pages;
using Storylines.Scripts.Variables;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components
{
    public sealed partial class ChapterNotesPane : UserControl
    {
        private bool _isUpdating;

        public ChapterNotesPane()
        {
            InitializeComponent();
        }

        public void LoadNotes()
        {
            _isUpdating = true;

            if (MainPage.ChapterList?.listView?.SelectedItem is Chapter chapter)
            {
                notesTextBox.Text = chapter.notes ?? string.Empty;
                notesTextBox.IsEnabled = true;
            }
            else
            {
                notesTextBox.Text = string.Empty;
                notesTextBox.IsEnabled = false;
            }

            _isUpdating = false;
        }

        private void OnNotesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (MainPage.ChapterList?.listView?.SelectedItem is Chapter chapter)
            {
                chapter.notes = notesTextBox.Text;
                Scripts.Functions.TimeTravelSystem.SomethingChanged();
            }
        }

        private void OnCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Current.ToggleNotesPane(false);
        }
    }
}
