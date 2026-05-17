using System.Globalization;

namespace Storylines.ViewModels.Settings;

public partial class GeneralSettingsViewModel : ObservableObject
{
    private readonly IAppSettingsService _settings;
    private bool _isInitializing;
    private bool _isSynchronizing;

    [ObservableProperty]
    private string _chapterName;

    [ObservableProperty]
    private bool _exitDialogueEnabled;

    [ObservableProperty]
    private bool _loadLastProjectOnStart;

    [ObservableProperty]
    private bool _autosaveEnabled;

    [ObservableProperty]
    private string _autosaveIntervalKey;

    [ObservableProperty]
    private double _dailyWordGoal;

    [ObservableProperty]
    private bool _experimentalFeaturesEnabled;

    public GeneralSettingsViewModel(IAppSettingsService settings)
    {
        _settings = settings;

        _isInitializing = true;
        _chapterName = _settings.ChapterName;
        _exitDialogueEnabled = _settings.ExitDialogueEnabled;
        _loadLastProjectOnStart = _settings.LoadLastProjectOnStart;
        _autosaveEnabled = _settings.AutosaveEnabled;
        _autosaveIntervalKey = _settings.AutosaveInterval.ToString(CultureInfo.InvariantCulture);
        _dailyWordGoal = _settings.DailyWordGoal;
        _experimentalFeaturesEnabled = _settings.ExperimentalFeaturesEnabled;
        _isInitializing = false;
    }

    [RelayCommand]
    private void ResetChapterName()
    {
        _settings.ResetChapterName();
        UpdateSilently(() => ChapterName = _settings.ChapterName);
    }

    partial void OnChapterNameChanged(string value)
    {
        if (ShouldSkipUpdate())
            return;

        _settings.ChapterName = value;
    }

    partial void OnExitDialogueEnabledChanged(bool value)
    {
        if (ShouldSkipUpdate())
            return;

        _settings.ExitDialogueEnabled = value;
    }

    partial void OnLoadLastProjectOnStartChanged(bool value)
    {
        if (ShouldSkipUpdate())
            return;

        _settings.LoadLastProjectOnStart = value;

        if (_settings.LoadLastProjectOnStart != value)
            UpdateSilently(() => LoadLastProjectOnStart = _settings.LoadLastProjectOnStart);
    }

    partial void OnAutosaveEnabledChanged(bool value)
    {
        if (ShouldSkipUpdate())
            return;

        _settings.AutosaveEnabled = value;

        if (_settings.AutosaveEnabled != value)
            UpdateSilently(() => AutosaveEnabled = _settings.AutosaveEnabled);
    }

    partial void OnAutosaveIntervalKeyChanged(string value)
    {
        if (ShouldSkipUpdate())
            return;

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double interval))
            return;

        _settings.AutosaveInterval = interval;

        string normalizedInterval = _settings.AutosaveInterval.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(normalizedInterval, value, StringComparison.Ordinal))
            UpdateSilently(() => AutosaveIntervalKey = normalizedInterval);
    }

    partial void OnDailyWordGoalChanged(double value)
    {
        if (ShouldSkipUpdate() || double.IsNaN(value))
            return;

        _settings.DailyWordGoal = (int)Math.Round(value);
    }

    partial void OnExperimentalFeaturesEnabledChanged(bool value)
    {
        if (ShouldSkipUpdate())
            return;

        _settings.ExperimentalFeaturesEnabled = value;
    }

    private bool ShouldSkipUpdate() => _isInitializing || _isSynchronizing;

    private void UpdateSilently(Action update)
    {
        _isSynchronizing = true;
        update();
        _isSynchronizing = false;
    }
}