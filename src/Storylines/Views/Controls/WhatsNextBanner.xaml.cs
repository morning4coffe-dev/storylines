using Storylines.Helpers;
using Storylines.Services.Interfaces;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

            App.TryGetService<ITelemetryService>()?.TrackBannerClicked("recurrents", "microsoft_store");
        }

        private void GitHubBanner_OnClick(object sender, RoutedEventArgs e)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/morning4coffe-dev/storylines"));

            App.TryGetService<ITelemetryService>()?.TrackBannerClicked("github", "github_repository");
        }
    }
}
