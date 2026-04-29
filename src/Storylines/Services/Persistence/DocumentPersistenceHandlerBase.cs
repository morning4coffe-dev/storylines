using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services.Interfaces;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Storylines.Services.Persistence
{
    internal abstract class DocumentPersistenceHandlerBase
    {
        protected DocumentPersistenceHandlerBase(
            IFileService fileService,
            IDialogService dialogs,
            EventAggregator events,
            ILogger logger,
            ProjectState projectState,
            ITextEditorService textEditor)
        {
            FileService = fileService;
            Dialogs = dialogs;
            Events = events;
            Logger = logger;
            ProjectState = projectState;
            TextEditor = textEditor;
        }

        protected IFileService FileService { get; }
        protected IDialogService Dialogs { get; }
        protected EventAggregator Events { get; }
        protected ILogger Logger { get; }
        protected ProjectState ProjectState { get; }
        protected ITextEditorService TextEditor { get; }

        public abstract Task SaveAsync(ProjectFile project);

        public abstract Task LoadAsync(ProjectFile project);

        protected static void ShowLoadErrorNotification()
        {
            NotificationManager.DisplayInAppNotification(
                Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                ResourceLoader.GetForViewIndependentUse().GetString("loadSaveSystemErrorText"),
                "");
        }
    }
}