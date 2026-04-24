using Microsoft.Extensions.DependencyInjection;
using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.Services.Modes;
using Storylines.Services.Serializers;
using Storylines.ViewModels;
using System;

namespace Storylines.Services
{
    internal static class ServiceConfiguration
    {
        public static IServiceProvider Configure()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ILogger, DebugLogger>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<JsonSaveSerializer>();
            services.AddSingleton<LegacySrlSerializer>();
            services.AddSingleton<ProjectState>();
            services.AddSingleton<EventAggregator>();
            services.AddSingleton<ITextEditorService, TextEditorService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IBranchingDialogueStore, ProjectStateBranchingDialogueStore>();
            services.AddSingleton<IBranchingDialogueEventPublisher, BranchingDialogueEventPublisher>();
            services.AddSingleton<IBranchingDialogueService, BranchingDialogueService>();
            services.AddSingleton<EditorModeService>();

            services.AddSingleton<AppViewModel>();
            services.AddSingleton<MainPageViewModel>();
            services.AddSingleton<ChaptersListViewModel>();
            services.AddSingleton<CommandBarViewModel>();
            services.AddSingleton<BranchingDialogueViewModel>();
            services.AddTransient<CharactersPageViewModel>();

            return services.BuildServiceProvider();
        }
    }
}