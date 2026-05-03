using Storylines.Views.Pages;
using Storylines.Services.Interfaces;
using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Storylines.Services
{
    public class NavigationService : INavigationService
    {
        private Frame _frame;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public event Action<NavigationTarget> Navigated;

        public void Initialize(Frame frame)
        {
            if (_frame != null)
                _frame.Navigated -= OnFrameNavigated;

            _frame = frame;

            if (_frame != null)
                _frame.Navigated += OnFrameNavigated;
        }

        public void NavigateTo(NavigationTarget target, object parameter = null)
        {
            if (_frame == null)
                return;

            switch (target)
            {
                case NavigationTarget.MainPage:
                    _frame.Navigate(typeof(MainPage), parameter, new DrillInNavigationTransitionInfo());
                    break;
                case NavigationTarget.Characters:
                    _frame.Navigate(typeof(CharactersPage), parameter, new DrillInNavigationTransitionInfo());
                    break;
                case NavigationTarget.Settings:
                    _frame.Navigate(typeof(SettingsPage), parameter);
                    break;
#if PRIVATE_PLUGINS
                case NavigationTarget.BranchingDialogue:
                    _frame.Navigate(typeof(Storylines.Views.Pages.BranchingDialoguePage), parameter, new DrillInNavigationTransitionInfo());
                    break;
#endif
                case NavigationTarget.Pinboard:
                    _frame.Navigate(typeof(StoryPinboardPage), parameter, new DrillInNavigationTransitionInfo());
                    break;
            }
        }

        public void GoBack()
        {
            if (_frame?.CanGoBack == true)
            {
                _frame.GoBack(new DrillInNavigationTransitionInfo());
            }
        }

        public void ClearFrame()
        {
            if (_frame != null)
                _frame.Navigated -= OnFrameNavigated;

            _frame = null;
        }

        private void OnFrameNavigated(object sender, NavigationEventArgs e)
        {
            if (TryGetNavigationTarget(e.SourcePageType, out var target))
                Navigated?.Invoke(target);
        }

        private static bool TryGetNavigationTarget(Type pageType, out NavigationTarget target)
        {
            if (pageType == typeof(MainPage))
            {
                target = NavigationTarget.MainPage;
                return true;
            }

            if (pageType == typeof(CharactersPage))
            {
                target = NavigationTarget.Characters;
                return true;
            }

            if (pageType == typeof(SettingsPage))
            {
                target = NavigationTarget.Settings;
                return true;
            }

#if PRIVATE_PLUGINS
            if (pageType == typeof(Storylines.Views.Pages.BranchingDialoguePage))
            {
                target = NavigationTarget.BranchingDialogue;
                return true;
            }
#endif

            if (pageType == typeof(StoryPinboardPage))
            {
                target = NavigationTarget.Pinboard;
                return true;
            }

            target = default;
            return false;
        }
    }
}
