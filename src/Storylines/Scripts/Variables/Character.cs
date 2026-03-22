using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Storylines.Scripts.Variables
{
    public class Character : INotifyPropertyChanged
    {
        private string _name;
        public string name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }
        public string token { get; private set; }
        private string _description;
        public string description
        {
            get { return _description; }
            set
            {
                _description = value;
                NotifyPropertyChanged();
            }
        }
        private CharacterPicture _picture;
        public CharacterPicture picture
        {
            get { return _picture; }
            set
            {
                _picture = value;
                NotifyPropertyChanged();
            }
        }

        private string _role;
        public string role
        {
            get { return _role; }
            set
            {
                _role = value;
                NotifyPropertyChanged();
            }
        }

        private string _age;
        public string age
        {
            get { return _age; }
            set
            {
                _age = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetToken(string token)
        {
            this.token = token;
        }

        public static async Task<BitmapImage> LoadProfilePictureAsync(CharacterPicture cp)
        {
            try
            {
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);
                var file = await folder.TryGetItemAsync(cp.fileName);

                if (file != null)
                    return new BitmapImage(new Uri(file.Path)) { DecodePixelHeight = Constants.LayoutConstants.ProfilePictureDecodeSize, DecodePixelWidth = Constants.LayoutConstants.ProfilePictureDecodeSize };
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger.Error($"Failed to load profile picture: {cp.fileName}", ex);
            }

            NotificationManager.DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, ResourceLoader.GetForCurrentView().GetString("picturesNotFound"), "");
            return null;
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CharacterPicture
    {
        public string localFilePath { set; get; }
        public string fileName { set; get; }
        public BitmapImage image { set; get; }
    }
}
