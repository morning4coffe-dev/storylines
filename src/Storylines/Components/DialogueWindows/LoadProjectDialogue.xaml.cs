using Storylines.Scripts.Functions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class LoadProjectDialogue : ContentDialog
    {
        public static LoadProjectDialogue loadFile;
        public bool isEscape = true;

        public static Thickness osMargin = new Thickness(-11, 4, -19, 4); /*new Thickness(-15, 4, -15, 4);*/
        public static double osWidth = 375;

        public LoadProjectDialogue()
        {
            InitializeComponent();
            loadFile = this;

            AppView.currentlyOpenedDialogue = loadFile;
            projectsHolder.ItemsSource = null;
        }

        public static void Open()
        {
            if (!TimeTravelSystem.unSavedProgress)
            {
                if (AppView.currentlyOpenedDialogue != null)
                    AppView.currentlyOpenedDialogue.Hide();

                var loadDialogue = new LoadProjectDialogue();
                _ = loadDialogue.ShowAsync();

                loadDialogue.RequestedTheme = AppView.current.RequestedTheme;
            }
            else
                _ = NotificationManager.DisplayUnsavedProgressDialogue(false);
        }
        public async Task LoadAllProjectsAsync()
        {
            var task = ProjectFile.LoadAllAsync();

            if (await Task.WhenAny(task, Task.Delay(1000)) == task)
            {
                ProjectFile.projectFiles = new ObservableCollection<ProjectFile>(ProjectFile.projectFiles.OrderByDescending(o => o.lastEdited).ToList());
                projectsHolder.ItemsSource = ProjectFile.projectFiles;

                progressRing.IsActive = false;
                CheckIfProjectsHolderIsEmpty();
            }
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            _ = LoadAllProjectsAsync();
        }

        private void OnCreateNewProject_Click(object sender, RoutedEventArgs e)
        {
            isEscape = false;
            Hide();

            SaveSystem.currentProject = new ProjectFile();
            SaveSystem.currentProject.projectName = "Project with no name";

            Pages.MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(true);
            AppView.current.ClearEverything();
            TimeTravelSystem.unSavedProgress = false;
        }

        private void OnFindProject_Click(object sender, RoutedEventArgs e)
        {
            SaveSystem.Load(new ProjectFile() { file = null });
        }

        private void OnOpenRecentProject_Click(object sender, RoutedEventArgs e)
        {
            TryLoadProject((sender as Button).Tag.ToString());
        }

        private void TryLoadProject(string token)
        {
            try
            {
                foreach (var projectFile in ProjectFile.projectFiles)
                {
                    if (projectFile.token == token)
                        SaveSystem.Load(projectFile);
                }
            }
            catch
            {
                ProjectFile.Remove(token);
            }

            CheckIfProjectsHolderIsEmpty();
        }

        private Button projectToRemove;

        private void OnOpenRecentProject_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if ((sender as Button) != null)
            {
                projectsHolderFlyout.ShowAt((Button)sender, e.GetPosition((Button)sender));
                projectToRemove = (Button)sender;
            }
        }

        private void OnOpenRecentProject_Holding(object sender, HoldingRoutedEventArgs e)
        {
            OnOpenRecentProject_RightTapped(sender, new RightTappedRoutedEventArgs());
        }

        private void OnProjectRemove_Click(object sender, RoutedEventArgs e)
        {
            if (projectToRemove != null)
            {
                ProjectFile.Remove(projectToRemove.Tag.ToString());

                CheckIfProjectsHolderIsEmpty();
            }
        }

        public void CheckIfProjectsHolderIsEmpty()
        {
            noFilesText.Visibility = ProjectFile.projectFiles.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public void CloseMenu()
        {
            Hide();
        }

        private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (isEscape)
                args.Cancel = true;
            if (!isEscape)
            {
                ProjectFile.projectFiles.Clear();
                AppView.currentlyOpenedDialogue = null;
            }
        }

        private void OnProjectsHolder_ItemClick(object sender, ItemClickEventArgs e)
        {
            TryLoadProject(((sender as ListView).SelectedItem as ProjectFile).token);
        }
    }
}
