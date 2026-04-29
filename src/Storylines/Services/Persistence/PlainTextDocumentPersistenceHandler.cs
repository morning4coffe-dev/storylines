using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Storylines.Services.Persistence
{
    internal sealed class PlainTextDocumentPersistenceHandler : DocumentPersistenceHandlerBase
    {
        private readonly Action _onLoaded;

        public PlainTextDocumentPersistenceHandler(
            IFileService fileService,
            IDialogService dialogs,
            EventAggregator events,
            ILogger logger,
            ProjectState projectState,
            ITextEditorService textEditor,
            Action onLoaded)
            : base(fileService, dialogs, events, logger, projectState, textEditor)
        {
            _onLoaded = onLoaded;
        }

        public override async Task SaveAsync(ProjectFile project)
        {
            var text = TextEditor.GetText(TextFormat.PlainText);
            await FileService.WriteAsync(project.file, text);
            Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = false });
        }

        public override async Task LoadAsync(ProjectFile project)
        {
            try
            {
                var text = await FileService.ReadAsync(project.file);
                await LoadTextAsync(project, text);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load plain text document", ex);
                ShowLoadErrorNotification();
                NotificationManager.UpdateMainProgressBar(0, NotificationManager.ProgressState.Error);
            }
        }

        public Task LoadTextAsync(ProjectFile project, string text)
        {
            Dialogs.ClearEverything();
            Dialogs.DismissLoadDialogue();

            try
            {
                var chapterName = project?.file?.DisplayName ?? project?.projectName ?? project?.Name ?? string.Empty;
                ProjectState.AddExistingChapter(chapterName, Guid.NewGuid().ToString(), text ?? string.Empty);
                TextEditor.SelectedChapterIndex = 0;

                _onLoaded();
                Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = false });
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to restore plain text recovery data", ex);
                ShowLoadErrorNotification();
                NotificationManager.UpdateMainProgressBar(0, NotificationManager.ProgressState.Error);
            }

            return Task.CompletedTask;
        }
    }
}