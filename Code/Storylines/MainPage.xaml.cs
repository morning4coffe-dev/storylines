using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using System;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Storylines
{
    public sealed partial class MainPage : Page
    {
        public static ContentDialog currentlyOpenedDialogue;

        public static MainPage mainPage;
        public static ChapterListComponent chapterList;
        public static MainCommandBar commandBar;
        public static ChapterTextBox chapterText;

        public static SettingsPage settings;

        public bool unSavedProgress = false;

        public Button backButton;

        public static SaveSystem saveSystem = new SaveSystem();

        public MainPage()
        {
            Windows.UI.ViewManagement.UISettings uiSettings = new Windows.UI.ViewManagement.UISettings();
            backButton = new Button()
            {
                Content = new SymbolIcon() { Symbol = Symbol.Back, Height = 12, Width = 18 },
                Visibility = Visibility.Collapsed,
                Background = new SolidColorBrush(Colors.Transparent),
                Style = Application.Current.Resources["ButtonRevealStyle"] as Style,
                Height = 35,
                Width = 40,
                BorderThickness = new Thickness(0)
            };
            backButton.Click += BackButton_Click;

            Window.Current.SetTitleBar(backButton);

            InitializeComponent();
            mainPage = this;

            mainPage.mainGrid.Children.Add(backButton);

            _ = ProjectFile.LoadAllAsync();

            UpdateTitleBar();

            AppSizeChanged();

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequest;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.item != null)
            {
                saveSystem.DefaultLaunch(App.item);
                App.item = null;
            }

            LoadTextBoxZoom();
        }

        public void UpdateTitleBar()
        {
            appHeaderText.Text =
            $"{Package.Current.DisplayName} -" +
            $" {SaveSystem.loadedProjectName}" +
            $"{_ = (unSavedProgress ? "*" : string.Empty)} -" +
            $" v{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}" +
            $"{(Package.Current.IsDevelopmentMode ? " (Development Build)" : " (Preview Build)")}";
        }

        public void ClearEverything()
        {
            chapterText.textBox.Document.SetText(Windows.UI.Text.TextSetOptions.None, "");
            chapterList.chapters.Clear();
            chapterList.chaptersListView.Items.Clear();
            EnableOrDisableChapterTools(false);
            Characters.characters.Clear();
        }

        public void EnableOrDisableChapterTools(bool enable)
        {
            if (enable)
            {
                chapterText.textBoxRectangle.Visibility = Visibility.Collapsed;
                chapterText.textBox.IsTabStop = true;
                chapterList.chapterDeleteButton.IsEnabled = true;
                textBoxZoomSlider.IsEnabled = true;
                textBoxZoomTextHyperlink.IsEnabled = true;

                UpdateDownBar();
            }
            else
            {
                chapterText.textBoxRectangle.Visibility = Visibility.Visible;
                chapterText.textBox.IsTabStop = false;
                chapterList.chapterDeleteButton.IsEnabled = false;
                textBoxZoomSlider.IsEnabled = false;
                textBoxZoomTextHyperlink.IsEnabled = false;

                downBarText.Text = "Add a new chapter, select it and then start typing.";
            }
        }

        public void SomethingChanged()
        {
            mainPage.unSavedProgress = !chapterList.switchedChapters;
            mainPage.UpdateTitleBar();

            chapterList.switchedChapters = false;
        }

        public void Autosave()
        {
            if (SettingsPage.isAutosaveEnabled && mainPage.unSavedProgress)
            {
                saveSystem.Save();
                //prehraj animaci
            }
        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            chapterList.OnChapterListComponent_PreviewKeyDown(sender, e);
        }

        private void OnPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AppSizeChanged();
        }

        public void AppSizeChanged()
        {
            chapterList.chapterListCommandBar.DefaultLabelPosition = ActualWidth < 1200 ? CommandBarDefaultLabelPosition.Collapsed : CommandBarDefaultLabelPosition.Right;

            if (ActualWidth < 740)
            {
                chapterList.chapterListCommandBar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible;
                commandBar.mainCommandBar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible;

                OpenOrCloseChapterListComponent(false);
            }
            else
            {
                chapterList.chapterListCommandBar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;
                commandBar.mainCommandBar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;

                OpenOrCloseChapterListComponent(true);
            }

            if (ActualWidth < 820)
            {
                OpenOrCloseChapterListComponent(false);
                chapterTextBoxMainPage.Margin = new Thickness(16);
            }
            else
            {
                OpenOrCloseChapterListComponent(true);
                chapterTextBoxMainPage.Margin = new Thickness(42);
            }

            UpdateTextBoxZoom(textBoxZoomSlider.Value);

            //if (!Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().IsFullScreenMode)
        }

        private void OnCloseChapterListComponent_Click(object sender, RoutedEventArgs e)
        {
            OpenOrCloseChapterListComponent(chapterListComponentMainPage.Visibility != Visibility.Visible);
        }

        public void OpenOrCloseChapterListComponent(bool open)
        {
            double addOrSubtract;
            if (!open)
            {
                chapterListComponentMainPage.Visibility = Visibility.Collapsed;
                chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 2);
                downBarMainPage.SetValue(Grid.ColumnSpanProperty, 2);
                closeOpenChapterListComponent.Icon = new SymbolIcon(Symbol.ClosePane);
                addOrSubtract = chapterListComponentMainPage.ActualWidth;
            }
            else
            {
                chapterListComponentMainPage.Visibility = Visibility.Visible;
                chapterTextBoxMainPage.SetValue(Grid.ColumnSpanProperty, 1);
                downBarMainPage.SetValue(Grid.ColumnSpanProperty, 1);
                closeOpenChapterListComponent.Icon = new SymbolIcon(Symbol.OpenPane);
                addOrSubtract = -chapterListComponentMainPage.ActualWidth;
            }

            chapterText.textBox.Width = (chapterText.textBoxScrollViewer.ActualWidth + addOrSubtract) * (1 / (textBoxZoomSlider.Value / 25));
        }

        private void OnCloseRequest(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            if (unSavedProgress)
            {
                e.Handled = true;
                _ = ExitDialogue.Open(true);
            }
        }
        #region DownBar
        public void UpdateDownBar()
        {
            RichEditBox textBox = chapterText.textBox;

            textBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out string txt);
            _ = txt.Replace(" ", "");

            string[] words = txt.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries);

            string[] paragraphs = txt.Split((char)13, StringSplitOptions.None);

            string selectedLetters = textBox.Document.Selection.Text.Length != 0 ? $" ({textBox.Document.Selection.Text.Length})" : "";

            downBarText.Text = $"Characters: {txt.Length - 1}{selectedLetters}   Words: {words.Length}   Paragraphs: {paragraphs.Length - 1}";
        }

        private void OnDownBarText_Click(object sender, RoutedEventArgs e)
        {
            ProjectStatsDialogue.Open();
        }
        #endregion

        #region Zoom
        private void OnTextBoxZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (chapterList.chaptersListView.SelectedItem != null)
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
            textBoxZoomText.Text = $"{Math.Round(sliderOne * 100)} %";
        }

        public void LoadTextBoxZoom()
        {
            textBoxZoomSlider.Value = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values["TextBoxZoomValue"] ?? 25);
            mainPage.UpdateTextBoxZoom(textBoxZoomSlider.Value);
        }

        private void OnTextBoxZoomText_Click(object sender, RoutedEventArgs e)
        {
            textBoxZoomTextFlyout.ShowAt(textBoxZoomText);
            textBoxZoomTextFlyoutTextBox.Text = textBoxZoomText.Text.Replace(" %", "");
        }

        private void OnTextBoxZoomText_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            textBoxZoomSlider.Value = 25;
            textBoxZoomTextFlyoutTextBox.Text = "100 %";
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            char[] chars = (sender as TextBox).Text.ToString().ToCharArray();
            string str = "";
            int dig = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsDigit(chars[i]))
                {
                    str += chars[i];
                    dig = Convert.ToInt32(str);
                }
            }

            textBoxZoomSlider.Value = dig * 25 / 100;
        }
        #endregion

        #region Settings
        public void OpenSettings(bool open)
        {
            if (open)
            {
                settingsPageView.Children.Add(new SettingsPage());
                backButton.Visibility = Visibility.Visible;
                mainPageView.Visibility = Visibility.Collapsed;
            }
            else
            {
                _ = settingsPageView.Children.Remove(settings);
                mainPageView.Visibility = Visibility.Visible;
                backButton.Visibility = Visibility.Collapsed;
            }
        }

        public static void LoadSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            SettingsPage.ChangeTheme(Convert.ToInt32(localSettings.Values["AppTheme"] ?? 2));

            SettingsPage.appColorEnabled = Convert.ToBoolean(localSettings.Values["AppColor"] ?? true);
            SettingsPage.UpdateSystemColor(SettingsPage.appColorEnabled);

            SettingsPage.textBoxSolidBackground = Convert.ToBoolean(localSettings.Values["SolidBackground"] ?? false);
            chapterText.TextBoxWhiteBackground(SettingsPage.textBoxSolidBackground);

            SettingsPage.isExitDialogueOn = Convert.ToBoolean(localSettings.Values["ExitDialogue"] ?? true);

            SettingsPage.isOnPageDownNewChapterEnabled = Convert.ToBoolean(localSettings.Values["OnPageDownNewChapterEnabled"] ?? true);

            commandBar.autosaveToggleButton.IsChecked = Convert.ToBoolean(localSettings.Values["AutosaveEnabled"] ?? false);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings(false);
            AppSizeChanged();
        }
        #endregion
    }
}
