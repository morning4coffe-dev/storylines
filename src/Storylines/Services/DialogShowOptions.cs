using Microsoft.UI.Xaml;

namespace Storylines.Services
{
    public sealed class DialogShowOptions
    {
        public static DialogShowOptions Default { get; } = new DialogShowOptions();

        public bool CloseCurrentDialog { get; init; } = true;
        public bool WaitForXamlRoot { get; init; } = true;
        public int XamlRootWaitTimeoutMs { get; init; } = 2000;
        public XamlRoot XamlRootOverride { get; init; }
    }
}