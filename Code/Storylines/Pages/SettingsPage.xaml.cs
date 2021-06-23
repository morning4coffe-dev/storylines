using Storylines.Pages.SettingsPages;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Pages
{
    public sealed partial class SettingsPage : UserControl
    {
        public enum CurrentSelectedTheme { Light, Dark, System };
        public static CurrentSelectedTheme selectedTheme = CurrentSelectedTheme.System;
        public static readonly Color appColor = Color.FromArgb(255, 211, 105, 5);
        public static bool appColorEnabled = true;
        public static bool textBoxSolidBackground = false;
        public static bool isExitDialogueOn = true;
        public static bool isOnPageDownNewChapterEnabled = true;

        public static bool isAutosaveEnabled = false;

        public SettingsPage()
        {
            this.InitializeComponent();
            MainPage.settings = this;

            settingsNavigationView.SelectedItem = personalizationPageItem;
            UpdateSize();
        }

        public static void ChangeTheme(int id)
        {
            switch (id)
            {
                case 0:
                    MainPage.mainPage.RequestedTheme = ElementTheme.Light;
                    selectedTheme = CurrentSelectedTheme.Light;
                    break;
                case 1:
                    MainPage.mainPage.RequestedTheme = ElementTheme.Dark;
                    selectedTheme = CurrentSelectedTheme.Dark;
                    break;
                case 2:
                    UpdateSystemTheme();
                    selectedTheme = CurrentSelectedTheme.System;
                    break;
            }
            MainPage.chapterText.TextBoxWhiteBackground(textBoxSolidBackground);

            ApplicationData.Current.LocalSettings.Values["AppTheme"] = id;
        }

        public static void UpdateSystemColor(bool appAccent)
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            if (!appAccent)
            {
                appColorEnabled = false;
                var accent = new UISettings().GetColorValue(UIColorType.Accent);
                App.Current.Resources["SystemAccentColor"] = accent;
                titleBar.ButtonForegroundColor = accent;
                MainPage.mainPage.backButton.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(accent);
            }
            else
            {
                appColorEnabled = true;
                App.Current.Resources["SystemAccentColor"] = appColor;
                titleBar.ButtonForegroundColor = appColor;
                MainPage.mainPage.backButton.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(appColor);
                //update
            }

            MainPage.mainPage.RequestedTheme = MainPage.mainPage.RequestedTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
            MainPage.mainPage.RequestedTheme = MainPage.mainPage.RequestedTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        }

        private void OnSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var item = args.SelectedItem as NavigationViewItem;
            switch (item.Tag.ToString())
            {
                case "Personalize":
                    contentFrame.Navigate(typeof(PersonalizationPage));
                    break;
                case "Accessibility":
                    contentFrame.Navigate(typeof(PersonalizationPage));
                    break;
                case "About":
                    contentFrame.Navigate(typeof(AboutPage));
                    break;
            }
        }

        public static void UpdateSystemTheme()
        {
            try
            {
                var DefaultTheme = new UISettings();
                var uiTheme = DefaultTheme.GetColorValue(UIColorType.Background).ToString();
                if (uiTheme == "#FF000000")
                {
                    MainPage.mainPage.RequestedTheme = ElementTheme.Dark;
                }
                else if (uiTheme == "#FFFFFFFF")
                {
                    MainPage.mainPage.RequestedTheme = ElementTheme.Light;
                }
            }
            catch { }
        }

        private void OnPersonalizationPageItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            settingsNavigationView.SelectedItem = personalizationPageItem;
        }

        private void OnPersonalizationPageItem_GotFocus(object sender, RoutedEventArgs e)
        {
            OnPersonalizationPageItem_Tapped(sender, new Windows.UI.Xaml.Input.TappedRoutedEventArgs());
        }

        private void OnAboutPageItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            settingsNavigationView.SelectedItem = aboutPageItem;
        }

        private void OnAboutPageItem_GotFocus(object sender, RoutedEventArgs e)
        {
            OnAboutPageItem_Tapped(sender, new Windows.UI.Xaml.Input.TappedRoutedEventArgs());
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSize();
        }

        public void UpdateSize()
        {
            if (this.ActualWidth < 780)
            {
                settingsNavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
            }
            else
            {
                settingsNavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
            }
        }
    }
}
