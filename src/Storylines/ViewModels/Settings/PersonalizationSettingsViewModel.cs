using Windows.UI;

namespace Storylines.ViewModels.Settings;

public partial class PersonalizationSettingsViewModel : ObservableObject
{
    private readonly IAppSettingsService _settings;
    private bool _isInitializing;
    private bool _isSynchronizing;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _selectedAccentIndex;

    [ObservableProperty]
    private Color _customAccentColor;

    [ObservableProperty]
    private bool _addChapterOnPageDownEnabled;

    [ObservableProperty]
    private string _editorFontFamily;

    [ObservableProperty]
    private double _editorFontSize;

    [ObservableProperty]
    private double _editorZoomPercent;

    public bool IsCustomAccentSelected => SelectedAccentIndex == (int)SettingsValues.SelectedAccent.Custom;

    public PersonalizationSettingsViewModel(IAppSettingsService settings)
    {
        _settings = settings;

        _isInitializing = true;
        _selectedThemeIndex = (int)_settings.SelectedTheme;
        _selectedAccentIndex = (int)_settings.SelectedAccent;
        _customAccentColor = _settings.CustomAccentColor;
        _addChapterOnPageDownEnabled = _settings.AddChapterOnPageDownEnabled;
        _editorFontFamily = _settings.EditorFontFamily;
        _editorFontSize = _settings.EditorFontSize;
        _editorZoomPercent = _settings.EditorZoom * 4d;
        _isInitializing = false;
    }

    [RelayCommand]
    private void ApplyAccentPreset(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return;

        SelectedAccentIndex = (int)SettingsValues.SelectedAccent.Custom;
        CustomAccentColor = CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(hex);
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (ShouldSkipUpdate())
            return;

        _settings.SelectedTheme = (SettingsValues.SelectedTheme)value;
    }

    partial void OnSelectedAccentIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsCustomAccentSelected));

        if (ShouldSkipUpdate())
            return;

        _settings.SelectedAccent = (SettingsValues.SelectedAccent)value;
    }

    partial void OnCustomAccentColorChanged(Color value)
    {
        if (ShouldSkipUpdate())
            return;

        _settings.CustomAccentColor = value;
    }

    partial void OnAddChapterOnPageDownEnabledChanged(bool value)
    {
        if (ShouldSkipUpdate())
            return;

        _settings.AddChapterOnPageDownEnabled = value;
    }

    partial void OnEditorFontFamilyChanged(string value)
    {
        if (ShouldSkipUpdate())
            return;

        _settings.EditorFontFamily = value;
    }

    partial void OnEditorFontSizeChanged(double value)
    {
        if (ShouldSkipUpdate() || double.IsNaN(value))
            return;

        _settings.EditorFontSize = value;

        if (Math.Abs(_settings.EditorFontSize - value) > double.Epsilon)
            UpdateSilently(() => EditorFontSize = _settings.EditorFontSize);
    }

    partial void OnEditorZoomPercentChanged(double value)
    {
        if (ShouldSkipUpdate() || double.IsNaN(value))
            return;

        _settings.EditorZoom = value / 4d;

        double normalizedZoom = _settings.EditorZoom * 4d;
        if (Math.Abs(normalizedZoom - value) > 0.01d)
            UpdateSilently(() => EditorZoomPercent = normalizedZoom);
    }

    private bool ShouldSkipUpdate() => _isInitializing || _isSynchronizing;

    private void UpdateSilently(Action update)
    {
        _isSynchronizing = true;
        update();
        _isSynchronizing = false;
    }
}