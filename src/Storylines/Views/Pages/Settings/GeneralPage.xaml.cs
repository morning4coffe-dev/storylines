using Storylines.ViewModels.Settings;

namespace Storylines.Views.Pages.Settings;

public sealed partial class GeneralPage : Page
{
    public GeneralSettingsViewModel ViewModel { get; }

    public GeneralPage()
    {
        ViewModel = App.GetService<GeneralSettingsViewModel>();
        InitializeComponent();
    }
}
