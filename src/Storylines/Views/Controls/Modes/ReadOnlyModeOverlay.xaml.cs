using Storylines.Services.Modes;

namespace Storylines.Views.Controls.Modes;

public sealed partial class ReadOnlyModeOverlay : UserControl
{
    private readonly WindowContext _windowContext;

    public ReadOnlyModeOverlay()
    {
        _windowContext = App.GetService<WindowContext>();
        InitializeComponent();

        var resources = ResourceLoader.GetForViewIndependentUse();
        modeTitleText.Text = resources.GetString("modeReadOnly.Text");
        modeDescriptionText.Text = resources.GetString("modeReadOnlyDescription");
    }

    private void OnExitPill_Click(object sender, RoutedEventArgs e)
    {
        _windowContext?.AppView?.TryExitActiveMode();
    }
}
