using Storylines.Pages;
using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using Storylines.ViewModels;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Storylines.Components
{
    public sealed partial class ChaptersList : UserControl
    {
        public static int selectedIndex;

        public ChaptersListViewModel ViewModel => ServiceLocator.ChaptersListViewModel;
        private CommandBarViewModel CommandBarVM => ServiceLocator.CommandBarViewModel;

        public ObservableCollection<Chapter> Chapters => ServiceLocator.ProjectState.Chapters;

        public bool switchedChapters
        {
            get => ViewModel.SwitchedChapters;
            set => ViewModel.SwitchedChapters = value;
        }

        public bool closedManually
        {
            get => ViewModel.ClosedManually;
            set => ViewModel.ClosedManually = value;
        }

        private bool _canAdd = true;
        public bool canAdd 
        {
            set
            {
                _canAdd = value;
                ViewModel.CanAdd = value;
                CheckForEmptyList();
            } 
            get 
            { 
                return _canAdd;
            } 
        }

        public ChaptersList()
        {
            InitializeComponent();

            MainPage.ChapterList = this;
        }

        #region Flyout
        private string chapterItemFlyoutedToken;
        private void OpenFlyout(string token, bool enabled)
        {
            chapterItemFlyoutedToken = token;

            addFlyout.IsEnabled = canAdd;

            renameFlyout.IsEnabled = enabled;
            removeFlyout.IsEnabled = enabled;
        }

        private void OnFlyoutDisplayButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFlyout(listView.SelectedItem == null ? "" : (listView.SelectedItem as Chapter).Token, true);
            chaptersListViewFlyout.ShowAt((Button)sender);
        }

        private void OnChaptersListViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            OpenFlyout((sender as Grid).Tag.ToString(), true);

            chaptersListViewFlyout.ShowAt((Grid)sender, e.GetPosition((Grid)sender));
        }

        private void OnChaptersListViewItem_Holding(object sender, HoldingRoutedEventArgs e)
        {
            OpenFlyout((sender as Grid).Tag.ToString(), true);

            chaptersListViewFlyout.ShowAt((Grid)sender, e.GetPosition((Grid)sender));
        }

        private void OnChaptersListViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var ch = (sender as Grid).Tag.ToString();
            ServiceLocator.Dialogs.OpenChapterRenamer(Scripts.Services.ServiceLocator.ProjectState.FindChapter(ch), true);
        }

        private void OnChaptersListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken == null && listView.IsEnabled)
            {
                chaptersListViewFlyout.ShowAt((Grid)sender, e.GetPosition((Grid)sender));
                OpenFlyout("", false);
            }
        }

        private void ChaptersListViewItemFlyout_Closed(object sender, object e)
        {
            chapterItemFlyoutedToken = null;
        }

        private void OnChapterAdd_Click(object sender, RoutedEventArgs e)
        {
            ServiceLocator.Dialogs.OpenChapterCreator();
        }

        private void OnChapterRename_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken != null)
               ServiceLocator.Dialogs.OpenChapterRenamer(Scripts.Services.ServiceLocator.ProjectState.FindChapter(chapterItemFlyoutedToken));
        }

        private void OnChapterDeleteFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken != null)
                Scripts.Services.ServiceLocator.ProjectState.RemoveChapter(chapterItemFlyoutedToken);

            CheckForEmptyList();
        }

        private void OnChapterEditTags_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken != null)
            {
                var chapter = Scripts.Services.ServiceLocator.ProjectState.FindChapter(chapterItemFlyoutedToken);
                if (chapter != null)
                    Components.DialogueWindows.ChapterTagsDialogue.Open(chapter);
            }
        }
        #endregion

        private void OnHyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            ServiceLocator.Dialogs.OpenChapterCreator();
        }

        private void OnChaptersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedItem != null)
            {
                try
                {
                    var lastNewLine = Scripts.Services.ServiceLocator.ProjectState.Chapters[listView.SelectedIndex].Text.LastIndexOf("\\par", StringComparison.Ordinal);
                    if (lastNewLine >= 0)
                        Scripts.Services.ServiceLocator.ProjectState.Chapters[listView.SelectedIndex].Text = Scripts.Services.ServiceLocator.ProjectState.Chapters[listView.SelectedIndex].Text.Remove(lastNewLine, "\\par".Length);
                }
                catch (Exception ex)
                {
                    Scripts.Services.ServiceLocator.Logger?.Warning($"Failed to trim trailing paragraph mark: {ex.Message}");
                }
                if (!reordering)
                    switchedChapters = true;
                selectedIndex = listView.SelectedIndex;

                ServiceLocator.TextEditor.SetText(
                    Scripts.Services.ServiceLocator.ProjectState.FindChapter((listView.SelectedItem as Chapter).Token).Text ?? string.Empty);

                MainPage.ChapterText.ChangeTextColor();
                ServiceLocator.Events.Publish(new ChapterToolsStateEvent { Enabled = true });
                MainPage.ChapterText.CheckForFormatting();

                ServiceLocator.TextEditor.Focus();

                ServiceLocator.Events.Publish(new RefreshNotesPaneEvent());
            }
            else
                ServiceLocator.Events.Publish(new ChapterToolsStateEvent { Enabled = false });

            CheckForEmptyList();
        }

        public void CheckForEmptyList()
        {
            ViewModel.UpdateListState();
            noChaptersPlaceholder.Visibility = listView.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            CommandBarVM.IsExportEnabled = ViewModel.IsExportEnabled;
            CommandBarVM.IsSaveEnabled = ViewModel.IsSaveEnabled;
            CommandBarVM.IsSaveCopyEnabled = ViewModel.IsSaveCopyEnabled;
            CommandBarVM.IsChapterAddEnabled = ViewModel.IsAddButtonEnabled;
        }

        #region Reorder Items
        private bool reordering = false;
        private int position;

        private void OnChaptersListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            reordering = true;
            position = listView.Items.IndexOf(e.Items[0]);
        }

        private void OnChaptersListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            reordering = false;

            Scripts.Services.ServiceLocator.ProjectState.ReorderChapter((args.Items[0] as Chapter).Token, listView.Items.IndexOf(args.Items[0]), position);
        }
        #endregion

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceLocator.Events.Publish(new ToggleChapterListEvent { Open = false, Manually = true });
        }
    }
}
