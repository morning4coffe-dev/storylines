using Storylines.Views.Pages;
using Storylines.Services;
using Storylines.Models;
using Storylines.ViewModels;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Storylines.Views.Dialogs;
using Storylines.Helpers;
using Storylines.Services.Interfaces;

namespace Storylines.Views.Controls
{
    public sealed partial class ChaptersList : UserControl
    {
        public static int selectedIndex;

        private readonly IDialogService _dialogs;
        private readonly EventAggregator _events;
        private readonly ILogger _logger;
        private readonly ProjectState _projectState;
        private readonly ITextEditorService _textEditor;
        private readonly ChaptersListViewModel _viewModel;
        private readonly CommandBarViewModel _commandBarViewModel;

        public ChaptersListViewModel ViewModel => _viewModel;
        private CommandBarViewModel CommandBarVM => _commandBarViewModel;

        public ObservableCollection<Chapter> Chapters => _projectState.Chapters;

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

            _dialogs = App.GetService<IDialogService>();
            _events = App.GetService<EventAggregator>();
            _logger = App.GetService<ILogger>();
            _projectState = App.GetService<ProjectState>();
            _textEditor = App.GetService<ITextEditorService>();
            _viewModel = App.GetService<ChaptersListViewModel>();
            _commandBarViewModel = App.GetService<CommandBarViewModel>();

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
            _dialogs.OpenChapterRenamer(_projectState.FindChapter(ch), true);
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
            _dialogs.OpenChapterCreator();
        }

        private void OnChapterRename_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken != null)
             _dialogs.OpenChapterRenamer(_projectState.FindChapter(chapterItemFlyoutedToken));
        }

        private void OnChapterDeleteFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken != null)
                _projectState.RemoveChapter(chapterItemFlyoutedToken);

            CheckForEmptyList();
        }

        private void OnChapterEditTags_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken != null)
            {
                var chapter = _projectState.FindChapter(chapterItemFlyoutedToken);
                if (chapter != null)
                    ChapterTagsDialogue.Open(chapter);
            }
        }

        private void OnSetStatus_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken != null && sender is MenuFlyoutItem item)
            {
                var chapter = _projectState.FindChapter(chapterItemFlyoutedToken);
                if (chapter != null && System.Enum.TryParse<ChapterStatus>(item.Tag?.ToString(), out var status))
                {
                    chapter.Status = status;
                    TimeTravelSystem.SomethingChanged();
                }
            }
        }
        #endregion

        private void OnHyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            _dialogs.OpenChapterCreator();
        }

        private void OnChaptersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedItem != null)
            {
                try
                {
                    var lastNewLine = _projectState.Chapters[listView.SelectedIndex].Text.LastIndexOf("\\par", StringComparison.Ordinal);
                    if (lastNewLine >= 0)
                        _projectState.Chapters[listView.SelectedIndex].Text = _projectState.Chapters[listView.SelectedIndex].Text.Remove(lastNewLine, "\\par".Length);
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Failed to trim trailing paragraph mark: {ex.Message}");
                }
                if (!reordering)
                    switchedChapters = true;
                selectedIndex = listView.SelectedIndex;

                _textEditor.SetText(
                    _projectState.FindChapter((listView.SelectedItem as Chapter).Token).Text ?? string.Empty);

                MainPage.ChapterText.ChangeTextColor();
                _events.Publish(new ChapterToolsStateEvent { Enabled = true });
                MainPage.ChapterText.CheckForFormatting();

                _textEditor.Focus();

                _events.Publish(new RefreshNotesPaneEvent());
            }
            else
                _events.Publish(new ChapterToolsStateEvent { Enabled = false });

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

            _projectState.ReorderChapter((args.Items[0] as Chapter).Token, listView.Items.IndexOf(args.Items[0]), position);
        }
        #endregion

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            _events.Publish(new ToggleChapterListEvent { Open = false, Manually = true });
        }
    }
}
