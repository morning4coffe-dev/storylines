using Storylines.Components.DialogueWindows;
using Storylines.Scripts.Services;
using Storylines.Scripts.Services.Interfaces;
using Storylines.Scripts.Variables;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

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
        private static ITextEditorService TextEditor => ServiceLocator.TextEditor;

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
                    _ = WriteToFileAsync(JsonSerializer.Serialize(projectData));
                    ServiceLocator.Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = true });
                }
                else if (currentProject.file.FileType == ".txt")
                {
                    string txt = TextEditor.GetText(TextFormat.PlainText);
                    ServiceLocator.Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = false });
                    _ = WriteToFileAsync(txt);
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
                _ = WriteToFileAsync(JsonSerializer.Serialize(projectData));
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
                LastOpenedChapter = TextEditor.SelectedChapterIndex,
                Name = currentProject.projectName
            };

            foreach (var character in ServiceLocator.ProjectState.Characters)
            {
                data.Characters.Add(new CharacterData
                {
                    Name = character.name,
                    Description = character.description,
                    PictureFileName = character.picture?.fileName ?? string.Empty,
                    Role = character.role,
                    Age = character.age
                });
            }

            foreach (var chapter in ServiceLocator.ProjectState.Chapters)
            {
                data.Chapters.Add(new ChapterData
                {
                    Name = chapter.name,
                    Text = chapter.text,
                    Notes = chapter.notes ?? string.Empty,
                    Synopsis = chapter.synopsis,
                    WordCountGoal = chapter.wordCountGoal
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

        private static async Task NewFileAsync(StorageFolder folder, string fileContent, string fileName)
        {
            StorageFile file = await folder.CreateFileAsync($@"{fileName}.srl", CreationCollisionOption.OpenIfExists);

            currentProject.file = file;
            ProjectFile.New(file);

            await WriteToFileAsync(fileContent);
        }

        public static async Task NewFileAsync(StorageFolder folder, string fullFileName)
        {
            var file = await folder.CreateFileAsync(fullFileName, CreationCollisionOption.OpenIfExists);

            currentProject.file = file;
            ProjectFile.New(file);

            Save();
        }

        private static async Task WriteToFileAsync(string fileContent)
        {
            try
            {
                await FileService.WriteAsync(currentProject.file, fileContent);
                ToDoAfterSave();
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
        }

        private static void ToDoAfterSave()
        {
            switch (afterSave)
            {
                case AfterSave.DoNothing:
                    TimeTravelSystem.unSavedProgress = false;
                    ServiceLocator.Events.Publish(new TitleBarUpdateEvent());
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

        public static async Task OpenFileExplorer_SaveAsync(string fileName)
        {
            StorageFolder folder = await FileService.PickFolderForSaveAsync();

            if (folder != null)
            {
                var projectData = CollectProjectData();
                await NewFileAsync(folder, JsonSerializer.Serialize(projectData), fileName);
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
                project.file = await OpenFileExplorerLoadAsync();

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
                    await ServiceLocator.ProjectState.AddExistingCharacterAsync(charData.Name, Guid.NewGuid().ToString(), charData.Description, picture, charData.Role, charData.Age);
                }

                foreach (var chapterData in projectData.Chapters)
                {
                    ServiceLocator.ProjectState.AddExistingChapter(chapterData.Name, Guid.NewGuid().ToString(), chapterData.Text, chapterData.Notes, chapterData.Synopsis, chapterData.WordCountGoal);
                }

                LoadVariables(projectData);
                Loaded();
                ServiceLocator.Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = true });
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

                ServiceLocator.ProjectState.AddExistingChapter(file.DisplayName, Guid.NewGuid().ToString(), txt);
                TextEditor.SelectedChapterIndex = 0;

                Loaded();
                ServiceLocator.Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = false });
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
            Components.ChaptersList.selectedIndex = projectData.LastOpenedChapter;
            TextEditor.SelectedChapterIndex = Components.ChaptersList.selectedIndex;
        }

        private static void Loaded()
        {
            TimeTravelSystem.unSavedProgress = false;
            savedValues.Clear();
            ServiceLocator.Events.Publish(new TitleBarUpdateEvent());

            NotificationManager.HideMainProgressBar();
        }

        private static async Task<StorageFile> OpenFileExplorerLoadAsync()
        {
            StorageFile file = await FileService.PickFileForOpenAsync();

            if (file != null)
            {
                if (!ProjectFile.CheckIfProjectExists(file))
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

            if (!ProjectFile.CheckIfProjectExists(file))
                ProjectFile.New(file);
        }

        //private static async Task DefaultLaunchAsync(StorageFile file)
        //{
        //    await ProjectFile.LoadAllAsync();

        //    if (!ProjectFile.CheckIfProjectExists(file))
        //        ProjectFile.New(file);
        //}
        #endregion
    }

}
