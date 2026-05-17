
namespace Storylines.Services.Interfaces;

    public interface INavigationService
    {
        void Initialize(Frame frame);
        void NavigateTo(NavigationTarget target, object parameter = null);
        void GoBack();
        bool CanGoBack { get; }

        event Action<NavigationTarget> Navigated;
    }

    public enum NavigationTarget
    {
        MainPage,
        Characters,
        Settings,
#if PRIVATE_PLUGINS
        BranchingDialogue,
#endif
        Pinboard
    }
