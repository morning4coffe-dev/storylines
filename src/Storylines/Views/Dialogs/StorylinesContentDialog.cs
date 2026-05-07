using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Storylines.Services;

namespace Storylines.Views.Dialogs
{
    public class StorylinesContentDialog : ContentDialog
    {
        private readonly WindowContext _windowContext;
        private bool _isPointerInside;
        private bool _outsideTapSubscribed;

        public StorylinesContentDialog()
        {
            _windowContext = App.TryGetService<WindowContext>();

            Opened += OnManagedDialogOpened;
            Closed += OnManagedDialogClosed;
            PointerEntered += OnDialogPointerEntered;
            PointerExited += OnDialogPointerExited;
        }

        public bool CloseOnOutsideTap { get; set; }

        protected virtual bool CanCloseOnOutsideTap() => true;

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
}