
namespace Storylines.Views.Dialogs;

public sealed partial class SaveDialogue : AppContentDialog
{
    private bool _submitted;

    public SaveDialogueViewModel ViewModel { get; }

    public enum Type { Save, SaveCopy }

    public SaveDialogue(Type dialogType)
    {
        ViewModel = new SaveDialogueViewModel(
            App.GetService<ILogger>(),
            App.GetService<IProjectPersistenceService>(),
            App.GetService<ProjectState>(),
            App.GetService<IFilePickerService>(),
            dialogType);

        InitializeComponent();
        CloseOnOutsideTap = true;

        extensionComboBox.IsEnabled = ViewModel.IsExtensionSelectionEnabled;
        extensionComboBox.SelectedIndex = 0;

        title.Text = Storylines.Resources.SaveDialogue.Title(dialogType);
    }

    public static void Open(Type type)
    {
        _ = OpenAsync(type);
    }

    public static Task<ContentDialogResult> OpenAsync(Type type)
    {
        return App.GetService<IDialogService>().ShowAsync(new SaveDialogue(type));
    }

    private void OnSaveToLocationButton_Click(object sender, RoutedEventArgs e)
    {
        _ = ChooseLocationAsync();
    }

    private void OnSaveLocationFrame_Tapped(object sender, TappedRoutedEventArgs e)
    {
        _ = ChooseLocationAsync();
    }

    private async Task ChooseLocationAsync()
    {
        await ViewModel.ChooseSaveLocationCommand.ExecuteAsync(null);
        if (ViewModel.HasLocation)
        {
            locationTextPlaceholder.Visibility = Visibility.Collapsed;
        }
    }

    private void OnTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.FileName = fileNameText.Text;
        ViewModel.ProjectName = nameText.Text;
    }

    private async void OnSubmitButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SubmitCommand.ExecuteAsync(null);
        _submitted = true;
        Hide();
    }

    private void OnCancelButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void ContentDialog_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && ViewModel.IsSubmitEnabled)
            OnSubmitButton_Click(sender, new RoutedEventArgs());
    }

    private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        if (!_submitted)
            ViewModel.CancelCommand.Execute(null);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        _ = ChooseLocationAsync();
    }

    bool isFlyoutOpen = false;
    private void OnExtensionComboBox_DropDownOpened(object sender, object e)
    {
        isFlyoutOpen = true;
    }

    private void OnExtensionComboBox_DropDownClosed(object sender, object e)
    {
        isFlyoutOpen = false;
        ViewModel.SelectedExtensionIndex = extensionComboBox.SelectedIndex;
    }

    protected override bool CanCloseOnOutsideTap()
    {
        return !isFlyoutOpen;
    }
}
