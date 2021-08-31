using Storylines.Scripts.Functions;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Storylines.Pages.SettingsPages
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
            appVersion = $"{ResourceLoader.GetForCurrentView().GetString("version")}: {v.Major}.{v.Minor}.{v.Build}";
        }

        private void OnReviewAndRateHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            _ = MicrosoftStoreAndAppCenterFunctions.PromptUserToRateApp();
        }

        private void OnAboutPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualWidth < 1070)
                aboutPage.Width = this.ActualWidth - 70;
            else
                aboutPage.Width = 1000;

            if (aboutPage.ActualWidth < 600)
            {
                linksStack.Orientation = Orientation.Vertical;
                linksStack.Spacing = 8;
            }
            else
            {
                linksStack.Orientation = Orientation.Horizontal;
                linksStack.Spacing = 120;
            }
        }
    }
}
