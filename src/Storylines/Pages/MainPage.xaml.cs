using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Scripts.Modes;
using Storylines.Scripts.Services;
using System;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Storylines.Pages
{
    public sealed partial class MainPage : Page
    {
        public static MainPage current { get; private set; }

        public static ChaptersList chapterList;
        public static MainCommandBar commandBar;
        public static ChapterTextBox chapterText;

        public static FocusMode focusMode;
        public static ReadMode readMode;

        public MainPage()
        {
            InitializeComponent();
            current = this;

            AppView.current.page = AppView.Pages.MainPage;

            SizeChanged();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.item != null)
            {
                SaveSystem.DefaultLaunch(App.item);
                App.item = null;
            }

            if (chapterList.listView.Items.Count > 0 && ChaptersList.selectedIndex <= chapterList.listView.Items.Count)
                chapterList.listView.SelectedIndex = ChaptersList.selectedIndex;
            chapterText.TextBoxWhiteBackground(Convert.ToBoolean(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.TextBoxSolidBackground] ?? false));

            LoadTextBoxZoom();

            if (SaveSystem.currentProject != null && SaveSystem.currentProject.file != null)
                EnableOrDisableToolsForStorylinesDocuments(SaveSystem.currentProject.file.FileType.Contains(".srl"));
        }

        public void EnableOrDisableChapterTools(bool enable)
        {
            chapterText.textBox.IsTabStop = enable;
            chapterText.textBoxRectangle.IsHitTestVisible = !enable;
            chapterText.textBox.IsHitTestVisible = enable;
            textBoxZoomSlider.IsEnabled = enable;
            textBoxZoomTextHyperlink.IsEnabled = enable;

            if (enable)
            {
                chapterText.textBoxRectangle.Visibility = Visibility.Collapsed;
                UpdateDownBar();
            }
            else
            {
                AppView.current.Focus(FocusState.Keyboard);
                chapterText.textBoxRectangle.Visibility = Visibility.Visible;
                downBarText.Text = ResourceLoader.GetForCurrentView().GetString("downBarTextS");
            }
        }

        public void EnableOrDisableToolsForStorylinesDocuments(bool enable)
        {
            chapterList.canAdd = enable;
            chapterList.listView.IsEnabled = enable;

            commandBar.exportButton.IsEnabled = enable;
            commandBar.charactersButton.IsEnabled = enable;

            chapterText.chapterTextCommandBar.IsEnabled = enable;
        }

        private void OnPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeChanged();
        }

        public new void SizeChanged()
        {
            if (ActualWidth < 800)
            {
                OpenOrCloseChapterList(false, false);
                chapterTextBoxMainPage.Margin = new Thickness(8);
                textBoxZoomSliderZoomText.Visibility = Visibility.Collapsed;
                textBoxZoomSlider.Visibility = Visibility.Collapsed;
            }
            else
            {
                OpenOrCloseChapterList(true, false);
                chapterTextBoxMainPage.Margin = new Thickness(20);
                textBoxZoomSliderZoomText.Visibility = Visibility.Visible;
                textBoxZoomSlider.Visibility = Visibility.Visible;
            }

            if (focusMode == null)
            {
                storyInfoDetailed.Visibility = ActualWidth < 700 ? Visibility.Collapsed : Visibility.Visible;
                storyInfo.Visibility = ActualWidth >= 700 ? Visibility.Collapsed : Visibility.Visible;
            }

            UpdateTextBoxZoom(textBoxZoomSlider.Value);
        }

        public void OpenOrCloseChapterList(bool open, bool manually)
        {
            double addOrSubtract = 0;

            if (!open)
            {
                //chapterListComponentMainPage.Visibility = Visibility.Collapsed;
                chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 2);
                mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                mainGrid.ColumnDefinitions[1].MinWidth = 0;
                closeOpenChapterListComponentIcon.Symbol = Symbol.ClosePane;
                addOrSubtract = chapterListComponentMainPage.ActualWidth;

                if (!chapterList.closedManually)
                    chapterList.closedManually = manually;
            }
            else
            {
                if (!chapterList.closedManually || manually)
                {
                   // chapterListComponentMainPage.Visibility = Visibility.Visible;
                    chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 1);
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                    mainGrid.ColumnDefinitions[1].MinWidth = 220;
                    closeOpenChapterListComponentIcon.Symbol = Symbol.OpenPane;
                    addOrSubtract = -chapterListComponentMainPage.ActualWidth;

                    chapterList.closedManually = false;
                }
            }

            chapterText.textBox.Width = (chapterText.textBoxScrollViewer.ActualWidth + addOrSubtract) * (1 / (textBoxZoomSlider.Value / 25));
        }

        #region DownBar
        public void UpdateDownBar() => ProjectStatsDialogue.UpdateDownBar();

        private void OnDownBarText_Click(object sender, RoutedEventArgs e) => ProjectStatsDialogue.Open();

        private void OnCloseChapterListComponent_Click(object sender, RoutedEventArgs e) =>
            OpenOrCloseChapterList(closeOpenChapterListComponentIcon.Symbol == Symbol.ClosePane, true);
        #endregion

        #region Zoom
        private void OnTextBoxZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (chapterList.listView.SelectedItem != null)
            {
                UpdateTextBoxZoom(textBoxZoomSlider.Value);
                ApplicationData.Current.LocalSettings.Values["TextBoxZoomValue"] = textBoxZoomSlider.Value;
            }
        }

        public void UpdateTextBoxZoom(double sliderValue)
        {
            double sliderOne = sliderValue / 25;
            _ = chapterText.textBoxScrollViewer.ChangeView(null, null, (float)sliderOne);

            chapterText.textBox.Width = chapterText.textBoxScrollViewer.ActualWidth * (1 / sliderOne);
            textBoxZoomText.Text = $"{Math.Round(sliderOne * 100)}%";
        }

        public void LoadTextBoxZoom()
        {
            textBoxZoomSlider.Value = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values["TextBoxZoomValue"] ?? 25);
            current.UpdateTextBoxZoom(textBoxZoomSlider.Value);
        }

        private void OnTextBoxZoomText_Click(object sender, RoutedEventArgs e)
        {
            textBoxZoomTextFlyout.ShowAt(textBoxZoomText);
            textBoxZoomTextFlyoutTextBox.Value = textBoxZoomSlider.Value * 4;
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            textBoxZoomSlider.Value = 25;
            textBoxZoomTextFlyoutTextBox.Value = textBoxZoomSlider.Value * 4;
            textBoxZoomTextFlyoutTextBox.Text = "100%";
        }

        private void OnTextBoxZoomTextFlyoutTextBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!double.IsNaN(sender.Value))
                textBoxZoomSlider.Value = sender.Value / 4;
        }
        #endregion

        private void OnShortCut_Pressed(object sender, KeyRoutedEventArgs e)
        {
            //ShortcutManager.Check(e);
        }
    }
}
