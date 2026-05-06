using Microsoft.UI.Xaml.Controls;

namespace Storylines.Services
{
    public sealed class DialogDefinition
    {
        public string Title { get; init; }
        public object Content { get; init; }
        public string PrimaryButtonText { get; init; }
        public string SecondaryButtonText { get; init; }
        public string CloseButtonText { get; init; }
        public ContentDialogButton DefaultButton { get; init; } = ContentDialogButton.Close;
        public bool IsPrimaryButtonEnabled { get; init; } = true;
    }
}