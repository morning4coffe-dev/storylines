using Microsoft.UI.Xaml.Controls;
using Storylines.Constants;
using Storylines.Views.Pages.Settings;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Storylines.Views.Pages
{
    public sealed partial class SettingsPage : Windows.UI.Xaml.Controls.Page
    {
        private const double MinimalPaneBreakpoint = LayoutConstants.CompactBreakpoint;
        private const double CompactPaneBreakpoint = 1100;

        public SettingsPage()
        {
            InitializeComponent();

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
            Type pageType = tag switch
            {
                "General" => typeof(GeneralPage),
                "Personalize" => typeof(PersonalizationPage),
                "Accessibility" => typeof(AccessibilityPage),
                "About" => typeof(AboutPage),
                _ => null
            };

            if (pageType != null)
                contentFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
        }

        private void OnAboutPageItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            settingsNavigationView.SelectedItem = aboutPageItem;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSize();
        }

        public void UpdateSize()
        {
            var paneDisplayMode = ActualWidth < MinimalPaneBreakpoint
                ? NavigationViewPaneDisplayMode.LeftMinimal
                : ActualWidth < CompactPaneBreakpoint
                    ? NavigationViewPaneDisplayMode.LeftCompact
                    : NavigationViewPaneDisplayMode.Auto;

            settingsNavigationView.PaneDisplayMode = paneDisplayMode;
            settingsNavigationView.IsPaneOpen = paneDisplayMode == NavigationViewPaneDisplayMode.Auto;
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
