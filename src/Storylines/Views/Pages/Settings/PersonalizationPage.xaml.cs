using Storylines.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;

namespace Storylines.Views.Pages.Settings
{
    public sealed partial class PersonalizationPage : Page
    {
        public PersonalizationSettingsViewModel ViewModel { get; }

        public PersonalizationPage()
        {
            ViewModel = App.GetService<PersonalizationSettingsViewModel>();
            InitializeComponent();
        }
    }
}
