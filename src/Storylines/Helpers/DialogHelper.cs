using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Storylines.Services;

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
                var windowContext = App.TryGetService<WindowContext>();
                if (windowContext?.XamlRoot != null)
                    dialog.XamlRoot = windowContext.XamlRoot;
            }
            catch
            {
                // Swallow exceptions to avoid breaking dialog flow
            }
        }
    }
}
