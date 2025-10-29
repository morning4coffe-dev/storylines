using Storylines.WinUI.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Storylines.WinUI.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            ViewModel.PageLoadedCommand.Execute(null);
        }

        public MainViewModel ViewModel => App.Current.Services.GetService<MainViewModel>();
    }
}
