using Storylines.Views.Pages;
using Storylines.Models;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Storylines.Helpers;
using Storylines.Services;

namespace Storylines.Views.Controls
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
                notesTextBox.Text = chapter.Notes ?? string.Empty;
                notesTextBox.IsEnabled = true;

                synopsisTextBox.Text = chapter.Synopsis ?? string.Empty;
                synopsisTextBox.IsEnabled = true;

                locationTextBox.Text = chapter.Location ?? string.Empty;
                locationTextBox.IsEnabled = true;

                plotThreadsTextBox.Text = chapter.PlotThreads?.Count > 0 ? string.Join(", ", chapter.PlotThreads) : string.Empty;
                plotThreadsTextBox.IsEnabled = true;
            }
            else
            {
                notesTextBox.Text = string.Empty;
                notesTextBox.IsEnabled = false;

                synopsisTextBox.Text = string.Empty;
                synopsisTextBox.IsEnabled = false;

                locationTextBox.Text = string.Empty;
                locationTextBox.IsEnabled = false;

                plotThreadsTextBox.Text = string.Empty;
                plotThreadsTextBox.IsEnabled = false;
            }

            _isUpdating = false;
        }

        private void OnNotesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (MainPage.ChapterList?.listView?.SelectedItem is Chapter chapter)
            {
                chapter.Notes = notesTextBox.Text;
                TimeTravelSystem.SomethingChanged();
            }
        }

        private void OnSynopsisTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (MainPage.ChapterList?.listView?.SelectedItem is Chapter chapter)
            {
                chapter.Synopsis = synopsisTextBox.Text;
                TimeTravelSystem.SomethingChanged();
            }
        }

        private void OnLocationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (MainPage.ChapterList?.listView?.SelectedItem is Chapter chapter)
            {
                chapter.Location = locationTextBox.Text;
                TimeTravelSystem.SomethingChanged();
            }
        }

        private void OnPlotThreadsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (MainPage.ChapterList?.listView?.SelectedItem is Chapter chapter)
            {
                chapter.PlotThreads = (plotThreadsTextBox.Text ?? string.Empty)
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(System.StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                // Auto-register new plot threads in the project
                foreach (var thread in chapter.PlotThreads)
                {
                    if (!App.GetService<ProjectState>().PlotThreads.Contains(thread, System.StringComparer.CurrentCultureIgnoreCase))
                        App.GetService<ProjectState>().PlotThreads.Add(thread);
                }

                TimeTravelSystem.SomethingChanged();
            }
        }

        private void OnCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Current.ToggleNotesPane(false);
        }
    }
}
