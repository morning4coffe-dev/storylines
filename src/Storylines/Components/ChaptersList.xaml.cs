using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Variables;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Storylines.Components
{
    public sealed partial class ChaptersList : UserControl
    {
        public static int selectedIndex;

        public bool switchedChapters = false;

        public bool closedManually;

        private bool _canAdd = true;
        public bool canAdd 
        {
            set
            { 
                _canAdd = value;
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
            OpenFlyout(listView.SelectedItem == null ? "" : (listView.SelectedItem as Chapter).token, true);
            chaptersListViewFlyout.ShowAt((Button)sender);
        }

        private void OnChaptersListViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            OpenFlyout((sender as Grid).Tag.ToString(), true);

            //var s = (FrameworkElement)sender;
            //var d = s.DataContext;
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
            ChapterCreatorOrRenamer.Open(Chapter.Find(ch), true);
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
            ChapterCreatorOrRenamer.Open(null, false);
        }

        private void OnChapterRename_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken != null)
               ChapterCreatorOrRenamer.Open(Chapter.Find(chapterItemFlyoutedToken), false);
        }

        private void OnChapterDeleteFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyoutedToken != null)
                Chapter.Remove(chapterItemFlyoutedToken);

            CheckForEmptyList();
        }
        #endregion

        private void OnHyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            ChapterCreatorOrRenamer.Open(null, false);
        }

        private void OnChaptersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedItem != null)
            {
                try
                {
                    var lastNewLine = Chapter.chapters[listView.SelectedIndex].text.LastIndexOf("\\par", StringComparison.Ordinal);
                    Chapter.chapters[listView.SelectedIndex].text = Chapter.chapters[listView.SelectedIndex].text.Remove(lastNewLine, "\\par".Length);
                }
                catch { }
                if (!reordering)
                    switchedChapters = true;
                selectedIndex = listView.SelectedIndex;

                MainPage.ChapterText.textBox.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, Chapter.Find((listView.SelectedItem as Chapter).token).text ?? string.Empty);

                MainPage.ChapterText.ChangeTextColor();
                MainPage.Current.EnableOrDisableChapterTools(true);
                MainPage.ChapterText.CheckForFormatting();

                _ = MainPage.ChapterText.textBox.Focus(FocusState.Keyboard);
            }
            else
                MainPage.Current.EnableOrDisableChapterTools(false);

            CheckForEmptyList();
        }

        public void CheckForEmptyList()
        { 
            noChaptersPlaceholder.Visibility = listView.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            MainPage.CommandBar.exportButton.IsEnabled = Chapter.chapters.Count > 0 || Character.characters.Count > 0;
            MainPage.CommandBar.saveButton.IsEnabled = Chapter.chapters.Count > 0;
            MainPage.CommandBar.saveCopyButton.IsEnabled = Chapter.chapters.Count > 0;
            MainPage.CommandBar.chapterAddButton.IsEnabled = _canAdd;
        }

        #region Reorder Items
        private bool reordering = false;
        private int position;

        private void OnChaptersListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            reordering = true;
            position = MainPage.ChapterList.listView.Items.IndexOf(e.Items[0]);
        }

        private void OnChaptersListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            reordering = false;

            Chapter.Reorder((args.Items[0] as Chapter).token, MainPage.ChapterList.listView.Items.IndexOf(args.Items[0]), position);
        }
        #endregion

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Current.OpenOrCloseChapterList(false, true);
        }
    }
}
