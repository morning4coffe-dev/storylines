using Storylines.Scripts.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components
{
    public sealed partial class WelcomePanel : UserControl
    {
        public WelcomePanel()
        {
            InitializeComponent();
        }

        private void OnNewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceLocator.Dialogs.OpenSaveDialogue();
        }

        private void OnOpenProjectButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceLocator.Dialogs.OpenLoadDialogue();
        }
    }
}
