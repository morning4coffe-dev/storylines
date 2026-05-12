
namespace Storylines.Views.Dialogs;

public sealed partial class LoadProjectDialogue : AppContentDialog
{
    private static IProjectPersistenceService Persistence => App.GetService<IProjectPersistenceService>();
    private static WindowContext WindowContext => App.GetService<WindowContext>();

    public bool isEscape = true;

    public static Thickness osMargin = new Thickness(-11, 4, -19, 4); /*new Thickness(-15, 4, -15, 4);*/
    public static double osWidth = 375;

    public LoadProjectDialogue()
    {
        InitializeComponent();
        projectsHolder.ItemsSource = null;
    }

    public static void Open(XamlRoot root)
        => _ = OpenAsync(root);

    public static async Task<ContentDialogResult> OpenAsync(XamlRoot root)
    {
        if (TimeTravelSystem.unSavedProgress)
        {
            _ = App.GetService<IDialogService>().ShowUnsavedProgressDialogueAsync(false);
            return ContentDialogResult.None;
        }

        var loadDialogue = new LoadProjectDialogue();

        try
        {
            return await App.GetService<IDialogService>().ShowAsync(
                loadDialogue,
                new DialogShowOptions
                {
                    XamlRootOverride = root,
                });
        }
        catch (Exception ex)
        {
            App.TryGetService<ILogger>()?.Warning($"Failed to open load dialog: {ex.Message}");
            return ContentDialogResult.None;
        }
    }
    public async Task LoadAllProjectsAsync()
    {
        var task = ProjectFile.LoadAllAsync();

        if (await Task.WhenAny(task, Task.Delay(1000)) == task)
        {
            ProjectFile.projectFiles = new ObservableCollection<ProjectFile>(ProjectFile.projectFiles.OrderByDescending(o => o.LastEdited).ToList());
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

        Persistence.CurrentProject = new ProjectFile();
        Persistence.CurrentProject.projectName = "Project with no name";

        WindowContext.MainPage?.UpdateToolsForDocument(true);
        WindowContext.AppView?.ClearEverything();
        TimeTravelSystem.unSavedProgress = false;
    }

    private void OnFindProject_Click(object sender, RoutedEventArgs e)
    {
        Persistence.Load(new ProjectFile() { file = null });
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
                if (projectFile.Token == token)
                    Persistence.Load(projectFile);
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
        if ((sender as Button) is not null)
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
        if (projectToRemove is not null)
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
        }
    }

    private void OnProjectsHolder_ItemClick(object sender, ItemClickEventArgs e)
    {
        TryLoadProject(((sender as ListView).SelectedItem as ProjectFile).Token);
    }
}
