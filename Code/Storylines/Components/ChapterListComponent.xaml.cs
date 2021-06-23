using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Storylines.Components
{
    public sealed partial class ChapterListComponent : UserControl
    {
        public List<Chapter> chapters = new List<Chapter>();

        public bool switchedChapters = false;

        public ChapterListComponent()
        {
            this.InitializeComponent();

            MainPage.chapterList = this;
        }

        #region ToolBar
        private void OnChapterAdd_Click(object sender, RoutedEventArgs e)
        {
            _ = ChapterCreatorOrRenamer.Open(null);
        }

        private void OnChapterDelete_Click(object sender, RoutedEventArgs e)
        {
            if (chaptersListView.SelectedItem != null)
            {
                Chapter.Remove((chaptersListView.SelectedItem as ListViewItem).Name);
            }
        }
        #endregion

        #region Flyout
        public ListViewItemPresenter chapterItemFlyouted;

        private void OnChaptersListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var ch = (FrameworkElement)e.OriginalSource;

            if (ch as TextBlock != null)
            {
                ch = VisualTreeHelper.GetParent(ch) as ListViewItemPresenter;
            }
            chapterItemFlyouted = (ch as ListViewItemPresenter);

            if (ch as ListViewItemPresenter != null)
            {
                chaptersListViewItemFlyout.ShowAt((ListView)sender, e.GetPosition((ListView)sender));
            }
            else
            {
                chaptersListViewFlyout.ShowAt((ListView)sender, e.GetPosition((ListView)sender));
            }
        }

        private void OnChapterEditName_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyouted != null)
            {
                var item = VisualTreeHelper.GetParent(chapterItemFlyouted) as ListViewItem;
                _ = ChapterCreatorOrRenamer.Open(Chapter.Find(item.Name));
            }
        }

        private void OnChapterDeleteFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (chapterItemFlyouted != null)
            {
                Chapter.Remove((VisualTreeHelper.GetParent(chapterItemFlyouted) as ListViewItem).Name);
            }
        }
        #endregion

        private void OnChaptersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!reordering)
            {
                if (chaptersListView.SelectedItem != null)
                {
                    try
                    {
                        var lastNewLine = chapters[chaptersListView.SelectedIndex].text.LastIndexOf("\\par", StringComparison.Ordinal);
                        chapters[chaptersListView.SelectedIndex].text = chapters[chaptersListView.SelectedIndex].text.Remove(lastNewLine, "\\par".Length);
                    }
                    catch { }

                    switchedChapters = MainPage.mainPage.unSavedProgress != true;
                    MainPage.chapterText.textBox.Document.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, Chapter.Find((chaptersListView.SelectedItem as ListViewItem).Name).text ?? string.Empty);

                    MainPage.chapterText.ChangeTextColor();
                    MainPage.mainPage.EnableOrDisableChapterTools(true);
                    MainPage.chapterText.CheckForFormatting();
                }
                else
                {
                    MainPage.mainPage.EnableOrDisableChapterTools(false);
                }
            }
        }


        public void OnChapterListComponent_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (chaptersListView.SelectedItem != null)
            {
                if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (e.Key == VirtualKey.PageUp)
                    {
                        if (chaptersListView.SelectedIndex > 0)
                        {
                            chaptersListView.SelectedIndex -= 1;
                        }
                    }
                    else
                    if (e.Key == VirtualKey.PageDown)
                    {
                        if (chaptersListView.SelectedIndex >= 0 && chaptersListView.SelectedIndex < (chaptersListView.Items.Count - 1))
                        {
                            chaptersListView.SelectedIndex += 1;
                        }
                        else
                        if (chaptersListView.Items.Count == chaptersListView.SelectedIndex + 1 && SettingsPage.isOnPageDownNewChapterEnabled)
                        {
                            Chapter.Add($"Chapter {MainPage.chapterList.chapters.Count + 1}: The one with no name");
                            chaptersListView.SelectedIndex += 1;
                        }
                    }
                }
            }
            else
            {
                if (chapters.Count > 0 && Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down) && (e.Key == VirtualKey.PageUp || e.Key == VirtualKey.PageDown))
                {
                    chaptersListView.SelectedIndex = 0;
                }
            }
        }

        #region Reorder Items
        private bool reordering = false;

        private void OnChaptersListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            reordering = true;
        }

        private void OnChaptersListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            reordering = false;

            for (int i = 0; i < chaptersListView.Items.Count; i++)
            {
                Chapter.Reorder((chaptersListView.Items[i] as ListViewItem).Name, i);
            }
        }
        #endregion
    }
}
