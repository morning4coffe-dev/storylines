using System.ComponentModel;

namespace Storylines.ViewModels;

public partial class ExportDialogViewModel : ObservableObject
{
    private readonly ProjectState _projectState;
    private readonly IProjectPersistenceService _persistence;
    private readonly IExportService _exportService;
    private readonly IFilePickerService _filePickerService;
    private readonly ILogger _logger;
    private readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();

    private StorageFolder _selectedFolder;
    private int _nameCollisionCheckVersion;

    public ObservableCollection<ExportSelectionItemViewModel> PrimarySelections { get; } = new ObservableCollection<ExportSelectionItemViewModel>();
    public ObservableCollection<ExportSelectionItemViewModel> DialogueCharacterSelections { get; } = new ObservableCollection<ExportSelectionItemViewModel>();
    public ObservableCollection<ExportFormatOptionViewModel> AvailableFormats { get; } = new ObservableCollection<ExportFormatOptionViewModel>();

    [ObservableProperty]
    private Visibility _detailsVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _primarySelectionVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _dialogueCharacterSelectionVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _includeChapterNameVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _locationTextVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _locationPlaceholderVisibility = Visibility.Visible;

    [ObservableProperty]
    private Visibility _nameCollisionVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _errorVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool _isErrorOpen;

    [ObservableProperty]
    private InfoBarSeverity _errorSeverity = InfoBarSeverity.Error;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _locationPath = string.Empty;

    [ObservableProperty]
    private string _primarySelectionHeader = Storylines.Resources.ExportDialogue.ChaptersToExport;

    [ObservableProperty]
    private string _primarySelectionSummary = Storylines.Resources.ExportDialogue.All;

    [ObservableProperty]
    private string _dialogueCharacterSelectionSummary = Storylines.Resources.ExportDialogue.All;

    [ObservableProperty]
    private bool _includeChapterName = true;

    [ObservableProperty]
    private bool _canSubmit;

    [ObservableProperty]
    private bool _isSubmitting;

    [ObservableProperty]
    private bool _isChaptersSelected;

    [ObservableProperty]
    private bool _isDialoguesSelected;

    [ObservableProperty]
    private bool _isCharactersSelected;

    [ObservableProperty]
    private ExportTarget _selectedTarget;

    [ObservableProperty]
    private ExportFormatOptionViewModel _selectedFormat;

    public ExportDialogViewModel(
        ProjectState projectState,
        IProjectPersistenceService persistence,
        IExportService exportService,
        IFilePickerService filePickerService,
        ILogger logger)
    {
        _projectState = projectState;
        _persistence = persistence;
        _exportService = exportService;
        _filePickerService = filePickerService;
        _logger = logger;
    }

    public void Initialize(ExportTarget initialTarget)
    {
        _selectedFolder = null;
        _nameCollisionCheckVersion = 0;

        FileName = BuildDefaultFileName();
        LocationPath = string.Empty;
        LocationTextVisibility = Visibility.Collapsed;
        LocationPlaceholderVisibility = Visibility.Visible;

        IncludeChapterName = true;
        ClearError();
        NameCollisionVisibility = Visibility.Collapsed;
        IsSubmitting = false;

        SelectTarget(initialTarget);
        if (initialTarget == ExportTarget.None)
            ResetTargetState();
    }

    partial void OnFileNameChanged(string value)
    {
        ClearError();
        RecalculateState();
        _ = UpdateNameCollisionWarningAsync();
    }

    partial void OnSelectedFormatChanged(ExportFormatOptionViewModel value)
    {
        ClearError();
        RecalculateState();
        _ = UpdateNameCollisionWarningAsync();
    }

    partial void OnIsSubmittingChanged(bool value)
    {
        RecalculateState();
    }

    public void SelectTarget(ExportTarget target)
    {
        if (target == ExportTarget.None)
        {
            ResetTargetState();
            return;
        }

        var capability = _exportService?.GetCapability(target);
        if (capability is null)
        {
            ResetTargetState();
            return;
        }

        SelectedTarget = target;

        IsChaptersSelected = target == ExportTarget.Chapters;
        IsDialoguesSelected = target == ExportTarget.Dialogues;
        IsCharactersSelected = target == ExportTarget.Characters;

        DetailsVisibility = Visibility.Visible;
        PrimarySelectionVisibility = capability.PrimarySelectionKind == ExportSelectionKind.None
            ? Visibility.Collapsed
            : Visibility.Visible;
        DialogueCharacterSelectionVisibility = capability.ShowsSecondaryCharacterFilter
            ? Visibility.Visible
            : Visibility.Collapsed;
        IncludeChapterNameVisibility = capability.SupportsIncludeChapterName
            ? Visibility.Visible
            : Visibility.Collapsed;

        PrimarySelectionHeader = !string.IsNullOrWhiteSpace(capability.PrimarySelectionLabelResourceKey)
            ? (_resources.GetString(capability.PrimarySelectionLabelResourceKey) ?? Storylines.Resources.ExportDialogue.ChaptersToExport)
            : string.Empty;

        LoadFormats(capability.Formats);
        LoadPrimarySelections(capability.PrimarySelectionKind);
        LoadDialogueCharacterSelections(capability.ShowsSecondaryCharacterFilter);

        ClearError();
        RecalculateState();
        _ = UpdateNameCollisionWarningAsync();
    }

