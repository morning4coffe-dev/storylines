using Microsoft.UI.Xaml.Controls;
using Storylines.Pages.SettingsPages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Storylines.Pages
{
    public sealed partial class SettingsPage : Windows.UI.Xaml.Controls.Page
    {
        public static SettingsPage settings;

        public SettingsPage()
        {
            InitializeComponent();
            settings = this;

            AppView.current.page = AppView.Pages.MainPage;

            UpdateSize();
        }

        private void OnSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var item = args.SelectedItem as NavigationViewItem;
            SwitchPage(item.Tag.ToString());
        }

        public void SwitchPage(string tag)
        {
            switch (tag)
            {
                case "General":
                    contentFrame.Navigate(typeof(GeneralPage), null, new SuppressNavigationTransitionInfo());
                    break;
                case "Personalize":
                    contentFrame.Navigate(typeof(PersonalizationPage), null, new SuppressNavigationTransitionInfo());
                    break;
                case "Accessibility":
                    contentFrame.Navigate(typeof(AccessibilityPage), null, new SuppressNavigationTransitionInfo());
                    break;
                case "About":
                    contentFrame.Navigate(typeof(AboutPage), null, new SuppressNavigationTransitionInfo());
                    break;
            }
        }


        private void OnAboutPageItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            settingsNavigationView.SelectedItem = aboutPageItem;
        }

        private void OnAboutPageItem_GotFocus(object sender, RoutedEventArgs e)
        {
            //OnAboutPageItem_Tapped(sender, new Windows.UI.Xaml.Input.TappedRoutedEventArgs());
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSize();
        }

        private void UserControl_ActualThemeChanged(FrameworkElement sender, object args)
        {
            //themeBorderThickness = sender.ActualTheme == ElementTheme.Dark ? new Thickness(0) : new Thickness(0.5);
        }

        public void UpdateSize()
        {
            if (ActualWidth < 780)
                settingsNavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
            else
                settingsNavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
        }

        private void Page_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            var item = Windows.UI.Xaml.Input.FocusManager.GetFocusedElement();
            if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space)
                if (item is NavigationViewItem && (item as NavigationViewItem).Tag.ToString() == "About")
                    settingsNavigationView.SelectedItem = aboutPageItem;
        }
    }
}
