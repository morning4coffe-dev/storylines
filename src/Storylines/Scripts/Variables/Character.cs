using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using Storylines.Scripts.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
                NotifyPropertyChanged(nameof(detailsLine));
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
                NotifyPropertyChanged(nameof(detailsLine));
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
                NotifyPropertyChanged(nameof(detailsLine));
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
                NotifyPropertyChanged(nameof(detailsLine));
            }
        }

        private string _appearance;
        public string appearance
        {
            get { return _appearance; }
            set
            {
                _appearance = value;
                NotifyPropertyChanged();
            }
        }

        private List<string> _traits = new List<string>();
        public List<string> traits
        {
            get { return _traits; }
            set
            {
                _traits = value ?? new List<string>();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(traitsText));
                NotifyPropertyChanged(nameof(detailsLine));
            }
        }

        public string traitsText
        {
            get { return traits == null || traits.Count == 0 ? string.Empty : string.Join(", ", traits); }
            set
            {
                traits = (value ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                NotifyPropertyChanged();
            }
        }

        public string detailsLine
        {
            get
            {
                var details = new List<string>();

                if (!string.IsNullOrWhiteSpace(role))
                    details.Add(role);

                if (!string.IsNullOrWhiteSpace(age))
                    details.Add(age);

                if (traits != null && traits.Count > 0)
                    details.Add(string.Join(", ", traits.Take(2)) + (traits.Count > 2 ? "…" : string.Empty));

                if (details.Count > 0)
                    return string.Join(" · ", details);

                return description;
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
                    return new BitmapImage(new Uri(file.Path)) { DecodePixelHeight = LayoutConstants.ProfilePictureDecodeSize, DecodePixelWidth = LayoutConstants.ProfilePictureDecodeSize };
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
