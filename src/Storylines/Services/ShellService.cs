
namespace Storylines.Services;

internal sealed class ShellService : IShellService
{
    private readonly WindowContext _windowContext;

    public ShellService(WindowContext windowContext)
    {
        _windowContext = windowContext;
    }

    public object CurrentDialog
    {
        get => _windowContext.CurrentDialog;
        set => _windowContext.CurrentDialog = value as ContentDialog;
    }

    public event Action ShellThemeChanged;

    public void RequestShellFocus()
    {
        _windowContext.AppView?.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
    }

    public void RaiseShellThemeChanged() => ShellThemeChanged?.Invoke();
}
