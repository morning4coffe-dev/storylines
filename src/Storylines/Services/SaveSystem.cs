using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Services.Serializers;
using Storylines.Models;
using Storylines.ViewModels;
using Storylines.Views.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace Storylines.Services
{
    public class SaveSystem
    {
        public Dictionary<string, string> SavedValues { get; } = new Dictionary<string, string>();

        public ProjectFile CurrentProject { get; set; }

        // Keep static accessors for backward compatibility during transition
        public static Dictionary<string, string> savedValues => Instance.SavedValues;
        public static ProjectFile currentProject
        {
            get => Instance.CurrentProject;
            set => Instance.CurrentProject = value;
        }

        private static SaveSystem Instance => App.GetService<SaveSystem>();

        private static EventAggregator Events => App.GetService<EventAggregator>();
        private static IDialogService Dialogs => App.GetService<IDialogService>();
        private static IFileService FileService => App.GetService<IFileService>();
        private static ISaveSerializer JsonSerializer => App.GetService<JsonSaveSerializer>();
        private static ISaveSerializer LegacySerializer => App.GetService<LegacySrlSerializer>();
        private static ILogger Logger => App.GetService<ILogger>();
        private static ProjectState ProjectState => App.GetService<ProjectState>();
        private static ITextEditorService TextEditor => App.GetService<ITextEditorService>();

        #region Save
        private enum AfterSave { DoNothing, ClearEverything, Exit };
        private static AfterSave afterSave;

        public static async Task SaveAsync()
        {
            afterSave = AfterSave.DoNothing;

            if (currentProject.file != null)
            {
                NotificationManager.DisplayMainProgressBar(true);

                if (currentProject.file.FileType == ".srl")
                {
                    var projectData = CollectProjectData();
                    await WriteToFileAsync(JsonSerializer.Serialize(projectData));
                    Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = true });
                }
                else if (currentProject.file.FileType == ".txt")
                {
                    string txt = TextEditor.GetText(TextFormat.PlainText);
                    Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = false });
                    await WriteToFileAsync(txt);
                }

                TimeTravelSystem.unSavedProgress = false;
            }
            else
                Dialogs.OpenSaveDialogue();
        }

        /// <summary>Synchronous wrapper for callers that cannot await (event handlers, shortcuts).</summary>
        public static void Save()
        {
            _ = SaveAsync();
        }

        public static void SaveCopy()
        {
            Dialogs.OpenSaveCopyDialogue();
        }

        public static async Task SaveAndExitOrClearAllAsync(bool exit)
        {
            afterSave = exit ? AfterSave.Exit : AfterSave.ClearEverything;

            if (currentProject.file != null)
            {
                var projectData = CollectProjectData();
                await WriteToFileAsync(JsonSerializer.Serialize(projectData));
                TimeTravelSystem.unSavedProgress = false;
            }
            else
                Dialogs.OpenSaveDialogue();
        }

        /// <summary>Synchronous wrapper for callers that cannot await.</summary>
        public static void SaveAndExitOrClearAll(bool exit)
        {
            _ = SaveAndExitOrClearAllAsync(exit);
        }

        internal static void CancelPendingAfterSaveAction()
        {
            afterSave = AfterSave.DoNothing;
        }

        internal static ProjectData CollectProjectData()
        {
            var data = new ProjectData
            {
                Version = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}",
                LastOpenedChapter = TextEditor.SelectedChapterIndex,
                Name = currentProject.projectName
            };

            foreach (var character in ProjectState.Characters)
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
                        TargetName = ProjectState.FindCharacter(r.TargetCharacterToken)?.Name ?? r.TargetCharacterToken,
                        Type = r.Type
                    }).ToList();
                }

                data.Characters.Add(charData);
            }

            foreach (var chapter in ProjectState.Chapters)
            {
                var graph = ProjectState.FindBranchingDialogueByChapter(chapter.Token);

                data.Chapters.Add(new ChapterData
                {
                    Id = chapter.Token,
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
                    PlotThreads = chapter.PlotThreads?.Count > 0 ? chapter.PlotThreads : null,
                    BranchingDialogueGraphId = graph?.Id
                });
            }

            if (ProjectState.PinboardConnections?.Count > 0)
                data.PinboardConnections = ProjectState.PinboardConnections;

            if (ProjectState.PlotThreads?.Count > 0)
                data.PlotThreads = ProjectState.PlotThreads;

            if (ProjectState.BranchingDialogues?.Count > 0)
            {
                data.BranchingDialogues = ProjectState.BranchingDialogues
                    .Where(g => g != null && !string.IsNullOrWhiteSpace(g.ChapterId))
                    .Select(CloneAndNormalizeGraph)
                    .ToList();
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
                await RecoveryService.ClearRecoveryDataAsync();
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
                    Events.Publish(new TitleBarUpdateEvent());
                    break;
                case AfterSave.ClearEverything:
                    currentProject = null;
                    Dialogs.ClearEverything();
                    TimeTravelSystem.unSavedProgress = false;
                    Dialogs.OpenLoadDialogue();
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
                    await LoadAsync(project);
            } 
            else
            {
                currentProject = project;

                if (ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] != null)
                    ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] = project.Token;

                NotificationManager.DisplayMainProgressBar(true);

                if (project.file.FileType == ".srl")
                    await LoadStorylinesDocument(project.file);
                else if (project.file.FileType == ".txt")
                    await LoadPlainDocument(project.file);
            }
        }

        public static async Task LoadStorylinesDocument(StorageFile file)
        {
            Dialogs.ClearEverything();
            Dialogs.DismissLoadDialogue();
            try
            {
                string content = await FileService.ReadAsync(file);

                ProjectData projectData;

                // Try JSON first, then fall back to legacy .srl format
                if (JsonSerializer.CanDeserialize(content))
                {
                    projectData = JsonSerializer.Deserialize(content);
                    projectData = NormalizeProjectData(projectData);
                    Logger.Info("Loaded project in JSON format");
                }
                else if (LegacySerializer.CanDeserialize(content))
                {
                    projectData = LegacySerializer.Deserialize(content);
                    projectData = NormalizeProjectData(projectData);
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
                    await ProjectState.AddExistingCharacterAsync(charData.Name, Guid.NewGuid().ToString(), charData.Description, picture, charData.Role, charData.Age, charData.Appearance, charData.Traits);
                }

                foreach (var chapterData in projectData.Chapters)
                {
                    ChapterStatus status = ChapterStatus.Draft;
                    if (!string.IsNullOrEmpty(chapterData.Status))
                        System.Enum.TryParse(chapterData.Status, true, out status);

                    var chapterToken = !string.IsNullOrWhiteSpace(chapterData.Id)
                        ? chapterData.Id
                        : Guid.NewGuid().ToString();

                    ProjectState.AddExistingChapter(chapterData.Name, chapterToken, chapterData.Text, chapterData.Notes, chapterData.Synopsis, chapterData.WordCountGoal, chapterData.Tags, chapterData.PinboardX ?? 0, chapterData.PinboardY ?? 0, status, chapterData.Location, chapterData.PlotThreads);
                }

                ProjectState.PinboardConnections = projectData.PinboardConnections ?? new System.Collections.Generic.List<PinboardConnectionData>();
                ProjectState.PlotThreads = projectData.PlotThreads ?? new System.Collections.Generic.List<string>();
                ProjectState.SetBranchingDialogues(projectData.BranchingDialogues?.Select(CloneAndNormalizeGraph).ToList());

                // Restore character relationships by resolving target names to tokens
                for (int ci = 0; ci < projectData.Characters.Count && ci < ProjectState.Characters.Count; ci++)
                {
                    var charData = projectData.Characters[ci];
                    if (charData.Relationships != null)
                    {
                        var character = ProjectState.Characters[ci];
                        character.Relationships = charData.Relationships
                            .Select(r =>
                            {
                                var target = ProjectState.Characters.FirstOrDefault(
                                    c => string.Equals(c.Name, r.TargetName, StringComparison.CurrentCultureIgnoreCase));
                                return target != null ? new CharacterRelationship { TargetCharacterToken = target.Token, Type = r.Type } : null;
                            })
                            .Where(r => r != null)
                            .ToList();
                    }
                }

                LoadVariables(projectData);
                Loaded();
                Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = true });
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
                Dialogs.ClearEverything();
                Dialogs.DismissLoadDialogue();

                string txt = await FileService.ReadAsync(file);

                ProjectState.AddExistingChapter(file.DisplayName, Guid.NewGuid().ToString(), txt);
                TextEditor.SelectedChapterIndex = 0;

                Loaded();
                Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = false });
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
            var chaptersListViewModel = App.TryGetService<ChaptersListViewModel>();
            if (chaptersListViewModel != null)
                chaptersListViewModel.SelectedIndex = projectData.LastOpenedChapter;
            else
                TextEditor.SelectedChapterIndex = projectData.LastOpenedChapter;
        }

        private static void Loaded()
        {
            TimeTravelSystem.unSavedProgress = false;
            savedValues.Clear();
            Events.Publish(new TitleBarUpdateEvent());

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

        private static ProjectData NormalizeProjectData(ProjectData projectData)
        {
            projectData ??= new ProjectData();
            projectData.Chapters ??= new List<ChapterData>();
            projectData.Characters ??= new List<CharacterData>();
            projectData.PinboardConnections ??= new List<PinboardConnectionData>();
            projectData.PlotThreads ??= new List<string>();
            projectData.BranchingDialogues ??= new List<BranchingDialogueGraphData>();

            var chapterIds = new HashSet<string>();
            foreach (var chapter in projectData.Chapters)
            {
                if (chapter == null)
                    continue;

                chapter.Id = EnsureUniqueId(chapter.Id, chapterIds);
                chapter.Name ??= string.Empty;
                chapter.Text ??= string.Empty;
                chapter.Notes ??= string.Empty;
            }

            foreach (var graph in projectData.BranchingDialogues)
            {
                if (graph == null)
                    continue;

                if (string.IsNullOrWhiteSpace(graph.ChapterId) || !chapterIds.Contains(graph.ChapterId))
                {
                    var chapterFromLink = projectData.Chapters.FirstOrDefault(c => c?.BranchingDialogueGraphId == graph.Id);
                    if (!string.IsNullOrWhiteSpace(chapterFromLink?.Id))
                        graph.ChapterId = chapterFromLink.Id;
                }

                graph.EnsureValid();
            }

            // Keep only graphs bound to an existing chapter to avoid orphaned data drift.
            projectData.BranchingDialogues = projectData.BranchingDialogues
                .Where(g => g != null && !string.IsNullOrWhiteSpace(g.ChapterId) && chapterIds.Contains(g.ChapterId))
                .ToList();

            // Backfill chapter-level graph references for easier future migrations.
            foreach (var chapter in projectData.Chapters)
            {
                var graph = projectData.BranchingDialogues.FirstOrDefault(g => g.ChapterId == chapter.Id);
                chapter.BranchingDialogueGraphId = graph?.Id;
            }

            return projectData;
        }

        private static string EnsureUniqueId(string id, HashSet<string> existing)
        {
            var candidate = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
            while (existing.Contains(candidate))
                candidate = Guid.NewGuid().ToString();

            existing.Add(candidate);
            return candidate;
        }

        private static BranchingDialogueGraphData CloneAndNormalizeGraph(BranchingDialogueGraphData graph)
        {
            if (graph == null)
                return null;

            var clone = new BranchingDialogueGraphData
            {
                Id = graph.Id,
                ChapterId = graph.ChapterId,
                StartNodeId = graph.StartNodeId,
                Nodes = graph.Nodes?.Select(node => new BranchingDialogueNodeData
                {
                    Id = node.Id,
                    Title = node.Title,
                    Speaker = node.Speaker,
                    Text = node.Text,
                    PositionX = node.PositionX,
                    PositionY = node.PositionY,
                    Tags = node.Tags?.ToList(),
                    Metadata = node.Metadata != null ? new Dictionary<string, string>(node.Metadata) : null,
                    Choices = node.Choices?.Select(choice => new BranchingDialogueChoiceData
                    {
                        Id = choice.Id,
                        Text = choice.Text,
                        TargetNodeId = choice.TargetNodeId,
                        Metadata = choice.Metadata != null ? new Dictionary<string, string>(choice.Metadata) : null,
                        Conditions = choice.Conditions?.Select(condition => new BranchingDialogueConditionData
                        {
                            Flag = condition.Flag,
                            Operator = condition.Operator,
                            Value = condition.Value
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            clone.EnsureValid();
            return clone;
        }

        #endregion
    }

}
