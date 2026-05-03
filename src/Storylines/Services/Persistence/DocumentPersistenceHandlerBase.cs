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
            ITextEditorService textEditor,
            INotificationService notifications)
        {
            FileService = fileService;
            Dialogs = dialogs;
            Events = events;
            Logger = logger;
            ProjectState = projectState;
            TextEditor = textEditor;
            Notifications = notifications;
        }

        protected IFileService FileService { get; }
        protected IDialogService Dialogs { get; }
        protected EventAggregator Events { get; }
        protected ILogger Logger { get; }
        protected ProjectState ProjectState { get; }
        protected ITextEditorService TextEditor { get; }
        protected INotificationService Notifications { get; }

        public abstract Task SaveAsync(ProjectFile project);

        public abstract Task LoadAsync(ProjectFile project);

        protected void ShowLoadErrorNotification()
        {
            Notifications.ShowNotification(
                Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                ResourceLoader.GetForViewIndependentUse().GetString("loadSaveSystemErrorText"));
        }
    }
}