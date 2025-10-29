using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Storylines.WinUI.Models
{
    public partial class Character : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private CharacterPicture _picture;

        public string Token { get; private set; }

        public void SetToken(string token) => Token = token;
    }

    public class CharacterPicture
    {
        public string FileName { get; set; }
        public BitmapImage Image { get; set; }
    }
}
