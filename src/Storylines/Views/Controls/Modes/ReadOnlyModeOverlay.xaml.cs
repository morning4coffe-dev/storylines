using Storylines.Services.Modes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Storylines.Views.Controls.Modes
{
    public sealed partial class ReadOnlyModeOverlay : UserControl
    {
        public ReadOnlyModeOverlay()
        {
            InitializeComponent();
        }

        private void OnExitPill_Click(object sender, RoutedEventArgs e)
        {
            App.TryGetService<EditorModeService>()?.Deactivate();
        }
    }
}
