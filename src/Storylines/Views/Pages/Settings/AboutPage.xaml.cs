using Storylines.Helpers;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace Storylines.Views.Pages.Settings
{
    public sealed partial class AboutPage : Page
    {
        private string appName;
        private string appVersion;

        public AboutPage()
        {
            this.InitializeComponent();

            appName = Package.Current.DisplayName;
            var v = Package.Current.Id.Version;
            appVersion = $"{ResourceLoader.GetForViewIndependentUse().GetString("version")}: {v.Major}.{v.Minor}.{v.Build}";
        }

        private void OnReviewAndRateHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            _ = MicrosoftStoreFunctions.PromptUserToRateAppAsync("about_page");
        }

        private void OnCheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            _ = MicrosoftStoreFunctions.CheckForNewUpdateAvailableAsync();
        }
    }
}
