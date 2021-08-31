using Storylines.Scripts.Functions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        //age?, relatives?, abilities?, gender?

        public event PropertyChangedEventHandler PropertyChanged;

        public static ObservableCollection<Character> characters = new ObservableCollection<Character>();

        public static void AddExisting(string name, string token, string description, CharacterPicture picture)
        {
            Character ch = new Character()
            {
                name = name,
                token = token,
                description = description,
            };

            if (picture != null)
                if (picture.fileName != null && picture.fileName.Length > 0)
                    ch.picture = new CharacterPicture() { fileName = picture.fileName, image = ProfilePictureFromCharacterPictureAsync(picture) };
                else
                    ch.picture = new CharacterPicture();

            characters.Add(ch);
        }

        public static Character CreateNew(string name, string description)
        {
            Character ch = new Character() { name = name, token = Guid.NewGuid().ToString(), description = description, picture = new CharacterPicture() };
            characters.Add(ch);
            TimeTravelCharacter.SomethingChanged(TimeTravelCharacter.Changed.Added, ch);
            return ch;
        }

        public static void Remove(string token)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].token == token)
                {
                    TimeTravelCharacter.SomethingChanged(TimeTravelCharacter.Changed.Removed, characters[i]);

                    _ = characters.Remove(characters[i]);
                }
            }
        }

        public static Character Find(string token)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].token == token)
                    return characters[i];
            }
            return null;
        }

        public static int FindID(string token)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].token == token)
                    return i;
            }
            return 0;
        }

        public static Character Copy(string token)
        {
            return (Character)Find(token).MemberwiseClone();
        }

        public void SetToken(string token)
        {
            this.token = token;
        }

        public static BitmapImage ProfilePictureFromCharacterPictureAsync(CharacterPicture cp)
        {
            var folder = ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists).AsTask().GetAwaiter().GetResult();
            var file = folder.TryGetItemAsync(cp.fileName).AsTask().GetAwaiter().GetResult();

            if (file != null)
                return new BitmapImage(new Uri(file.Path)) { DecodePixelHeight = 60, DecodePixelWidth = 60 };
            else
                NotificationManager.DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, ResourceLoader.GetForCurrentView().GetString("picturesNotFound"), "");
                return null;
        }

        public void NotifyPropertyChanged(string propertyName = "")
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
