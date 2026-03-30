using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Pages
{
    public sealed partial class StoryPinboardPage : Page
    {
        private int _dragStartPosition;

        public StoryPinboardPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            AppView.current.page = AppView.Pages.MainPage; // stays as MainPage context so back button works

            var chapters = ServiceLocator.ProjectState.Chapters;
            pinboardList.ItemsSource = chapters;

            subtitleText.Text = $"{chapters.Count} chapter{(chapters.Count == 1 ? "" : "s")}";
            emptyState.Visibility = chapters.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ─── Back navigation ──────────────────────────────────────────

        private void OnBackButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceLocator.Navigation.GoBack();
        }

        // ─── Drag-to-reorder ──────────────────────────────────────────

        private void OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            _dragStartPosition = pinboardList.Items.IndexOf(e.Items[0]);
        }

        private void OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (args.Items.Count == 0) return;

            int newPos = pinboardList.Items.IndexOf(args.Items[0]);
            if (newPos != _dragStartPosition)
            {
                ServiceLocator.ProjectState.ReorderChapter(
                    (args.Items[0] as Chapter).Token,
                    newPos,
                    _dragStartPosition);

                Scripts.Functions.TimeTravelSystem.SomethingChanged();
            }
        }
    }
}
