using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Storylines.Helpers
{
    public static class DialogHelper
    {
        public static void EnsureXamlRoot(ContentDialog dialog)
        {
            try
            {
                if (dialog == null) return;
                if (dialog.XamlRoot != null) return;
                var root = App.MainWindow?.Content as FrameworkElement;
                if (root?.XamlRoot != null)
                    dialog.XamlRoot = root.XamlRoot;
            }
            catch
            {
                // Swallow exceptions to avoid breaking dialog flow
            }
        }
    }
}
