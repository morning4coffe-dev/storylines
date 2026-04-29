using Storylines.ViewModels.Settings;
using Windows.UI.Xaml.Controls;

namespace Storylines.Views.Pages.Settings
{
    public sealed partial class AccessibilityPage : Page
    {
        public AccessibilitySettingsViewModel ViewModel { get; }

        public AccessibilityPage()
        {
            ViewModel = App.GetService<AccessibilitySettingsViewModel>();
            InitializeComponent();
        }
    }
}
