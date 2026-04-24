using Storylines.Helpers;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Views.Controls
{
    public sealed partial class WhatsNextBanner : UserControl
    {
        public WhatsNextBanner()
        {
            this.InitializeComponent();
        }

        private void Recurrents_OnClick(object sender, RoutedEventArgs e)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://apps.microsoft.com/detail/9N5MJT8G06KC?launch=true&cid=storylines-banner&mode=mini"));

            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData("Recurrents_OnClick", "true");
        }

        private void GitHubBanner_OnClick(object sender, RoutedEventArgs e)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/morning4coffe-dev/storylines"));

            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData("GitHubBanner_OnClick", "true");
        }
    }
}
