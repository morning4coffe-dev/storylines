using Microsoft.Extensions.DependencyInjection;
using Storylines.WinUI.ViewModels;
using System;
using Windows.UI.Xaml.Controls;

namespace Storylines.WinUI.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            ViewModel.Navigate += (Type page) => contentFrame.Navigate(page);
        }

        public SettingsViewModel ViewModel => App.Current.Services.GetService<SettingsViewModel>();
    }
}
