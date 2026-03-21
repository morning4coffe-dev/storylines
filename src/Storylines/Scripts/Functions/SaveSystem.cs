using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using Storylines.Scripts.Services.Interfaces;
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

        private static ILogger Logger => ServiceLocator.Logger;
        private static IFileService FileService => ServiceLocator.FileService;
        private static ISaveSerializer JsonSerializer => ServiceLocator.JsonSerializer;
        private static ISaveSerializer LegacySerializer => ServiceLocator.LegacySerializer;

        #region Save
        private enum AfterSave { DoNothing, ClearEverything, Exit };
        private static AfterSave afterSave;

        public static void Save()
        {
            afterSave = AfterSave.DoNothing;

            if (currentProject.file != null)
            {
                if (currentProject.file.FileType == ".srl")
                {
                    var projectData = CollectProjectData();
                    WriteToFile(JsonSerializer.Serialize(projectData));
                    MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(true);
                }
                else if (currentProject.file.FileType == ".txt")
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
            afterSave = exit ? AfterSave.Exit : AfterSave.ClearEverything;

            if (currentProject.file != null)
            {
                var projectData = CollectProjectData();
                WriteToFile(JsonSerializer.Serialize(projectData));
                TimeTravelSystem.unSavedProgress = false;
            }
            else
                SaveDialogue.Open(SaveDialogue.Type.Save);
        }

        private static ProjectData CollectProjectData()
        {
            var data = new ProjectData
            {
                Version = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}",
                LastOpenedChapter = MainPage.ChapterList.listView.SelectedIndex,
                Name = currentProject.projectName
            };

            foreach (var character in Character.characters)
            {
                data.Characters.Add(new CharacterData
                {
                    Name = character.name,
                    Description = character.description,
                    PictureFileName = character.picture?.fileName ?? string.Empty
                });
            }

            foreach (var chapter in Chapter.chapters)
            {
                data.Chapters.Add(new ChapterData
                {
                    Name = chapter.name,
                    Text = chapter.text,
                    Notes = chapter.notes ?? string.Empty
                });
            }

            return data;
        }

        // Legacy format support — kept for SaveCopy / backward compat
        private static string GetSaveValues()
        {
            var projectData = CollectProjectData();
            return JsonSerializer.Serialize(projectData);
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
                await FileService.WriteAsync(currentProject.file, fileContent);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to write project file", ex);
                NotificationManager.DisplayInAppNotification(
                    Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                    ResourceLoader.GetForCurrentView().GetString("saveSaveSystemErrorText"), "");
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
            StorageFolder folder = await FileService.PickFolderForSaveAsync();

            if (folder != null)
            {
                var projectData = CollectProjectData();
                NewFile(folder, JsonSerializer.Serialize(projectData), fileName);
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
                else if (project.file.FileType == ".txt")
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
                string content = await FileService.ReadAsync(file);

                ProjectData projectData;

                // Try JSON first, then fall back to legacy .srl format
                if (JsonSerializer.CanDeserialize(content))
                {
                    projectData = JsonSerializer.Deserialize(content);
                    Logger.Info("Loaded project in JSON format");
                }
                else if (LegacySerializer.CanDeserialize(content))
                {
                    projectData = LegacySerializer.Deserialize(content);
                    Logger.Info("Loaded project in legacy SRL format — will save as JSON on next save");
                }
                else
                {
                    Logger.Error("Unable to detect file format for: " + file.Name);
                    NotificationManager.DisplayInAppNotification(
                        Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                        ResourceLoader.GetForCurrentView().GetString("loadSaveSystemErrorText"), "");
                    return;
                }

                currentProject.projectVersion = projectData.Version;
                currentProject.projectName = projectData.Name;

                foreach (var charData in projectData.Characters)
                {
                    var picture = !string.IsNullOrEmpty(charData.PictureFileName)
                        ? new CharacterPicture { fileName = charData.PictureFileName }
                        : null;
                    Character.AddExisting(charData.Name, Guid.NewGuid().ToString(), charData.Description, picture);
                }

                foreach (var chapterData in projectData.Chapters)
                {
                    Chapter.AddExisting(chapterData.Name, Guid.NewGuid().ToString(), chapterData.Text, chapterData.Notes);
                }

                LoadVariables(projectData);
                Loaded();
                MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(true);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load Storylines document", ex);
                NotificationManager.DisplayInAppNotification(
                    Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                    ResourceLoader.GetForCurrentView().GetString("loadSaveSystemErrorText"), "");
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

                string txt = await FileService.ReadAsync(file);

                Chapter.AddExisting(file.DisplayName, Guid.NewGuid().ToString(), txt);
                MainPage.ChapterList.listView.SelectedIndex = 0;

                Loaded();
                MainPage.Current.EnableOrDisableToolsForStorylinesDocuments(false);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load plain text document", ex);
                NotificationManager.DisplayInAppNotification(
                    Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                    ResourceLoader.GetForCurrentView().GetString("loadSaveSystemErrorText"), "");
                NotificationManager.UpdateMainProgressBar(0, NotificationManager.ProgressState.Error);
            }
        }

        private static void LoadVariables(ProjectData projectData)
        {
            ChaptersList.selectedIndex = projectData.LastOpenedChapter;
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
            StorageFile file = await FileService.PickFileForOpenAsync();

            if (file != null)
            {
                if (!ProjectFile.ChectIfProjectExists(file))
                    ProjectFile.New(file);

                return file;
            }
            else
            {
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
