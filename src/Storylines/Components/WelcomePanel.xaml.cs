using Storylines.Components.DialogueWindows;
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
            SaveDialogue.Open(SaveDialogue.Type.Save);
        }

        private void OnOpenProjectButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProjectDialogue.Open();
        }
    }
}
