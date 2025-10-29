using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Storylines.WinUI.Views.SettingsPages;
using System;

namespace Storylines.WinUI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        public Action<Type> Navigate;

        [RelayCommand]
        private void NavigateTo(string tag)
        {
            switch (tag)
            {
                case "General":
                    Navigate?.Invoke(typeof(GeneralPage));
                    break;
                case "Personalize":
                    Navigate?.Invoke(typeof(PersonalizationPage));
                    break;
                case "Accessibility":
                    Navigate?.Invoke(typeof(AccessibilityPage));
                    break;
                case "About":
                    Navigate?.Invoke(typeof(AboutPage));
                    break;
            }
        }
    }
}
