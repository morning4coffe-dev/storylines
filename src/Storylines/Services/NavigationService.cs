using Storylines.Views.Pages;
using Storylines.Services.Interfaces;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Storylines.Services
{
    public class NavigationService : INavigationService
    {
        private Frame _frame;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public event Action<NavigationTarget> Navigated;

        public void Initialize(Frame frame)
        {
            _frame = frame;
        }

        public void NavigateTo(NavigationTarget target)
        {
            switch (target)
            {
                case NavigationTarget.MainPage:
                    _frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
                    break;
                case NavigationTarget.Characters:
                    _frame.Navigate(typeof(CharactersPage), null, new DrillInNavigationTransitionInfo());
                    break;
                case NavigationTarget.Settings:
                    _frame.Navigate(typeof(SettingsPage));
                    break;
                case NavigationTarget.Pinboard:
                    _frame.Navigate(typeof(StoryPinboardPage), null, new DrillInNavigationTransitionInfo());
                    break;
            }

            Navigated?.Invoke(target);
        }

        public void GoBack()
        {
            if (_frame?.CanGoBack == true)
            {
                _frame.GoBack(new DrillInNavigationTransitionInfo());
            }
        }
    }
}
