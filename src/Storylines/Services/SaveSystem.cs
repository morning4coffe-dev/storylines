using Storylines.Views.Dialogs;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Storylines.Views.Controls;

namespace Storylines.Services
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
                var charData = new CharacterData
                {
                    Name = character.Name,
                    Description = character.Description,
                    PictureFileName = character.Picture?.FileName ?? string.Empty,
                    Role = character.Role,
                    Age = character.Age,
                    Appearance = character.Appearance,
                    Traits = character.Traits?.Where(item => !string.IsNullOrWhiteSpace(item)).ToList()
                };

                if (character.Relationships?.Count > 0)
                {
                    charData.Relationships = character.Relationships.Select(r => new CharacterRelationshipData
                    {
                        TargetName = ServiceLocator.ProjectState.FindCharacter(r.TargetCharacterToken)?.Name ?? r.TargetCharacterToken,
                        Type = r.Type
                    }).ToList();
                }

                data.Characters.Add(charData);
            }

            foreach (var chapter in ServiceLocator.ProjectState.Chapters)
            {
                data.Chapters.Add(new ChapterData
                {
                    Name = chapter.Name,
                    Text = chapter.Text,
                    Notes = chapter.Notes ?? string.Empty,
                    Synopsis = chapter.Synopsis,
                    WordCountGoal = chapter.WordCountGoal,
                    Tags = chapter.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
                    PinboardX = chapter.PinboardX != 0 ? chapter.PinboardX : (double?)null,
                    PinboardY = chapter.PinboardY != 0 ? chapter.PinboardY : (double?)null,
                    Status = chapter.Status != ChapterStatus.Draft ? chapter.Status.ToString() : null,
                    Location = chapter.Location,
                    PlotThreads = chapter.PlotThreads?.Count > 0 ? chapter.PlotThreads : null
                });
            }

            if (ServiceLocator.ProjectState.PinboardConnections?.Count > 0)
                data.PinboardConnections = ServiceLocator.ProjectState.PinboardConnections;

            if (ServiceLocator.ProjectState.PlotThreads?.Count > 0)
                data.PlotThreads = ServiceLocator.ProjectState.PlotThreads;

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
                    ResourceLoader.GetForViewIndependentUse().GetString("saveSaveSystemErrorText"), "");
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
                    ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] = project.Token;

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
                        ResourceLoader.GetForViewIndependentUse().GetString("loadSaveSystemErrorText"), "");
                    return;
                }

                currentProject.projectVersion = projectData.Version;
                currentProject.projectName = projectData.Name;

                foreach (var charData in projectData.Characters)
                {
                    var picture = !string.IsNullOrEmpty(charData.PictureFileName)
                        ? new CharacterPicture { FileName = charData.PictureFileName }
                        : null;
                    await ServiceLocator.ProjectState.AddExistingCharacterAsync(charData.Name, Guid.NewGuid().ToString(), charData.Description, picture, charData.Role, charData.Age, charData.Appearance, charData.Traits);
                }

                foreach (var chapterData in projectData.Chapters)
                {
                    ChapterStatus status = ChapterStatus.Draft;
                    if (!string.IsNullOrEmpty(chapterData.Status))
                        System.Enum.TryParse(chapterData.Status, true, out status);

                    ServiceLocator.ProjectState.AddExistingChapter(chapterData.Name, Guid.NewGuid().ToString(), chapterData.Text, chapterData.Notes, chapterData.Synopsis, chapterData.WordCountGoal, chapterData.Tags, chapterData.PinboardX ?? 0, chapterData.PinboardY ?? 0, status, chapterData.Location, chapterData.PlotThreads);
                }

                ServiceLocator.ProjectState.PinboardConnections = projectData.PinboardConnections ?? new System.Collections.Generic.List<PinboardConnectionData>();
                ServiceLocator.ProjectState.PlotThreads = projectData.PlotThreads ?? new System.Collections.Generic.List<string>();

                // Restore character relationships by resolving target names to tokens
                for (int ci = 0; ci < projectData.Characters.Count && ci < ServiceLocator.ProjectState.Characters.Count; ci++)
                {
                    var charData = projectData.Characters[ci];
                    if (charData.Relationships != null)
                    {
                        var character = ServiceLocator.ProjectState.Characters[ci];
                        character.Relationships = charData.Relationships
                            .Select(r =>
                            {
                                var target = ServiceLocator.ProjectState.Characters.FirstOrDefault(
                                    c => string.Equals(c.Name, r.TargetName, StringComparison.CurrentCultureIgnoreCase));
                                return target != null ? new CharacterRelationship { TargetCharacterToken = target.Token, Type = r.Type } : null;
                            })
                            .Where(r => r != null)
                            .ToList();
                    }
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
                    ResourceLoader.GetForViewIndependentUse().GetString("loadSaveSystemErrorText"), "");
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
                    ResourceLoader.GetForViewIndependentUse().GetString("loadSaveSystemErrorText"), "");
                NotificationManager.UpdateMainProgressBar(0, NotificationManager.ProgressState.Error);
            }
        }

        private static void LoadVariables(ProjectData projectData)
        {
            ChaptersList.selectedIndex = projectData.LastOpenedChapter;
            TextEditor.SelectedChapterIndex = ChaptersList.selectedIndex;
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

        #endregion
    }

}
