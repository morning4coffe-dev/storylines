using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.Services.Persistence;
using Storylines.Services.Serializers;
using Storylines.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Microsoft.UI.Xaml;

namespace Storylines.Services
{
    public class ProjectPersistenceService : IProjectPersistenceService
    {
        private readonly EventAggregator _events;
        private readonly IDialogService _dialogs;
        private readonly IFileService _fileService;
        private readonly JsonSaveSerializer _jsonSerializer;
        private readonly LegacySrlSerializer _legacySerializer;
        private readonly ILogger _logger;
        private readonly ProjectState _projectState;
        private readonly ITextEditorService _textEditor;
        private readonly IUndoRedoService _undoRedo;
        private readonly INotificationService _notifications;
        private readonly WindowContext _windowContext;
        private readonly IWindowManager _windowManager;
        private readonly Dictionary<string, DocumentPersistenceHandlerBase> _handlers;
        private readonly SemaphoreSlim _operationLock = new SemaphoreSlim(1, 1);

        private DispatcherTimer _autosaveTimer;
        private AfterSaveAction _afterSaveAction;

        private enum AfterSaveAction
        {
            None,
            ClearEverything,
            Exit
        }

        public ProjectPersistenceService(
            EventAggregator events,
            IDialogService dialogs,
            IFileService fileService,
            JsonSaveSerializer jsonSerializer,
            LegacySrlSerializer legacySerializer,
            ILogger logger,
            ProjectState projectState,
            ITextEditorService textEditor,
            IUndoRedoService undoRedo,
            INotificationService notifications,
            WindowContext windowContext,
            IWindowManager windowManager)
        {
            _events = events;
            _dialogs = dialogs;
            _fileService = fileService;
            _jsonSerializer = jsonSerializer;
            _legacySerializer = legacySerializer;
            _undoRedo = undoRedo;
            _notifications = notifications;
            _logger = logger;
            _projectState = projectState;
            _textEditor = textEditor;
            _windowContext = windowContext;
            _windowManager = windowManager;

            _handlers = new Dictionary<string, DocumentPersistenceHandlerBase>(StringComparer.OrdinalIgnoreCase)
            {
                [".srl"] = new StorylinesDocumentPersistenceHandler(
                    _fileService,
                    _dialogs,
                    _events,
                    _logger,
                    _projectState,
                    _textEditor,
                    _jsonSerializer,
                    _legacySerializer,
                    CollectProjectData,
                    NormalizeProjectData,
                    LoadVariables,
                    OnProjectLoaded,
                    _notifications),
                [".txt"] = new PlainTextDocumentPersistenceHandler(
                    _fileService,
                    _dialogs,
                    _events,
                    _logger,
                    _projectState,
                    _textEditor,
                    OnProjectLoaded,
                    _notifications)
            };
        }

        public Dictionary<string, string> SavedValues { get; } = new Dictionary<string, string>();

        public ProjectFile CurrentProject { get; set; }

        public void Save()
        {
            _ = SaveAsync();
        }

        public Task SaveAsync()
        {
            return SaveCurrentProjectAsync(AfterSaveAction.None);
        }

        public void SaveCopy()
        {
            _dialogs.OpenSaveCopyDialogue();
        }

        public void SaveAndExitOrClearAll(bool exit)
        {
            _ = SaveAndExitOrClearAllAsync(exit);
        }

        public Task SaveAndExitOrClearAllAsync(bool exit)
        {
            return SaveCurrentProjectAsync(exit ? AfterSaveAction.Exit : AfterSaveAction.ClearEverything);
        }

        public void CancelPendingAfterSaveAction()
        {
            _afterSaveAction = AfterSaveAction.None;
        }

        public ProjectData CollectProjectData()
        {
            var data = new ProjectData
            {
                Version = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}",
                LastOpenedChapter = _textEditor.SelectedChapterIndex,
                Name = CurrentProject?.projectName
            };