    public async Task PickFolderAsync()
    {
        try
        {
            var folder = await _filePickerService.PickFolderAsync();
            if (folder is null)
                return;

            _selectedFolder = folder;
            LocationPath = folder.Path;
            LocationTextVisibility = Visibility.Visible;
            LocationPlaceholderVisibility = Visibility.Collapsed;

            ClearError();
            RecalculateState();
            await UpdateNameCollisionWarningAsync();
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to pick export folder: {ex.Message}");
            ShowError("exportFailedGeneric");
        }
    }

    public async Task<bool> SubmitAsync()
    {
        if (!CanSubmit || SelectedFormat is null)
            return false;

        IsSubmitting = true;
        ClearError();

        try
        {
            var selection = ExportSelectionBuilder.Build(
                SelectedTarget,
                PrimarySelections.Select(item => item.ToSelectionState()),
                DialogueCharacterSelections.Select(item => item.ToSelectionState()));

            var request = new ExportRequest
            {
                Target = SelectedTarget,
                FormatId = SelectedFormat.Definition.Id,
                Folder = _selectedFolder,
                FileName = FileName,
                ChapterIndexes = selection.ChapterIndexes,
                CharacterTokens = selection.CharacterIds,
                DialogueCharacterTokens = selection.DialogueCharacterIds,
                IncludeChapterName = IncludeChapterName,
            };

            var result = await _exportService.ExportAsync(request);
            if (!result.Succeeded)
            {
                ShowError(result.ErrorResourceKey, result.NotificationSeverity);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to submit export", ex);
            ShowError("exportFailedGeneric");
            return false;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void ResetTargetState()
    {
        SelectedTarget = ExportTarget.None;
        IsChaptersSelected = false;
        IsDialoguesSelected = false;
        IsCharactersSelected = false;

        DetailsVisibility = Visibility.Collapsed;
        PrimarySelectionVisibility = Visibility.Collapsed;
        DialogueCharacterSelectionVisibility = Visibility.Collapsed;
        IncludeChapterNameVisibility = Visibility.Collapsed;
        NameCollisionVisibility = Visibility.Collapsed;

        ClearSelections(PrimarySelections);
        ClearSelections(DialogueCharacterSelections);
        AvailableFormats.Clear();
        SelectedFormat = null;
        PrimarySelectionSummary = Storylines.Resources.ExportDialogue.All;
        DialogueCharacterSelectionSummary = Storylines.Resources.ExportDialogue.All;
        ClearError();

        RecalculateState();
    }

    private void LoadFormats(IReadOnlyList<ExportFormatDefinition> formats)
    {
        AvailableFormats.Clear();
        foreach (var format in formats ?? Array.Empty<ExportFormatDefinition>())
            AvailableFormats.Add(new ExportFormatOptionViewModel(format));

        SelectedFormat = AvailableFormats.FirstOrDefault();
    }

    private void LoadPrimarySelections(ExportSelectionKind selectionKind)
    {
        IEnumerable<ExportSelectionItemViewModel> items = selectionKind switch
        {
            ExportSelectionKind.Chapters => _projectState.Chapters.Select((chapter, index) =>
                new ExportSelectionItemViewModel(chapter?.Token ?? index.ToString(), chapter?.Name ?? string.Empty, index)),

            ExportSelectionKind.Characters => _projectState.Characters.Select(character =>
                new ExportSelectionItemViewModel(character?.Token ?? string.Empty, character?.Name ?? string.Empty)),

            _ => Array.Empty<ExportSelectionItemViewModel>(),
        };

        SetSelections(PrimarySelections, items);
        UpdateSelectionSummaries();
    }

    private void LoadDialogueCharacterSelections(bool isVisible)
    {
        if (!isVisible)
        {
            ClearSelections(DialogueCharacterSelections);
            DialogueCharacterSelectionSummary = Storylines.Resources.ExportDialogue.All;
            return;
        }

        var items = _projectState.Characters.Select(character =>
            new ExportSelectionItemViewModel(character?.Token ?? string.Empty, character?.Name ?? string.Empty));

        SetSelections(DialogueCharacterSelections, items);
        UpdateSelectionSummaries();
    }

    private void SetSelections(ObservableCollection<ExportSelectionItemViewModel> collection, IEnumerable<ExportSelectionItemViewModel> items)
    {
        ClearSelections(collection);

        foreach (var item in items)
        {
            item.PropertyChanged += OnSelectionItemChanged;
            collection.Add(item);
        }
    }

    private void ClearSelections(ObservableCollection<ExportSelectionItemViewModel> collection)
    {
        foreach (var item in collection)
            item.PropertyChanged -= OnSelectionItemChanged;

        collection.Clear();
    }

    private void OnSelectionItemChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExportSelectionItemViewModel.IsSelected))
            return;

        ClearError();
        UpdateSelectionSummaries();
        RecalculateState();
    }

