using Microsoft.Extensions.DependencyInjection;
using Storylines.WinUI.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Storylines.WinUI.Views
{
    public sealed partial class CharactersPage : Page
    {
        public CharactersPage()
        {
            this.InitializeComponent();
        }

        public CharactersViewModel ViewModel => App.Current.Services.GetService<CharactersViewModel>();
    }
}
