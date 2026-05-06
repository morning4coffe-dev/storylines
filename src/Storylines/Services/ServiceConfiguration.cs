using Microsoft.Extensions.DependencyInjection;
using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.Services.Modes;
using Storylines.Services.Serializers;
using Storylines.ViewModels;
using Storylines.ViewModels.Settings;
using System;

namespace Storylines.Services
{
    internal static class ServiceConfiguration
    {
        public static IServiceProvider Configure()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ILogger, DebugLogger>();
            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddSingleton<ITelemetryProvider, AppCenterTelemetryProvider>();
            services.AddSingleton<JsonSaveSerializer>();
            services.AddSingleton<LegacySrlSerializer>();

            services.AddScoped<WindowContext>();
            services.AddScoped<IDispatcherService, WinUIDispatcherService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IUndoRedoService, UndoRedoService>();
            services.AddScoped<ITelemetryService, TelemetryService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IFilePickerService, FilePickerService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IAppSettingsService, AppSettingsService>();
            services.AddScoped<ProjectState>();
            services.AddScoped<EventAggregator>();
            services.AddScoped<IProjectPersistenceService, ProjectPersistenceService>();
            services.AddScoped<ITextEditorService, TextEditorService>();
            services.AddScoped<INavigationService, NavigationService>();
            services.AddScoped<IDialogService, DialogService>();
            services.AddScoped<IShellService, ShellService>();
            services.AddScoped<IChapterWorkflowService, ChapterWorkflowService>();
            services.AddScoped<IDictationService, DictationService>();
            services.AddScoped<ISpeechService, SpeechService>();
            services.AddScoped<IWritingStatsService, WritingStatsService>();
            services.AddScoped<IThemeService, ThemeService>();
            services.AddScoped<ICommandRegistry, CommandRegistry>();
            services.AddScoped<IInspectorViewModel, ViewModels.InspectorViewModel>();
            services.AddScoped<ISnapshotService, SnapshotService>();
            services.AddScoped<EditorModeService>();

            services.AddScoped<AppViewModel>();
            services.AddScoped<MainPageViewModel>();
            services.AddScoped<ChaptersListViewModel>();
            services.AddScoped<CommandBarViewModel>();
            services.AddScoped<SpeechHubViewModel>();
            services.AddTransient<ExportDialogViewModel>();
            services.AddTransient<CharactersPageViewModel>();
            services.AddTransient<ViewModels.Settings.GeneralSettingsViewModel>();
            services.AddTransient<ViewModels.Settings.PersonalizationSettingsViewModel>();
            services.AddTransient<ViewModels.Settings.AccessibilitySettingsViewModel>();

#if PRIVATE_PLUGINS
            services.AddScoped<Interfaces.IBranchingDialogueStore, ProjectStateBranchingDialogueStore>();
            services.AddScoped<Interfaces.IBranchingDialogueEventPublisher, BranchingDialogueEventPublisher>();
            services.AddScoped<Interfaces.IBranchingDialogueService, BranchingDialogueService>();
            services.AddTransient<ViewModels.BranchingDialogueViewModel>();
#endif

            return services.BuildServiceProvider();
        }
    }
}
