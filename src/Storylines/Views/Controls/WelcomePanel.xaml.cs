using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Views.Controls
{
    public sealed partial class WelcomePanel : UserControl
    {
        public WelcomePanel()
        {
            InitializeComponent();
        }

        private void OnNewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            App.GetService<IDialogService>().OpenSaveDialogue();
        }

        private void OnOpenProjectButton_Click(object sender, RoutedEventArgs e)
        {
            App.GetService<IDialogService>().OpenLoadDialogue();
        }

        private async void OnWelcomePanel_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadRecentProjectsAsync();
        }

        private async Task LoadRecentProjectsAsync()
        {
            await ProjectFile.LoadAllAsync();

            var recent = ProjectFile.projectFiles
                .OrderByDescending(p => p.LastEdited)
                .Take(5)
                .ToList();

            if (recent.Count > 0)
            {
                recentProjectsList.ItemsSource = new ObservableCollection<ProjectFile>(recent);
                recentProjectsSection.Visibility = Visibility.Visible;
            }
        }

        private void OnRecentProject_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProjectFile project)
                SaveSystem.Load(project);
        }
    }
}
