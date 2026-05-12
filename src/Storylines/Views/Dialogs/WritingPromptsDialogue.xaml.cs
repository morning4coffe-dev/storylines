
namespace Storylines.Views.Dialogs;

public sealed partial class WritingPromptsDialogue : AppContentDialog
{
    public WritingPromptsViewModel ViewModel { get; }

    public WritingPromptsDialogue()
    {
        ViewModel = App.GetService<WritingPromptsViewModel>();
        ViewModel.CloseRequested += () => Hide();

        InitializeComponent();
    }

    public static async Task OpenAsync()
    {
        try
        {
            await App.GetService<IDialogService>().ShowAsync(new WritingPromptsDialogue());
        }
        catch (Exception ex)
        {
            App.TryGetService<ILogger>()?.Warning($"Failed to open writing prompts dialog: {ex.Message}");
        }
    }

    private void OnShuffle_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowRandomPromptCommand.Execute(null);
    }

    private void OnCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedCategoryIndex = categoryComboBox.SelectedIndex;
    }

    private void OnCopyPrompt_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyPromptCommand.Execute(null);
    }

    private void OnCreateChapterFromPrompt_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateChapterFromPromptCommand.Execute(null);
    }
}
