using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.Services.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storylines.Services.Persistence
{
    internal sealed class StorylinesDocumentPersistenceHandler : DocumentPersistenceHandlerBase
    {
        private readonly JsonSaveSerializer _jsonSerializer;
        private readonly LegacySrlSerializer _legacySerializer;
        private readonly Func<ProjectData> _collectProjectData;
        private readonly Func<ProjectData, ProjectData> _normalizeProjectData;
        private readonly Action<ProjectData> _loadVariables;
        private readonly Action _onLoaded;

        public StorylinesDocumentPersistenceHandler(
            IFileService fileService,
            IDialogService dialogs,
            EventAggregator events,
            ILogger logger,
            ProjectState projectState,
            ITextEditorService textEditor,
            JsonSaveSerializer jsonSerializer,
            LegacySrlSerializer legacySerializer,
            Func<ProjectData> collectProjectData,
            Func<ProjectData, ProjectData> normalizeProjectData,
            Action<ProjectData> loadVariables,
            Action onLoaded,
            INotificationService notifications)
            : base(fileService, dialogs, events, logger, projectState, textEditor, notifications)
        {
            _jsonSerializer = jsonSerializer;
            _legacySerializer = legacySerializer;
            _collectProjectData = collectProjectData;
            _normalizeProjectData = normalizeProjectData;
            _loadVariables = loadVariables;
            _onLoaded = onLoaded;
        }

        public override async Task SaveAsync(ProjectFile project)
        {
            var projectData = _collectProjectData();
            await FileService.WriteAsync(project.file, _jsonSerializer.Serialize(projectData));
            Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = true });
        }

        public override async Task LoadAsync(ProjectFile project)
        {
            Dialogs.ClearEverything();
            Dialogs.DismissLoadDialogue();

            try
            {
                var content = await FileService.ReadAsync(project.file);
                ProjectData projectData;

                if (_jsonSerializer.CanDeserialize(content))
                {
                    projectData = _normalizeProjectData(_jsonSerializer.Deserialize(content));
                    Logger.Info("Loaded project in JSON format");
                }
                else if (_legacySerializer.CanDeserialize(content))
                {
                    projectData = _normalizeProjectData(_legacySerializer.Deserialize(content));
                    Logger.Info("Loaded project in legacy SRL format - will save as JSON on next save");
                }
                else
                {
                    Logger.Error("Unable to detect file format for: " + project.file.Name);
                    ShowLoadErrorNotification();
                    Notifications.UpdateProgressBar(0, Storylines.Services.Interfaces.ProgressBarState.Error);
                    return;
                }

                await ApplyProjectDataAsync(project, projectData);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load Storylines document", ex);
                ShowLoadErrorNotification();
                Notifications.UpdateProgressBar(0, Storylines.Services.Interfaces.ProgressBarState.Error);
            }
        }

        public async Task LoadProjectDataAsync(ProjectFile project, ProjectData projectData)
        {
            Dialogs.ClearEverything();
            Dialogs.DismissLoadDialogue();

            try
            {
                await ApplyProjectDataAsync(project, _normalizeProjectData(projectData));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to restore Storylines recovery data", ex);
                ShowLoadErrorNotification();
                Notifications.UpdateProgressBar(0, Storylines.Services.Interfaces.ProgressBarState.Error);
            }
        }

        private async Task ApplyProjectDataAsync(ProjectFile project, ProjectData projectData)
        {
            project.projectVersion = projectData.Version;
            project.projectName = projectData.Name;

            foreach (var charData in projectData.Characters)
            {
                var picture = !string.IsNullOrEmpty(charData.PictureFileName)
                    ? new CharacterPicture { FileName = charData.PictureFileName }
                    : null;

                await ProjectState.AddExistingCharacterAsync(
                    charData.Name,
                    Guid.NewGuid().ToString(),
                    charData.Description,
                    picture,
                    charData.Role,
                    charData.Age,
                    charData.Appearance,
                    charData.Traits);
            }

            foreach (var chapterData in projectData.Chapters)
            {
                var status = ChapterStatus.Draft;
                if (!string.IsNullOrEmpty(chapterData.Status))
                    Enum.TryParse(chapterData.Status, true, out status);

                var chapterToken = !string.IsNullOrWhiteSpace(chapterData.Id)
                    ? chapterData.Id
                    : Guid.NewGuid().ToString();

                ProjectState.AddExistingChapter(
                    chapterData.Name,
                    chapterToken,
                    chapterData.Text,
                    chapterData.Notes,
                    chapterData.Synopsis,
                    chapterData.WordCountGoal,
                    chapterData.Tags,
                    chapterData.PinboardX ?? 0,
                    chapterData.PinboardY ?? 0,
                    status,
                    chapterData.Location,
                    chapterData.PlotThreads,
                    chapterData.LastCaretPosition ?? 0,
                    chapterData.LastVerticalOffset ?? 0);
            }

            ProjectState.PinboardConnections = projectData.PinboardConnections ?? new List<PinboardConnectionData>();
            ProjectState.PlotThreads = projectData.PlotThreads ?? new List<string>();

            for (var charIndex = 0; charIndex < projectData.Characters.Count && charIndex < ProjectState.Characters.Count; charIndex++)
            {
                var charData = projectData.Characters[charIndex];
                if (charData.Relationships is null)
                    continue;

                var character = ProjectState.Characters[charIndex];
                character.Relationships = charData.Relationships
                    .Select(relationship =>
                    {
                        var target = ProjectState.Characters.FirstOrDefault(existing =>
                            string.Equals(existing.Name, relationship.TargetName, StringComparison.CurrentCultureIgnoreCase));

                        return target is not null
                            ? new CharacterRelationship { TargetCharacterToken = target.Token, Type = relationship.Type }
                            : null;
                    })
                    .Where(relationship => relationship is not null)
                    .ToList();
            }

            _loadVariables(projectData);
            _onLoaded();
            Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = true });
        }
    }
}