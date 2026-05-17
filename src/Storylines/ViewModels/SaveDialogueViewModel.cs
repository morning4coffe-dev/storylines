using SaveDialogueView = Storylines.Views.Dialogs.SaveDialogue;

namespace Storylines.ViewModels;

/// <summary>
/// ViewModel for the SaveDialogue.
/// Manages file name, save location, collision checking, and submit validation.
/// </summary>
public partial class SaveDialogueViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private readonly IProjectPersistenceService _persistence;
    private readonly ProjectState _projectState;
    private readonly IFilePickerService _filePicker;

    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _projectName = string.Empty;
    [ObservableProperty] private string _locationPath = string.Empty;
    [ObservableProperty] private bool _hasLocation;
    [ObservableProperty] private bool _isSubmitEnabled;
    [ObservableProperty] private Visibility _isCollisionWarningVisible = Visibility.Collapsed;
    [ObservableProperty] private int _selectedExtensionIndex;

    public ObservableCollection<string> Extensions { get; } = new();
    public StorageFolder SaveFolder { get; private set; }

    public SaveDialogueView.Type DialogType { get; }
    private int _collisionCheckVersion;

    public SaveDialogueViewModel(
        ILogger logger,
        IProjectPersistenceService persistence,
        ProjectState projectState,
        IFilePickerService filePicker,
        SaveDialogueView.Type dialogType)
    {
        _logger = logger;
        _persistence = persistence;
        _projectState = projectState;
        _filePicker = filePicker;
        DialogType = dialogType;

        Extensions.Add(".srl");

        if (_projectState.Chapters.Count <= 1 && _projectState.Characters.Count == 0)
            Extensions.Add(".txt");

        SelectedExtensionIndex = 0;
    }

    public bool IsExtensionSelectionEnabled =>
        _projectState.Chapters.Count <= 1 && _projectState.Characters.Count == 0;

    partial void OnFileNameChanged(string value) => _ = ValidateAsync(true);
    partial void OnSelectedExtensionIndexChanged(int value) => _ = ValidateAsync(true);

    [RelayCommand]
    private async Task ChooseSaveLocationAsync()
    {
        StorageFolder folder = await _filePicker.PickFolderAsync();

        if (folder is not null)
        {
            SaveFolder = folder;
            LocationPath = folder.Path;
            HasLocation = true;
            await ValidateAsync(true);
        }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (_persistence.CurrentProject is null)
            _persistence.CurrentProject = new ProjectFile();

        _persistence.CurrentProject.projectName = ProjectName;

        var extension = SelectedExtensionIndex >= 0 && SelectedExtensionIndex < Extensions.Count
            ? Extensions[SelectedExtensionIndex]
            : ".srl";

        await _persistence.NewFileAsync(SaveFolder, $"{FileName}{extension}");
    }

    [RelayCommand]
    private void Cancel()
    {
        _persistence.CancelPendingAfterSaveAction();
    }

    private async Task ValidateAsync(bool checkCollision)
    {
        IsSubmitEnabled = SaveFolder is not null && SettingsValues.IsStringSaveable(FileName);

        if (!checkCollision || SaveFolder is null || string.IsNullOrEmpty(FileName))
        {
            IsCollisionWarningVisible = Visibility.Collapsed;
            return;
        }

        var version = ++_collisionCheckVersion;

        try
        {
            var extension = SelectedExtensionIndex >= 0 && SelectedExtensionIndex < Extensions.Count
                ? Extensions[SelectedExtensionIndex]
                : ".srl";

            var file = await SaveFolder.TryGetItemAsync($"{FileName}{extension}");

            if (version != _collisionCheckVersion) return;
            IsCollisionWarningVisible = file is not null ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (version == _collisionCheckVersion)
                IsCollisionWarningVisible = Visibility.Collapsed;
            _logger?.Warning($"Failed to check for file collision: {ex.Message}");
        }
    }
}
