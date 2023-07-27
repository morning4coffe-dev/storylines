using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Storylines.Components
{
    public class SaveSystem
    {
        public static Dictionary<string, string> savedValues = new Dictionary<string, string>();

        public static ProjectFile currentProject;

        #region Save
        private enum AfterSave { DoNothing, ClearEverything, Exit };
        private static AfterSave afterSave;

        public static void Save()
        {
            afterSave = AfterSave.DoNothing;

            //System.Runtime.Serialization.Formatters.Binary.BinaryFormatter form = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //form.Serialize(afterSave);

            if (currentProject.file != null)
            {
                if (currentProject.file.FileType == ".srl")
                {
                     WriteToFile(GetSaveValues());
                    MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(true);
                }
                else
                if (currentProject.file.FileType == ".txt")
                {
                    MainPage.ChapterText.textBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out string txt);
                    MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(false);
                    WriteToFile(txt);
                }

                NotificationManager.DisplayMainProgressBar(true);
                TimeTravelSystem.unSavedProgress = false;
            }
            else
                SaveDialogue.Open(SaveDialogue.Type.Save);
        }

        public static void SaveCopy()
        {
            SaveDialogue.Open(SaveDialogue.Type.SaveCopy);
        }

        public static void SaveAndExitOrClearAll(bool exit)
        {
            if (exit)
                afterSave = AfterSave.Exit;
            else
                afterSave = AfterSave.ClearEverything;

            if (currentProject.file != null)
            {
                WriteToFile(GetSaveValues());
                TimeTravelSystem.unSavedProgress = false;
            }
            else
                SaveDialogue.Open(SaveDialogue.Type.Save);
        }

        private static string GetSaveValues()
        {
            savedValues.Clear();
            savedValues.Add("version", $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}");
            savedValues.Add("lastOpenedChapter", $"{MainPage.ChapterList.listView.SelectedIndex}");
            savedValues.Add("name", currentProject.projectName);

            for (int i = 0; i < Character.characters.Count; i++)
            {
                savedValues.Add($"character{i}", $"{Character.characters[i].name}<Y&⨝m>{Character.characters[i].description}<Y&⨝m>{Character.characters[i].picture.fileName}");
            }

            for (int i = 0; i < Chapter.chapters.Count; i++)
            {
                if (Chapter.chapters[i].text.Contains("<Y&⨝m>") || Chapter.chapters[i].text.Contains(">[Y≇g&<") || Chapter.chapters[i].text.Contains("@N*∛$\n"))
                {
                    Chapter.chapters[i].text.Replace("<Y&⨝m>", "");
                    Chapter.chapters[i].text.Replace(">[Y≇g&<", "");
                    Chapter.chapters[i].text.Replace("@N*∛$\n", "");
                }

                savedValues.Add($"chapter{i}", $"{Chapter.chapters[i].name}<Y&⨝m>{Chapter.chapters[i].text}");
            }

            string[] valuesToSave = savedValues.Values.ToArray();
            string[] keysToSave = savedValues.Keys.ToArray();
            string toSave = "";

            for (int i = 0; i < savedValues.Count; i++)
            {
                toSave += $"{keysToSave[i]}>[Y≇g&<{valuesToSave[i]}@N*∛$\n";
            }

            return toSave;
        }

        private static async void NewFile(StorageFolder folder, string fileContent, string fileName)
        {
            StorageFile file = await folder.CreateFileAsync($@"{fileName}.srl", CreationCollisionOption.OpenIfExists);

            currentProject.file = file;
            ProjectFile.New(file);

            WriteToFile(fileContent);
        }

        public static async void NewFile(StorageFolder folder, string fullFileName)
        {
            var file = await folder.CreateFileAsync(fullFileName, CreationCollisionOption.OpenIfExists);

            currentProject.file = file;
            ProjectFile.New(file);

            Save();
        }

        private static async void WriteToFile(string fileContent)
        {
            try
            {
                IBuffer buffUTF8 = CryptographicBuffer.ConvertStringToBinary(fileContent, BinaryStringEncoding.Utf8);
                await FileIO.WriteBufferAsync(currentProject.file, buffUTF8);
            }
            catch
            {
                NotificationManager.DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, ResourceLoader.GetForCurrentView().GetString("saveSaveSystemErrorText"), "");
                NotificationManager.UpdateMainProgressBar(0, NotificationManager.ProgressState.Error);

                afterSave = AfterSave.DoNothing;
            }
            
            ToDoAfterSave();
        }

        private static void ToDoAfterSave()
        {
            switch (afterSave)
            {
                case AfterSave.DoNothing:
                    TimeTravelSystem.unSavedProgress = false;
                    AppView.current.UpdateTitleBar();
                    break;
                case AfterSave.ClearEverything:
                    currentProject = null;
                    AppView.current.ClearEverything();
                    TimeTravelSystem.unSavedProgress = false;
                    LoadProjectDialogue.Open();
                    break;
                case AfterSave.Exit:
                    App.Current.Exit();
                    break;
            }

            NotificationManager.HideMainProgressBar();
        }

        public static async void OpenFileExplorer_SaveAsync(string fileName)
        {
            Windows.Storage.Pickers.FolderPicker picker = new Windows.Storage.Pickers.FolderPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                NewFile(folder, GetSaveValues(), fileName);
            }
        }
        #endregion

        #region Load
        public static void Load(ProjectFile project)
        {
            _ = LoadAsync(project);
        }

        public static async Task LoadAsync(ProjectFile project)
        {
            if (project.file == null)
            {
                project.file = await OpenFileEplorerLoadAsync();

                if(project.file != null)
                    _ = LoadAsync(project);
            } 
            else
            {
                currentProject = project;

                if (ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] != null)
                    ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] = project.token;

                NotificationManager.DisplayMainProgressBar(true);

                if (project.file.FileType == ".srl")
                    _ = LoadStorylinesDocument(project.file);
                else
                if (project.file.FileType == ".txt")
                    _ = LoadPlainDocument(project.file);
            }
        }

        public static async Task LoadStorylinesDocument(StorageFile file)
        {
            AppView.current.ClearEverything();
            LoadProjectDialogue.loadFile.isEscape = false;
            LoadProjectDialogue.loadFile.Hide();
            try
            {
                string txt = await FileIO.ReadTextAsync(file);
                string[] loadedStrings = txt.Split(txt.Contains("@N*∛$\n") ? "@N*∛$\n" : "@N*∛$\r\n", StringSplitOptions.RemoveEmptyEntries);

                savedValues.Clear();
                foreach (string s in loadedStrings) 
                {
                    string[] values = s.Split(">[Y≇g&<", StringSplitOptions.RemoveEmptyEntries);
                    savedValues.Add(values[0], values[1]);
                }

                    currentProject.projectVersion = savedValues["version"];
                if (SettingsValues.IsCurrentVersionGreater(currentProject.projectVersion, "0.5.53.0"))
                    currentProject.projectName = savedValues["name"];

                for (int i = 0; i < savedValues.Count; i++)
                {
                    if (savedValues.Keys.ToList()[i].Contains("character"))
                    {
                        string[] str = savedValues.Values.ToList()[i].Split("<Y&⨝m>", StringSplitOptions.None);
                        if (SettingsValues.IsCurrentVersionGreater(currentProject.projectVersion, "0.5.4.0") && str[2] != null)
                            Character.AddExisting(str[0], Guid.NewGuid().ToString(), str[1], new CharacterPicture() { fileName = str[2] });
                        else
                            Character.AddExisting(str[0], Guid.NewGuid().ToString(), str[1], null);
                    }
                    else
                    if (savedValues.Keys.ToList()[i].Contains("chapter"))
                    {
                        string[] str = savedValues.Values.ToList()[i].Split("<Y&⨝m>", StringSplitOptions.None);
                        Chapter.AddExisting(str[0], Guid.NewGuid().ToString(), str[1]);
                    }
                }

                LoadVariables();

                Loaded();
                MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(true);
                //Windows.Security.Cryptography.CryptographicBuffer.ConvertBinaryToString
            }
            catch
            {
                NotificationManager.DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, ResourceLoader.GetForCurrentView().GetString("loadSaveSystemErrorText"), "");
                NotificationManager.UpdateMainProgressBar(0, NotificationManager.ProgressState.Error);
            }
        }

        public static async Task LoadPlainDocument(StorageFile file)
        {
            try
            {
                AppView.current.ClearEverything();
                LoadProjectDialogue.loadFile.isEscape = false;
                LoadProjectDialogue.loadFile.Hide();

                string txt = await FileIO.ReadTextAsync(file);

                Chapter.AddExisting(file.DisplayName, Guid.NewGuid().ToString(), txt);
                MainPage.ChapterList.listView.SelectedIndex = 0;

                //saveFile = file;

                //loadedProjectName = file.Name;
                Loaded();
                MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(false);
            }
            catch
            {
                NotificationManager.DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, ResourceLoader.GetForCurrentView().GetString("loadSaveSystemErrorText"), "");
                NotificationManager.UpdateMainProgressBar(0, NotificationManager.ProgressState.Error);
            }
        }

        private static void LoadVariables()
        {
            ChaptersList.selectedIndex = Convert.ToInt32(savedValues["lastOpenedChapter"] ?? "0");
            MainPage.ChapterList.listView.SelectedIndex = ChaptersList.selectedIndex;
        }

        private static void Loaded()
        {
            TimeTravelSystem.unSavedProgress = false;
            savedValues.Clear();
            AppView.current.UpdateTitleBar();

            NotificationManager.HideMainProgressBar();
        }

        private static async Task<StorageFile> OpenFileEplorerLoadAsync()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeFilter.Add(".srl");
            picker.FileTypeFilter.Add(".txt");

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                if (!ProjectFile.ChectIfProjectExists(file))
                    ProjectFile.New(file);

                return file;
            }
            else
            {
                //NotificationManager.DisplayInAppNotification(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, "An error occurred your file couldn't be load.", "");
                return null;
            }
        }

        public static void DefaultLaunch(IStorageItem storageItem)
        {
            var file = storageItem as StorageFile;

            Load(new ProjectFile() { file = file });

            if (!ProjectFile.ChectIfProjectExists(file))
                ProjectFile.New(file);
        }

        //private static async Task DefaultLaunchAsync(StorageFile file)
        //{
        //    await ProjectFile.LoadAllAsync();

        //    if (!ProjectFile.ChectIfProjectExists(file))
        //        ProjectFile.New(file);
        //}
        #endregion
    }

    public class ProjectFile : INotifyPropertyChanged
    {
        public string name { get; set; }
        public string token { get; private set; }
        public string path { get; set; }
        public StorageFile file { get; set; }

        public string projectName { get; set; }
        public string projectVersion { get; set; }

        public Uri icon { get; set; }
        public string shortPath { get; set; }
        public string lastEditedFormatted { get; private set; }
        public DateTimeOffset lastEdited { get; private set; }

        public Windows.UI.Xaml.Thickness osMargin { get; private set; } = LoadProjectDialogue.osMargin;
        public double osWidth { get; private set; } = LoadProjectDialogue.osWidth;

        public static ObservableCollection<ProjectFile> projectFiles = new ObservableCollection<ProjectFile>();

        public event PropertyChangedEventHandler PropertyChanged;

        public static void New(StorageFile file)
        {
            _ = Remember(file);
        }

        public static ProjectFile LoadExisting(StorageFile file, string token)
        {
            BasicProperties basicProperties = file.GetBasicPropertiesAsync().AsTask().GetAwaiter().GetResult();
            return new ProjectFile()
            {
                name = file.Name,
                path = file.Path,
                token = token,
                file = file,
                icon = new Uri(file.FileType == ".txt" ? "ms-appx:/Assets/Icons/Text-document-icon.png" : "ms-appx:/Assets/Icons/Storylines-document-icon.png"),
                shortPath = file.Path.Replace(@"\" + file.Name, string.Empty).Replace(@"\", "/"),
                lastEditedFormatted = basicProperties.DateModified.ToString("g", Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.Culture),
                lastEdited = basicProperties.DateModified
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
                if (projectFiles[i].token == token)
                {
                    projectFiles.RemoveAt(i);
                    StorageApplicationPermissions.FutureAccessList.Remove(token);
                }
            }
        }

        public static async Task LoadAllAsync()
        {
            foreach (AccessListEntry token in StorageApplicationPermissions.FutureAccessList.Entries)
            {
                Task<StorageFile> task = GetProjectFromTokenAsync(token.Token);

                if (await Task.WhenAny(task, Task.Delay(1000)) == task)
                {
                    StorageFile file = task.Result;
                    projectFiles.Add(LoadExisting(file, token.Token));
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

        public static bool ChectIfProjectExists(StorageFile file)
        {
            for (int i = 0; i < projectFiles.Count; i++)
            {
                if (projectFiles[i].path == file.Path)
                {
                    return true;
                }
            }
            return false;
        }

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
