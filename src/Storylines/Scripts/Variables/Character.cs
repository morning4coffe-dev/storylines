using CommunityToolkit.Mvvm.ComponentModel;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using Storylines.Scripts.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Storylines.Scripts.Variables
{
    public partial class Character : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private CharacterPicture _picture;

        [ObservableProperty]
        private string _role;

        [ObservableProperty]
        private string _age;

        [ObservableProperty]
        private string _appearance;

        partial void OnNameChanged(string value) => OnPropertyChanged(nameof(DetailsLine));
        partial void OnDescriptionChanged(string value) => OnPropertyChanged(nameof(DetailsLine));
        partial void OnRoleChanged(string value) => OnPropertyChanged(nameof(DetailsLine));
        partial void OnAgeChanged(string value) => OnPropertyChanged(nameof(DetailsLine));

        private List<string> _traits = new List<string>();
        public List<string> Traits
        {
            get => _traits;
            set
            {
                if (SetProperty(ref _traits, value ?? new List<string>()))
                {
                    OnPropertyChanged(nameof(TraitsText));
                    OnPropertyChanged(nameof(DetailsLine));
                }
            }
        }

        public string TraitsText
        {
            get => Traits == null || Traits.Count == 0 ? string.Empty : string.Join(", ", Traits);
            set
            {
                Traits = (value ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                OnPropertyChanged();
            }
        }

        public string DetailsLine
        {
            get
            {
                var details = new List<string>();

                if (!string.IsNullOrWhiteSpace(Role))
                    details.Add(Role);

                if (!string.IsNullOrWhiteSpace(Age))
                    details.Add(Age);

                if (Traits != null && Traits.Count > 0)
                    details.Add(string.Join(", ", Traits.Take(2)) + (Traits.Count > 2 ? "…" : string.Empty));

                if (details.Count > 0)
                    return string.Join(" · ", details);

                return Description;
            }
        }

        public string Token { get; private set; }

        public void SetToken(string token)
        {
            Token = token;
        }

        public static async Task<BitmapImage> LoadProfilePictureAsync(CharacterPicture cp)
        {
            try
            {
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);
                var file = await folder.TryGetItemAsync(cp.FileName);

                if (file != null)
                    return new BitmapImage(new Uri(file.Path)) { DecodePixelHeight = LayoutConstants.ProfilePictureDecodeSize, DecodePixelWidth = LayoutConstants.ProfilePictureDecodeSize };
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger.Error($"Failed to load profile picture: {cp.FileName}", ex);
            }

            NotificationManager.DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, ResourceLoader.GetForViewIndependentUse().GetString("picturesNotFound"), "");
            return null;
        }
    }

    public class CharacterPicture
    {
        public string LocalFilePath { set; get; }
        public string FileName { set; get; }
        public BitmapImage Image { set; get; }
    }
}
