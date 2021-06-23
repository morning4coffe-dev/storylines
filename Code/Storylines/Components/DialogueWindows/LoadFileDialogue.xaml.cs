using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class LoadFileDialogue : ContentDialog
    {
        public static LoadFileDialogue loadFile;
        public bool isEscape = true;

        public LoadFileDialogue()
        {
            this.InitializeComponent();
            loadFile = this;

            MainPage.currentlyOpenedDialogue = loadFile;
        }

        public static void Open()
        {
            if (!MainPage.mainPage.unSavedProgress)
            {
                if (MainPage.currentlyOpenedDialogue != null)
                    MainPage.currentlyOpenedDialogue.Hide();

                var loadDialogue = new LoadFileDialogue();
                _ = loadDialogue.ShowAsync();

                loadDialogue.RequestedTheme = MainPage.mainPage.RequestedTheme;
            }
            else
            {
                _ = ExitDialogue.Open(false);
            }
        }

        private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (isEscape)
                args.Cancel = true;
            if (!isEscape)
                MainPage.currentlyOpenedDialogue = null;
        }

        private void OnCreateNewProject_Click(object sender, RoutedEventArgs e)
        {
            isEscape = false;
            this.Hide();

            SaveSystem.saveFile = null;
            SaveSystem.loadedProjectName = "AStoryWithNoName.srl";
            MainPage.mainPage.ClearEverything();
            MainPage.mainPage.unSavedProgress = false;
        }

        private void OnFindProject_Click(object sender, RoutedEventArgs e)
        {
            MainPage.saveSystem.OpenFileEplorerLoadAsync();
        }

        public void NewProjectsButton(ProjectFile pf)
        {
            Button bt = new Button()
            {
                Name = pf.token,
                Content = pf.name,
                Tag = pf.path,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 40,
                CornerRadius = new CornerRadius(4),
                Style = (Style)Application.Current.Resources["ButtonRevealStyle"],
            };

            ToolTip toolTip = new ToolTip
            {
                Content = bt.Tag
            };
            ToolTipService.SetToolTip(bt, toolTip);

            bt.Click += OnOpenRecentProject_Click;
            bt.RightTapped += OnOpenRecentProject_RightTapped;

            projectsHolder.Children.Insert(0, bt);
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            foreach (var storageFileToken in ProjectFile.projectFiles)
            {
                NewProjectsButton(storageFileToken);
            }

            CheckIfProjectsHolderIsEmpty();
        }

        private void OnOpenRecentProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = 0; i < ProjectFile.projectFiles.Count; i++)
                {
                    if (ProjectFile.projectFiles[i].token == (sender as Button).Name)
                    {
                        _ = MainPage.saveSystem.Load(ProjectFile.projectFiles[i].file);
                    }
                }
            }
            catch
            {
                projectsHolder.Children.Remove(sender as Button);
                ProjectFile.Remove((sender as Button).Name);

                CheckIfProjectsHolderIsEmpty();
            }
        }

        private Button projectToRemove;

        private void OnOpenRecentProject_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if ((sender as TextBlock) != null)
            {
                sender = VisualTreeHelper.GetParent((FrameworkElement)sender);
                sender = VisualTreeHelper.GetParent((FrameworkElement)sender) as Grid;
            }

            if (sender as Grid != null)
            {
                sender = VisualTreeHelper.GetParent((FrameworkElement)sender) as Button;
            }

            if (sender as Button != null)
            {
                projectsHolderFlyout.ShowAt((Button)sender, e.GetPosition((Button)sender));
                projectToRemove = (Button)sender;
            }
        }

        private void OnProjectRemove_Click(object sender, RoutedEventArgs e)
        {
            if (projectToRemove != null)
            {
                projectsHolder.Children.Remove(projectToRemove);
                ProjectFile.Remove(projectToRemove.Name);

                CheckIfProjectsHolderIsEmpty();
            }
        }

        public void CloseMenu()
        {
            this.Hide();
        }

        public void CheckIfProjectsHolderIsEmpty()
        {
            if (projectsHolder.Children.Count > 0)
            {
                noFilesText.Visibility = Visibility.Collapsed;
            }
            else
            {
                noFilesText.Visibility = Visibility.Visible;
            }
        }
    }
}
