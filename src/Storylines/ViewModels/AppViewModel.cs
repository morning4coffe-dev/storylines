using Windows.ApplicationModel;

namespace Storylines.ViewModels;

public partial class AppViewModel : ObservableObject
{
    public string SearchText { get; }

    [ObservableProperty]
    private string _titleText;

    [ObservableProperty]
    private string _unsavedIndicatorText;

    [ObservableProperty]
    private Visibility _unsavedIndicatorVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool _isBackButtonVisible;

    [ObservableProperty]
    private bool _isBackButtonEnabled;

    [ObservableProperty]
    private Visibility _titleBarSearchVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private AppPages _currentPage;

    public enum AppPages { Settings, Characters, MainPage }

    private readonly string _editedString;
    private readonly IProjectPersistenceService _persistence;
    private readonly IUndoRedoService _undoRedo;

    public AppViewModel(EventAggregator events, IProjectPersistenceService persistence, IUndoRedoService undoRedo)
    {
        var resources = ResourceLoader.GetForViewIndependentUse();
        _persistence = persistence;
        _undoRedo = undoRedo;
        _editedString = resources.GetString("appHeaderEdited");
        SearchText = resources.GetString("shortcutSearch");
        events.Subscribe<TitleBarUpdateEvent>(_ => UpdateTitleBar());
        events.Subscribe<SettingChangedEvent>(OnSettingChanged);

        UpdateExperimentalVisibility();
        UpdateTitleBar();
    }

    private void OnSettingChanged(SettingChangedEvent e)
    {
        if (e.SettingKey == SettingsValueStrings.ExperimentalFeaturesEnabled)
            UpdateExperimentalVisibility();
    }

    private void UpdateExperimentalVisibility()
    {
        TitleBarSearchVisibility = SettingsValues.experimentalFeaturesEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public void UpdateTitleBar()
    {
        if (CurrentPage == AppPages.Settings)
        {
            var version = Package.Current.Id.Version;
            TitleText =
                $"{Package.Current.DisplayName}" +
                $" {version.Major}.{version.Minor}{(version.Build.ToString().Equals("0") ? string.Empty : $".{version.Build}")}{(version.Revision.ToString().Equals("0") ? string.Empty : $".{version.Revision}")}" +
                $"{(Package.Current.IsDevelopmentMode ? " Dev" : " Preview")}";
        }
        else
        {
            var name = GetProjectName();
            TitleText = name ?? Package.Current.DisplayName;
        }

        UnsavedIndicatorText = _undoRedo.IsDirty ? _editedString : string.Empty;
        UnsavedIndicatorVisibility = _undoRedo.IsDirty ? Visibility.Visible : Visibility.Collapsed;
    }

    public string GetProjectName()
    {
        if (_persistence?.CurrentProject is not null)
        {
            if (!string.IsNullOrEmpty(_persistence.CurrentProject.projectName))
                return _persistence.CurrentProject.projectName;
            else
                return _persistence.CurrentProject.Name;
        }
        return null;
    }

    public void UpdateBackButtonState(bool canGoBack, bool hasModeActive)
    {
        bool canUseBack = canGoBack || hasModeActive;
        IsBackButtonVisible = canUseBack;
        IsBackButtonEnabled = canUseBack;
    }
}