    private void UpdateSelectionSummaries()
    {
        PrimarySelectionSummary = BuildSelectionSummary(PrimarySelections);
        DialogueCharacterSelectionSummary = BuildSelectionSummary(DialogueCharacterSelections);
    }

    private string BuildSelectionSummary(IEnumerable<ExportSelectionItemViewModel> selections)
    {
        var selectedItems = (selections ?? Enumerable.Empty<ExportSelectionItemViewModel>())
            .Where(item => item.IsSelected)
            .Select(item => item.DisplayName)
            .Where(displayName => !string.IsNullOrWhiteSpace(displayName))
            .ToArray();

        if (selectedItems.Length == 0)
            return Storylines.Resources.ExportDialogue.None;

        var totalItems = selections?.Count() ?? 0;
        if (selectedItems.Length == totalItems)
            return Storylines.Resources.ExportDialogue.All;

        return string.Join(", ", selectedItems);
    }

    private void RecalculateState()
    {
        var hasValidName = !string.IsNullOrWhiteSpace(FileName) && SettingsValues.IsStringSaveable(FileName);
        var hasFolder = _selectedFolder is not null;
        var hasFormat = SelectedFormat is not null;
        var hasTarget = SelectedTarget != ExportTarget.None;
        var hasPrimarySelection = PrimarySelectionVisibility != Visibility.Visible || PrimarySelections.Any(item => item.IsSelected);
        var hasDialogueCharacterSelection = DialogueCharacterSelectionVisibility != Visibility.Visible || DialogueCharacterSelections.Any(item => item.IsSelected);

        CanSubmit = !IsSubmitting
            && hasTarget
            && hasValidName
            && hasFolder
            && hasFormat
            && hasPrimarySelection
            && hasDialogueCharacterSelection;
    }

    private async Task UpdateNameCollisionWarningAsync()
    {
        var requestVersion = ++_nameCollisionCheckVersion;
        var folder = _selectedFolder;
        var fileName = FileName;
        var extension = SelectedFormat?.Definition?.DefaultExtension;

        if (folder is null || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(extension))
        {
            NameCollisionVisibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var file = await folder.TryGetItemAsync(fileName + extension);
            if (requestVersion != _nameCollisionCheckVersion)
                return;

            NameCollisionVisibility = file is not null ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (requestVersion == _nameCollisionCheckVersion)
                NameCollisionVisibility = Visibility.Collapsed;

            _logger?.Warning($"Failed to check export file collision: {ex.Message}");
        }
    }

    public void DismissError()
    {
        IsErrorOpen = false;
        ErrorVisibility = Visibility.Collapsed;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        ErrorSeverity = InfoBarSeverity.Error;
        DismissError();
    }

    private void ShowError(string resourceKey, InfoBarSeverity severity = InfoBarSeverity.Error)
    {
        var message = !string.IsNullOrWhiteSpace(resourceKey)
            ? _resources.GetString(resourceKey)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(message))
            message = _resources.GetString("exportFailedGeneric") ?? "Export failed.";

        ErrorMessage = message;
        ErrorSeverity = severity;
        IsErrorOpen = true;
        ErrorVisibility = Visibility.Visible;
    }

    private string BuildDefaultFileName()
    {
        var projectDisplayName = _persistence?.CurrentProject?.file is not null
            ? _persistence.CurrentProject.file.DisplayName
            : "my-story";

        return $"{projectDisplayName}-{Storylines.Resources.ExportDialogue.Title.ToLower()}";
    }
}

public sealed partial class ExportSelectionItemViewModel : ObservableObject
{
    public ExportSelectionItemViewModel(string id, string displayName, int? index = null)
    {
        Id = id;
        DisplayName = displayName;
        Index = index;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public int? Index { get; }

    [ObservableProperty]
    private bool _isSelected = true;

    public ExportSelectionState ToSelectionState() => new ExportSelectionState(Id, IsSelected, Index);
}

public sealed class ExportFormatOptionViewModel
{
    public ExportFormatOptionViewModel(ExportFormatDefinition definition)
    {
        Definition = definition;
    }

    public ExportFormatDefinition Definition { get; }
    public string DisplayExtension => Definition.DefaultExtension;
}