using System;

namespace Storylines.Scripts.Services.Interfaces
{
    public interface INavigationService
    {
        void NavigateTo(NavigationTarget target);
        void GoBack();
        bool CanGoBack { get; }

        event Action<NavigationTarget> Navigated;
    }

    public enum NavigationTarget
    {
        MainPage,
        Characters,
        Settings
    }
}
