
namespace Storylines.Views.Dialogs;

public sealed partial class ProjectFileInfoDialogue : AppContentDialog
{
    public ProjectFileInfoViewModel ViewModel { get; }

    public ProjectFileInfoDialogue()
    {
        ViewModel = App.GetService<ProjectFileInfoViewModel>();
        InitializeComponent();
        CloseOnOutsideTap = true;
    }

    public static void Open()
    {
        _ = OpenAsync();
    }

    public static async Task<ContentDialogResult> OpenAsync()
    {
        var dialog = new ProjectFileInfoDialogue();
        var showTask = App.GetService<IDialogService>().ShowAsync(dialog);
        await dialog.ViewModel.LoadFileInfoAsync();
        return await showTask;
    }

    private void OnCloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
