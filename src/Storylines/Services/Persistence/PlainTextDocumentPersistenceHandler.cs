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
            Dialogs.ClearEverything();
            Dialogs.DismissLoadDialogue();

            try
            {
                var text = await FileService.ReadAsync(project.file);

                ProjectState.AddExistingChapter(project.file.DisplayName, Guid.NewGuid().ToString(), text);
                TextEditor.SelectedChapterIndex = 0;

                _onLoaded();
                Events.Publish(new ToolsStateChangedEvent { IsStorylinesDocument = false });
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load plain text document", ex);
                ShowLoadErrorNotification();
                NotificationManager.UpdateMainProgressBar(0, NotificationManager.ProgressState.Error);
            }
        }
    }
}