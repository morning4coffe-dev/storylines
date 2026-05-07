using Storylines.Views.Pages;
using Storylines.Models;
using Storylines.Services;
using Storylines.ViewModels;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Storylines.Views.Controls
{
    public sealed partial class ChaptersList : UserControl
    {
        private readonly ChaptersListViewModel _viewModel;
        private readonly WindowContext _windowContext;

        public ChaptersListViewModel ViewModel => _viewModel;

        public bool closedManually
        {
            get => ViewModel.ClosedManually;
            set => ViewModel.ClosedManually = value;
        }

        public bool canAdd 
        {
            get => ViewModel.CanAdd;
            set => ViewModel.CanAdd = value;
        }

        public ChaptersList()
        {
            InitializeComponent();

            _windowContext = App.GetService<WindowContext>();
            _viewModel = App.GetService<ChaptersListViewModel>();

            _windowContext.ChapterList = this;
        }

        #region Flyout
        private string chapterItemFlyoutedToken;
        private void OpenFlyout(string token, bool enabled)
        {
            chapterItemFlyoutedToken = token;

            addFlyout.IsEnabled = ViewModel.IsAddButtonEnabled;

            renameFlyout.IsEnabled = enabled;
            duplicateFlyout.IsEnabled = enabled;
            removeFlyout.IsEnabled = enabled;
            editTagsFlyout.IsEnabled = enabled;
            setStatusFlyout.IsEnabled = enabled;
        }

        private void OnFlyoutDisplayButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFlyout(ViewModel.SelectedChapter?.Token ?? string.Empty, ViewModel.SelectedChapter is not null);
            chaptersListViewFlyout.ShowAt((Button)sender);
        }

        private void OnChaptersListViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            OpenFlyout((sender as Grid).Tag.ToString(), true);

            chaptersListViewFlyout.ShowAt((Grid)sender, e.GetPosition((Grid)sender));
            e.Handled = true;
        }

        private void OnChaptersListViewItem_Holding(object sender, HoldingRoutedEventArgs e)
        {
            OpenFlyout((sender as Grid).Tag.ToString(), true);

            chaptersListViewFlyout.ShowAt((Grid)sender, e.GetPosition((Grid)sender));
            e.Handled = true;
        }

        private void OnChaptersListViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ViewModel.OpenRenameChapterDialog((sender as Grid)?.Tag?.ToString(), true);
        }

        private void OnChaptersListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken is null && listView.IsEnabled)
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
            ViewModel.OpenCreateChapterDialogCommand.Execute(null);
        }

        private void OnChapterRename_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenRenameChapterDialog(chapterItemFlyoutedToken);
        }

        private void OnChapterDeleteFlyout_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteChapter(chapterItemFlyoutedToken);
        }

        private void OnChapterDuplicate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DuplicateChapter(chapterItemFlyoutedToken);
        }

        private void OnChapterEditTags_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenChapterTagsDialog(chapterItemFlyoutedToken);
        }

        private void OnSetStatus_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken is not null && sender is MenuFlyoutItem item)
            {
                if (System.Enum.TryParse<ChapterStatus>(item.Tag?.ToString(), out var status))
                    ViewModel.SetChapterStatus(chapterItemFlyoutedToken, status);
            }
        }
        #endregion

        private void OnHyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            ViewModel.OpenCreateChapterDialogCommand.Execute(null);
        }

        #region Reorder Items
        private int position;

        private void OnChaptersListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            position = listView.Items.IndexOf(e.Items[0]);
        }

        private void OnChaptersListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            ViewModel.ReorderChapter((args.Items[0] as Chapter)?.Token, listView.Items.IndexOf(args.Items[0]), position);
        }
        #endregion
    }
}
