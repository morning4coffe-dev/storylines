using Storylines.Services;
using Storylines.ViewModels;
using Storylines.ViewModels.Modes;
using Storylines.Views.Controls;
using Storylines.Views.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Storylines.Views.Controls.Modes
{
    public sealed partial class FocusModeOverlay : UserControl
    {
        // Exposed as a field so x:Bind in XAML can reach it without a full VM wrapper.
        internal readonly CommandBarViewModel _cmdBarVm;

        public FocusModeOverlay(FocusModeViewModel vm)
        {
            _cmdBarVm = App.GetService<CommandBarViewModel>();
            InitializeComponent();
        }

        private void OnAutosaveToggle_Click(object sender, RoutedEventArgs e)
        {
            _cmdBarVm.ToggleAutosaveCommand.Execute(null);
        }

        private void OnReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            // Delegate to the main command bar which owns the MediaElement.
            MainPage.CommandBar?.ReadAloud();
        }
    }
}
