using Newtonsoft.Json.Linq;
using System;
using System.Xml.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Storylines.Scripts.Functions;

namespace Storylines.Components.CustomControls
{
    public sealed partial class WhatsNextBanner : UserControl
    {
        public WhatsNextBanner()
        {
            this.InitializeComponent();
        }

        private void WhatsNext_OnClick(object sender, RoutedEventArgs e)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://medium.com/p/2a0e1a3c9c1a"));

            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData("OnWhatsNext_Click", "true");
        }
    }
}
