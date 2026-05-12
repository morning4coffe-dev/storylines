using Storylines.Services.Modes;
using Storylines.ViewModels.Modes;

namespace Storylines.Views.Dialogs;

public sealed partial class ModePickerDialogue : AppShellDialog
{
    private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();
    private enum SelectedMode { Edit, Focus, ReadOnly }
    private SelectedMode _selectedMode = SelectedMode.Focus;

    public ModePickerDialogue(string preselect = "focus")
    {
        InitializeComponent();

        DialogTitle = _resources.GetString("modePickerTitle.Text");
        PrimaryActionText = _resources.GetString("modeEnterButton.Text");
        PrimaryActionGlyph = "\uE70F";

        timePicker.Time = new TimeSpan(0, 20, 0);

        switch (preselect)
        {
            case "edit":
                SelectCard(SelectedMode.Edit);
                break;
            case "readonly":
                SelectCard(SelectedMode.ReadOnly);
                break;
            default: // "focus"
                SelectCard(SelectedMode.Focus);
                break;
        }
    }

    public static void Open(string preselect = "focus")
    {
        _ = OpenAsync(preselect);
    }

    public static Task<ContentDialogResult> OpenAsync(string preselect = "focus")
    {
        return App.GetService<IDialogService>().ShowAsync(new ModePickerDialogue(preselect));
    }

    // ── card selection ────────────────────────────────────────────────────
    private void SelectCard(SelectedMode mode)
    {
        _selectedMode = mode;
        focusOptions.Visibility = mode == SelectedMode.Focus
            ? Visibility.Visible : Visibility.Collapsed;

        UpdateCardHighlight(editCard,     mode == SelectedMode.Edit);
        UpdateCardHighlight(focusCard,    mode == SelectedMode.Focus);
        UpdateCardHighlight(readOnlyCard, mode == SelectedMode.ReadOnly);
    }

    private static void UpdateCardHighlight(Button card, bool selected)
    {
        card.BorderThickness = selected
            ? new Thickness(2)
            : new Thickness(1);
        card.BorderBrush = selected
            ? (Brush)Application.Current.Resources["SystemControlHighlightAccentBrush"]
            : (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
    }

    private void OnEditCard_Click(object sender, RoutedEventArgs e)    => SelectCard(SelectedMode.Edit);
    private void OnFocusCard_Click(object sender, RoutedEventArgs e)   => SelectCard(SelectedMode.Focus);
    private void OnReadOnlyCard_Click(object sender, RoutedEventArgs e)=> SelectCard(SelectedMode.ReadOnly);

    protected override Task<bool> ExecutePrimaryActionAsync()
    {
        var modeService = App.GetService<EditorModeService>();

        switch (_selectedMode)
        {
            case SelectedMode.Edit:
                modeService.Deactivate();
                break;

            case SelectedMode.Focus:
                if ((bool)autosaveCheckBox.IsChecked)
                    App.GetService<IProjectPersistenceService>().EnableAutosave();
                else
                    App.GetService<IProjectPersistenceService>().DisableAutosave();

                var focusMode = new FocusMode(
                    App.GetService<EventAggregator>(),
                    App.GetService<INotificationService>(),
                    App.GetService<WindowContext>())
                {
                    FullScreen    = (bool)fullScreenCheckBox.IsChecked,
                    Time          = timePicker.Time,
                    MeasureTarget = (int)measureValueNumBox.Value,
                    Metric        = (MeasureMetric)toMeasureComboBox.SelectedIndex,
                };
                modeService.Activate(focusMode);

                App.TryGetService<ITelemetryService>()?.TrackFocusModeStarted(
                    (bool)fullScreenCheckBox.IsChecked,
                    (bool)autosaveCheckBox.IsChecked,
                    focusMode.Metric.ToString(),
                    focusMode.MeasureTarget,
                    focusMode.Time);
                break;

            case SelectedMode.ReadOnly:
                modeService.Activate(ReadOnlyMode.Instance);
                break;
        }

        return Task.FromResult(true);
    }

    // ── focus options helpers ─────────────────────────────────────────────
    private void OnAutosaveCheckBox_Click(object sender, RoutedEventArgs e)
        => autosaveTip.IsOpen = !(bool)autosaveCheckBox.IsChecked;

    private void OnAutosaveTipActionButton_Click(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
    {
        autosaveCheckBox.IsChecked = true;
        autosaveTip.IsOpen = false;
    }

    private void OnTimeCheckBox_Click(object sender, RoutedEventArgs e)
    {
        timePicker.Visibility = (bool)timeCheckBox.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        if (!(bool)timeCheckBox.IsChecked)
            timePicker.Time = TimeSpan.Zero;
    }

    private void OnMeasureCheckBox_Click(object sender, RoutedEventArgs e)
    {
        measureValueNumBox.Value = 0;
        measureStack.Visibility = (bool)measureCheckBox.IsChecked ? Visibility.Visible : Visibility.Collapsed;
    }
}