            foreach (var character in _projectState.Characters)
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
                    charData.Relationships = character.Relationships.Select(relationship => new CharacterRelationshipData
                    {
                        TargetName = _projectState.FindCharacter(relationship.TargetCharacterToken)?.Name ?? relationship.TargetCharacterToken,
                        Type = relationship.Type
                    }).ToList();
                }

                data.Characters.Add(charData);
            }

            foreach (var chapter in _projectState.Chapters)
            {
                data.Chapters.Add(new ChapterData
                {
                    Id = chapter.Token,
                    Name = chapter.Name,
                    Text = chapter.Text,
                    LastCaretPosition = chapter.LastCaretPosition > 0 ? chapter.LastCaretPosition : (int?)null,
                    LastVerticalOffset = chapter.LastVerticalOffset > 0 ? chapter.LastVerticalOffset : (double?)null,
                    Notes = chapter.Notes ?? string.Empty,
                    Synopsis = chapter.Synopsis,
                    WordCountGoal = chapter.WordCountGoal,
                    Tags = chapter.Tags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList(),
                    PinboardX = chapter.PinboardX != 0 ? chapter.PinboardX : (double?)null,
                    PinboardY = chapter.PinboardY != 0 ? chapter.PinboardY : (double?)null,
                    Status = chapter.Status != ChapterStatus.Draft ? chapter.Status.ToString() : null,
                    Location = chapter.Location,
                    PlotThreads = chapter.PlotThreads?.Count > 0 ? chapter.PlotThreads : null
                });
            }

            if (_projectState.PinboardConnections?.Count > 0)
                data.PinboardConnections = _projectState.PinboardConnections;

            if (_projectState.PlotThreads?.Count > 0)
                data.PlotThreads = _projectState.PlotThreads;

            return data;
        }

        public async Task OpenFileExplorerSaveAsync(string fileName)
        {
            var folder = await _fileService.PickFolderForSaveAsync();
            if (folder == null)
                return;

            await NewFileAsync(folder, $"{fileName}.srl");
        }

        public async Task NewFileAsync(StorageFolder folder, string fullFileName)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            var file = await folder.CreateFileAsync(fullFileName, CreationCollisionOption.OpenIfExists);
            var project = EnsureCurrentProject();

            project.file = file;
            ProjectFile.New(file);

            await PersistCurrentProjectAsync();
        }

        public void Load(ProjectFile project)
        {
            _ = LoadAsync(project);
        }

        public async Task LoadAsync(ProjectFile project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            if (project.file == null)
            {
                project.file = await OpenFileExplorerLoadAsync();
                if (project.file == null)
                    return;
            }

            await _operationLock.WaitAsync();

            try
            {
                CurrentProject = project;

                if (ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] != null)
                    ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] = project.Token;

                _notifications.ShowProgressBar(true);

                if (!TryResolveHandler(project.file, out var handler))
                {
                    _logger.Error($"Unsupported project file type: {project.file.FileType}");
                    ShowLoadErrorNotification();
                    _notifications.UpdateProgressBar(0, Storylines.Services.Interfaces.ProgressBarState.Error);
                    return;
                }

                await handler.LoadAsync(project);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task<bool> TryRestoreRecoveryAsync()
        {
            if (!RecoveryService.HasRecoveryData())
                return false;

            var recoveryJson = await RecoveryService.GetRecoveryJsonAsync();
            if (!_jsonSerializer.CanDeserialize(recoveryJson))
            {
                await RecoveryService.ClearRecoveryDataAsync();
                return false;
            }

            var projectData = NormalizeProjectData(_jsonSerializer.Deserialize(recoveryJson));
            var recoveredProject = await CreateRecoveredProjectAsync(projectData);
            var documentType = recoveredProject?.file?.FileType ?? RecoveryService.GetRecoveryDocumentType();

            await _operationLock.WaitAsync();

            try
            {
                CurrentProject = recoveredProject;
                _notifications.ShowProgressBar(true);

                if (string.Equals(documentType, ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    var plainTextHandler = _handlers[".txt"] as PlainTextDocumentPersistenceHandler;
                    if (plainTextHandler == null)
                        throw new InvalidOperationException("Plain text recovery handler is not available.");

                    await plainTextHandler.LoadTextAsync(CurrentProject, GetRecoveredPlainText(projectData));
                }
                else
                {
                    var storylinesHandler = _handlers[".srl"] as StorylinesDocumentPersistenceHandler;
                    if (storylinesHandler == null)
                        throw new InvalidOperationException("Storylines recovery handler is not available.");

                    await storylinesHandler.LoadProjectDataAsync(CurrentProject, projectData);
                }

                _undoRedo.MarkDirty();
                _events.Publish(new TitleBarUpdateEvent());
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to restore recovery data", ex);
                ShowLoadErrorNotification();
                _notifications.UpdateProgressBar(0, Storylines.Services.Interfaces.ProgressBarState.Error);
                return false;
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public void DefaultLaunch(IStorageItem storageItem)
        {
            var file = storageItem as StorageFile;
            if (file == null)
                return;

            Load(new ProjectFile { file = file });

            if (!ProjectFile.CheckIfProjectExists(file))
                ProjectFile.New(file);
        }

        public void EnableAutosave()
        {
            StopAutosaveTimer();

            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AutosaveEnabled] = true;

            _autosaveTimer = new DispatcherTimer
            {
                Interval = GetAutosaveInterval()
            };

            _autosaveTimer.Tick += OnAutosaveTimerTick;
            _autosaveTimer.Start();

            _ = TryAutosaveAsync();
        }

        public void DisableAutosave()
        {
            StopAutosaveTimer();
            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AutosaveEnabled] = false;
        }

        public void RefreshAutosave()
        {
            if (SettingsValues.autosaveEnabled)
                EnableAutosave();
        }

        private async Task SaveCurrentProjectAsync(AfterSaveAction afterSaveAction)
        {
            _afterSaveAction = afterSaveAction;

            if (EnsureCurrentProject().file == null)
            {
                _dialogs.OpenSaveDialogue();
                return;
            }

            await PersistCurrentProjectAsync();
        }

        private async Task PersistCurrentProjectAsync()
        {
            await _operationLock.WaitAsync();

            try
            {
                var project = EnsureCurrentProject();
                if (project.file == null)
                {
                    _dialogs.OpenSaveDialogue();
                    return;
                }

                _notifications.ShowProgressBar(true);

                if (!TryResolveHandler(project.file, out var handler))
                {
                    _logger.Error($"Unsupported project file type: {project.file.FileType}");
                    ShowSaveErrorNotification();
                    _notifications.UpdateProgressBar(0, Storylines.Services.Interfaces.ProgressBarState.Error);
                    _afterSaveAction = AfterSaveAction.None;
                    return;
                }

                await handler.SaveAsync(project);
                await RecoveryService.ClearRecoveryDataAsync();

                CompleteAfterSave();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to write project file", ex);
                ShowSaveErrorNotification();
                _notifications.UpdateProgressBar(0, Storylines.Services.Interfaces.ProgressBarState.Error);
                _afterSaveAction = AfterSaveAction.None;
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private ProjectFile EnsureCurrentProject()
        {
            CurrentProject ??= new ProjectFile();
            return CurrentProject;
        }

        private async Task<ProjectFile> CreateRecoveredProjectAsync(ProjectData projectData)
        {
            var token = RecoveryService.GetRecoveryProjectToken();
            if (string.IsNullOrWhiteSpace(token))
                return CreateTransientRecoveredProject(projectData);

            try
            {
                var file = await ProjectFile.GetProjectFromTokenAsync(token);
                if (file == null)
                    return CreateTransientRecoveredProject(projectData);

                var project = await ProjectFile.LoadExistingAsync(file, token);
                project.ProjectName = string.IsNullOrWhiteSpace(projectData?.Name) ? project.ProjectName : projectData.Name;
                project.ProjectVersion = projectData?.Version;
                return project;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to re-associate recovered project with its source file: {ex.Message}");
                return CreateTransientRecoveredProject(projectData);
            }
        }

        private static ProjectFile CreateTransientRecoveredProject(ProjectData projectData)
        {
            var name = !string.IsNullOrWhiteSpace(projectData?.Name)
                ? projectData.Name
                : projectData?.Chapters?.FirstOrDefault()?.Name;

            return new ProjectFile
            {
                Name = name,
                ProjectName = name,
                ProjectVersion = projectData?.Version
            };
        }

        private static string GetRecoveredPlainText(ProjectData projectData)
        {
            return projectData?.Chapters?.FirstOrDefault()?.Text ?? string.Empty;
        }

        private bool TryResolveHandler(StorageFile file, out DocumentPersistenceHandlerBase handler)
        {
            handler = null;
            return file != null && _handlers.TryGetValue(file.FileType, out handler);
        }

        private async Task<StorageFile> OpenFileExplorerLoadAsync()
        {
            var file = await _fileService.PickFileForOpenAsync();
            if (file == null)
                return null;

            if (!ProjectFile.CheckIfProjectExists(file))
                ProjectFile.New(file);

            return file;
        }

        private void CompleteAfterSave()
        {
            var pendingAction = _afterSaveAction;
            _afterSaveAction = AfterSaveAction.None;

            _undoRedo.MarkClean();

            switch (pendingAction)
            {
                case AfterSaveAction.None:
                    _events.Publish(new TitleBarUpdateEvent());
                    break;
                case AfterSaveAction.ClearEverything:
                    CurrentProject = null;
                    _dialogs.ClearEverything();
                    _dialogs.OpenLoadDialogue();
                    break;
                case AfterSaveAction.Exit:
                    _windowManager.Close(_windowContext);
                    break;
            }

            _notifications.HideProgressBar();
        }

        private void LoadVariables(ProjectData projectData)
        {
            // Route selection through ITextEditorService so persistence has no VM dependency.
            // The ChaptersListViewModel observes SelectedChapterIndex via its own bindings.
            _textEditor.SelectedChapterIndex = projectData.LastOpenedChapter;
        }

        private void OnProjectLoaded()
        {
            _undoRedo.MarkClean();
            SavedValues.Clear();
            _events.Publish(new TitleBarUpdateEvent());

            _notifications.HideProgressBar();
        }

        private void OnAutosaveTimerTick(object sender, object e)
        {
            _ = TryAutosaveAsync();
        }

        private async Task TryAutosaveAsync()
        {
            if (!SettingsValues.autosaveEnabled || !_undoRedo.IsDirty || CurrentProject?.file == null || _afterSaveAction != AfterSaveAction.None)
                return;

            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.Warning($"Autosave failed: {ex.Message}");
            }
        }

        private void StopAutosaveTimer()
        {
            if (_autosaveTimer == null)
                return;

            _autosaveTimer.Tick -= OnAutosaveTimerTick;
            _autosaveTimer.Stop();
            _autosaveTimer = null;
        }

        private static TimeSpan GetAutosaveInterval()
        {
            var interval = SettingsValues.autosaveInterval;
            return interval >= 1
                ? TimeSpan.FromMinutes(interval)
                : TimeSpan.FromSeconds(interval * 60);
        }

        private void ShowSaveErrorNotification()
        {
            _notifications.ShowNotification(
                Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                ResourceLoader.GetForViewIndependentUse().GetString("saveSaveSystemErrorText"));
        }

        private void ShowLoadErrorNotification()
        {
            _notifications.ShowNotification(
                Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                ResourceLoader.GetForViewIndependentUse().GetString("loadSaveSystemErrorText"));
        }

        private static ProjectData NormalizeProjectData(ProjectData projectData)
        {
            projectData ??= new ProjectData();
            projectData.Chapters ??= new List<ChapterData>();
            projectData.Characters ??= new List<CharacterData>();
            projectData.PinboardConnections ??= new List<PinboardConnectionData>();
            projectData.PlotThreads ??= new List<string>();

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
    }
}
