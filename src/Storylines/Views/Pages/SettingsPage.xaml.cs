using Microsoft.UI.Xaml.Controls;
using Storylines.Constants;
using Storylines.Views.Pages.Settings;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Storylines.Services;

namespace Storylines.Views.Pages
{
    public sealed partial class SettingsPage : Microsoft.UI.Xaml.Controls.Page
    {
        private const double MinimalPaneBreakpoint = LayoutConstants.SettingsMinimalPaneBreakpoint;
        private const double CompactPaneBreakpoint = LayoutConstants.SettingsCompactPaneBreakpoint;
        private NavigationViewPaneDisplayMode? _lastPaneDisplayMode;

        public SettingsPage()
        {
            InitializeComponent();

            App.GetService<WindowContext>().AppView.page = AppView.Pages.Settings;

            UpdateSize();
        }

        private void OnSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is not NavigationViewItem item || item.Tag is not string tag)
                return;

            aboutPageItem.IsSelected = false;
            SwitchPage(tag);
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

            if (pageType is not null)
                contentFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
        }

        private void OnAboutPageItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            SelectAboutPage();
        }

        private void SelectAboutPage()
        {
            if (contentFrame.CurrentSourcePageType == typeof(AboutPage) && aboutPageItem.IsSelected)
                return;

            foreach (var menuItem in settingsNavigationView.MenuItems)
                if (menuItem is NavigationViewItem navigationViewItem)
                    navigationViewItem.IsSelected = false;

            aboutPageItem.IsSelected = true;
            SwitchPage("About");
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

            if (_lastPaneDisplayMode == paneDisplayMode)
                return;

            _lastPaneDisplayMode = paneDisplayMode;
            settingsNavigationView.PaneDisplayMode = paneDisplayMode;
            settingsNavigationView.IsPaneOpen = paneDisplayMode == NavigationViewPaneDisplayMode.Auto;
        }

        private void Page_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            var item = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement();
            if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space)
                if (item is NavigationViewItem && (item as NavigationViewItem).Tag.ToString() == "About")
                    SelectAboutPage();
        }
    }
}
