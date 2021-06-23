using Microsoft.Services.Store.Engagement;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Storylines.Pages.SettingsPages
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
            UpdateSize();
        }

        private void OnReport_Click(object sender, RoutedEventArgs e)
        {
            reportStack.Visibility = Visibility.Visible;
            sendButton.IsEnabled = false;
        }

        private void OnSentReport_Click(object sender, RoutedEventArgs e)
        {
            if (reportTextBox.Text != "")
            {
                StoreServicesCustomEventLogger.GetDefault().Log($"Bug:({Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision})_{ reportTextBox.Text}");
                reportTextBox.Text = "";
            }

        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSize();
        }

        public void UpdateSize()
        {
            if (this.ActualWidth < 900)
            {
                reportStack.Orientation = Orientation.Vertical;
                reportTextBox.Width = 360;
            }
            else
            {
                reportStack.Orientation = Orientation.Horizontal;
                reportTextBox.Width = 700;
            }
        }

        private void reportTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (reportTextBox.Text.Length > 2)
            {
                sendButton.IsEnabled = true;
            }
            else
            {
                sendButton.IsEnabled = false;
            }
        }
    }
}
