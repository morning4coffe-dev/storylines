using Microsoft.UI.Xaml.Controls;
using Storylines.ViewModels.Settings;

namespace Storylines.Views.Pages.Settings
{
    public sealed partial class DeveloperPage : Page
    {
        public DeveloperSettingsViewModel ViewModel { get; }

        public DeveloperPage()
        {
            ViewModel = App.GetService<DeveloperSettingsViewModel>();
            InitializeComponent();
            Loaded += (_, _) => ViewModel.RefreshSnapshotCommand.Execute(null);
        }
    }
}
