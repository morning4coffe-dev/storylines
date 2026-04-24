using Storylines.Views.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;

namespace Storylines.Models
{
    public class ProjectFile : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Token { get; private set; }
        public string Path { get; set; }
        public StorageFile file { get; set; }

        public string projectName { get; set; }
        public string projectVersion { get; set; }

        public Uri Icon { get; set; }
        public string ShortPath { get; set; }
        public string LastEditedFormatted { get; private set; }
        public DateTimeOffset LastEdited { get; private set; }

        public Windows.UI.Xaml.Thickness osMargin { get; private set; } = LoadProjectDialogue.osMargin;
        public double osWidth { get; private set; } = LoadProjectDialogue.osWidth;

        public static ObservableCollection<ProjectFile> projectFiles = new ObservableCollection<ProjectFile>();

        public event PropertyChangedEventHandler PropertyChanged;

        public static void New(StorageFile file)
        {
            _ = Remember(file);
        }

        public static async Task<ProjectFile> LoadExistingAsync(StorageFile file, string token)
        {
            BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            return new ProjectFile()
            {
                Name = file.Name,
                Path = file.Path,
                Token = token,
                file = file,
                Icon = new Uri(file.FileType == ".txt" ? "ms-appx:/Assets/Icons/Text-document-icon.png" : "ms-appx:/Assets/Icons/Storylines-document-icon.png"),
                ShortPath = file.Path.Replace(@"\" + file.Name, string.Empty).Replace(@"\", "/"),
                LastEditedFormatted = basicProperties.DateModified.ToString("g", Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.Culture),
                LastEdited = basicProperties.DateModified
            };
        }

        private static string Remember(StorageFile file)
        {
            string token = Guid.NewGuid().ToString();
            if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= StorageApplicationPermissions.FutureAccessList.MaximumItemsAllowed)
                StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries[0].Token);

            StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, file);
            return token;
        }

        public static void Remove(string token)
        {
            for (int i = 0; i < projectFiles.Count; i++)
            {
                if (projectFiles[i].Token == token)
                {
                    projectFiles.RemoveAt(i);
                    StorageApplicationPermissions.FutureAccessList.Remove(token);
                    return;
                }
            }
        }

        public static async Task LoadAllAsync()
        {
            projectFiles.Clear();

            foreach (AccessListEntry token in StorageApplicationPermissions.FutureAccessList.Entries)
            {
                Task<StorageFile> task = GetProjectFromTokenAsync(token.Token);

                if (await Task.WhenAny(task, Task.Delay(1000)) == task)
                {
                    StorageFile file = task.Result;
                    projectFiles.Add(await LoadExistingAsync(file, token.Token));
                }
                else
                    StorageApplicationPermissions.FutureAccessList.Remove(token.Token);
            }
        }

        public static async Task<StorageFile> GetProjectFromTokenAsync(string token)
        {
            if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
                return null;
            return await StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
        }

        public static bool CheckIfProjectExists(StorageFile file)
        {
            for (int i = 0; i < projectFiles.Count; i++)
            {
                if (projectFiles[i].Path == file.Path)
                {
                    return true;
                }
            }
            return false;
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
