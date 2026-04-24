using Storylines.Services.Interfaces;
using Storylines.Services.Serializers;
using Storylines.Models;
using Storylines.ViewModels;

namespace Storylines.Services
{
    public static class ServiceLocator
    {
        public static ILogger Logger { get; private set; }
        public static IFileService FileService { get; private set; }
        public static INotificationService NotificationService { get; private set; }
        public static ISaveSerializer JsonSerializer { get; private set; }
        public static ISaveSerializer LegacySerializer { get; private set; }
        public static ProjectState ProjectState { get; private set; }
        public static EventAggregator Events { get; private set; }
        public static ITextEditorService TextEditor { get; private set; }
        public static INavigationService Navigation { get; private set; }
        public static IDialogService Dialogs { get; private set; }

        // ViewModels
        public static AppViewModel AppViewModel { get; private set; }
        public static MainPageViewModel MainPageViewModel { get; private set; }
        public static ChaptersListViewModel ChaptersListViewModel { get; private set; }
        public static CommandBarViewModel CommandBarViewModel { get; private set; }

        public static void Initialize()
        {
            Logger = new DebugLogger();
            FileService = new Services.FileService();
            JsonSerializer = new JsonSaveSerializer();
            LegacySerializer = new LegacySrlSerializer();
            ProjectState = new ProjectState();
            Events = new EventAggregator();
            TextEditor = new TextEditorService();
            Navigation = new NavigationService();
            Dialogs = new DialogService();

            // Initialize ViewModels after services are ready
            AppViewModel = new AppViewModel();
            MainPageViewModel = new MainPageViewModel();
            ChaptersListViewModel = new ChaptersListViewModel();
            CommandBarViewModel = new CommandBarViewModel();
        }

        /// <summary>
        /// Called after the UI Frame is available to wire up NavigationService.
        /// </summary>
        public static void InitializeNavigation(Windows.UI.Xaml.Controls.Frame frame)
        {
            (Navigation as NavigationService)?.Initialize(frame);
        }
    }
}
