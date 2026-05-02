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
            services.AddSingleton<IDispatcherService, WinUIDispatcherService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IUndoRedoService, UndoRedoService>();
            services.AddSingleton<ITelemetryProvider, AppCenterTelemetryProvider>();
            services.AddSingleton<ITelemetryService, TelemetryService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IFilePickerService, FilePickerService>();
            services.AddSingleton<IExportService, ExportService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            services.AddSingleton<JsonSaveSerializer>();
            services.AddSingleton<LegacySrlSerializer>();
            services.AddSingleton<ProjectState>();
            services.AddSingleton<EventAggregator>();
            services.AddSingleton<IProjectPersistenceService, ProjectPersistenceService>();
            services.AddSingleton<ITextEditorService, TextEditorService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IChapterWorkflowService, ChapterWorkflowService>();
            services.AddSingleton<IDictationService, DictationService>();
            services.AddSingleton<ISpeechService, SpeechService>();
            services.AddSingleton<IWritingStatsService, WritingStatsService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ICommandRegistry, CommandRegistry>();
            services.AddSingleton<IInspectorViewModel, ViewModels.InspectorViewModel>();
            services.AddSingleton<ISnapshotService, SnapshotService>();
            services.AddSingleton<EditorModeService>();

            services.AddSingleton<AppViewModel>();
            services.AddSingleton<MainPageViewModel>();
            services.AddSingleton<ChaptersListViewModel>();
            services.AddSingleton<CommandBarViewModel>();
            services.AddSingleton<SpeechHubViewModel>();
            services.AddTransient<ExportDialogViewModel>();
            services.AddTransient<CharactersPageViewModel>();
            services.AddTransient<ViewModels.Settings.GeneralSettingsViewModel>();
            services.AddTransient<ViewModels.Settings.PersonalizationSettingsViewModel>();
            services.AddTransient<ViewModels.Settings.AccessibilitySettingsViewModel>();

            return services.BuildServiceProvider();
        }
    }
}