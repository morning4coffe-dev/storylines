
namespace Storylines.Views.Dialogs;

public class AppContentDialog : ContentDialog
{
    private readonly WindowContext _windowContext;
    private int _openTransientCount;
    private bool _isPointerInside;
    private bool _outsideTapSubscribed;

    public AppContentDialog()
    {
        _windowContext = App.TryGetService<WindowContext>();

        Opened += OnManagedDialogOpened;
        Closed += OnManagedDialogClosed;
        PointerEntered += OnDialogPointerEntered;
        PointerExited += OnDialogPointerExited;
    }

    public bool CloseOnOutsideTap { get; set; }

    protected bool HasOpenTransientElements => _openTransientCount > 0;

    protected void NotifyTransientOpened()
    {
        _openTransientCount++;
    }

    protected void NotifyTransientClosed()
    {
        _openTransientCount = Math.Max(0, _openTransientCount - 1);
    }

    protected virtual bool CanCloseOnOutsideTap() => !HasOpenTransientElements;

    private void OnManagedDialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        if (!CloseOnOutsideTap || _outsideTapSubscribed || _windowContext?.RootElement is null)
            return;

        _windowContext.RootElement.PointerPressed += OnRootPointerPressed;
        _outsideTapSubscribed = true;
    }

    private void OnManagedDialogClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        if (!_outsideTapSubscribed || _windowContext?.RootElement is null)
            return;

        _windowContext.RootElement.PointerPressed -= OnRootPointerPressed;
        _outsideTapSubscribed = false;
        _openTransientCount = 0;
        _isPointerInside = false;
    }

    private void OnDialogPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerInside = true;
    }

    private void OnDialogPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerInside = false;
    }

    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerInside && CanCloseOnOutsideTap())
            Hide();
    }
}
