using Microsoft.UI.Xaml.Controls;

namespace Storylines.Scripts.Services.Interfaces
{
    public interface INotificationService
    {
        void ShowNotification(InfoBarSeverity severity, string text, string longText = "");
        void ShowProgressBar(bool isIndeterminate);
        void HideProgressBar();
    }
}
