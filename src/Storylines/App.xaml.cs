using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Microsoft.Windows.AppLifecycle;

namespace Storylines
{
    public partial class App : Application
    {
        private Task _telemetryInitializationTask;
        private bool _isWindowInitialized;

        public static new App Current => Application.Current as App;

        public static Window MainWindow { get; private set; }

        internal static IStorageItem PendingActivatedItem { get; set; }

        public IServiceProvider Services { get; }

        public App()
        {
            InitializeComponent();
            UnhandledException += App_UnhandledException;

            Services = ServiceConfiguration.Configure();
        }

        public static T GetService<T>() where T : notnull
        {
            if (Current?.Services is null)
                throw new InvalidOperationException("Application services have not been configured.");

            return Current.Services.GetRequiredService<T>();
        }

        public static T TryGetService<T>() where T : class
            => Current?.Services?.GetService<T>();

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new Window();
            MainWindow.Title = "Storylines";
            MainWindow.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

            var rootFrame = EnsureRootFrame();

            var hasRecoveryData = RecoveryService.HasRecoveryData();

            EnsureShell(rootFrame, string.Empty, "OnLaunched");
            await ActivateAsync("launch", !hasRecoveryData);

            // Handle file activation via AppLifecycle
            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            if (activatedArgs?.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.File)
            {
                var fileArgs = activatedArgs.Data as Windows.ApplicationModel.Activation.IFileActivatedEventArgs;
                if (fileArgs?.Files?.Count > 0)
                    PendingActivatedItem = fileArgs.Files.FirstOrDefault();
            }

            if (hasRecoveryData)
            {
                if (!await TryRestoreRecoveryAsync() && !await LoadLastProjectAsync())
                    GetService<IDialogService>().OpenLoadDialogue();

                return;
            }

            _ = LoadLastProjectAsync();
        }

        private void LanguageCheck()
        {
            if (!string.IsNullOrWhiteSpace(SettingsValues.language))
            {
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = SettingsValues.language;
                return;
            }

            var preferredLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages.FirstOrDefault();
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride =
                !string.IsNullOrWhiteSpace(preferredLanguage) && SettingsValues.IsUserLanguageSupported()
                    ? preferredLanguage
                    : "en";
        }

        private Frame EnsureRootFrame()
        {
            if (MainWindow.Content is Frame existingRootFrame)
                return existingRootFrame;

            var rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            MainWindow.Content = rootFrame;
            return rootFrame;
        }

        private void EnsureShell(Frame rootFrame, string arguments, string activationSource)
        {
            if (rootFrame.Content != null)
                return;

            if (!rootFrame.Navigate(typeof(AppView), arguments))
                throw new Exception($"Failed to create initial page ({activationSource})");
        }

        private async Task ActivateAsync(string activationKind, bool openLoadDialog = true)
        {
            LanguageCheck();

            MainWindow.Activate();

            ConfigureCurrentWindow(activationKind, openLoadDialog);
            await TryProcessPendingActivationItemAsync();
        }

        private async Task<bool> TryRestoreRecoveryAsync()
        {
            if (!RecoveryService.HasRecoveryData())
                return false;

            switch (await ShowRecoveryRestoreDialogAsync())
            {
                case RecoveryStartupChoice.Restore:
                    return await GetService<IProjectPersistenceService>().TryRestoreRecoveryAsync();
                case RecoveryStartupChoice.Discard:
                    await RecoveryService.ClearRecoveryDataAsync();
                    return false;
                default:
                    return false;
            }
        }

        private static async Task<RecoveryStartupChoice> ShowRecoveryRestoreDialogAsync()
        {
            var resources = ResourceLoader.GetForViewIndependentUse();
            var recoveryDialog = new ContentDialog
            {
                Title = resources.GetString("recoveryRestoreDialogTitle"),
                Content = resources.GetString("recoveryRestoreDialogDescription"),
                PrimaryButtonText = resources.GetString("recoveryRestoreDialogRestore"),
                SecondaryButtonText = resources.GetString("recoveryRestoreDialogDiscard"),
                CloseButtonText = resources.GetString("recoveryRestoreDialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = AppView.current?.ActualTheme ?? ElementTheme.Default,
                XamlRoot = MainWindow.Content.XamlRoot,
            };

            AppView.currentlyOpenedDialogue = recoveryDialog;

            try
            {
                switch (await recoveryDialog.ShowAsync())
                {
                    case ContentDialogResult.Primary:
                        return RecoveryStartupChoice.Restore;
                    case ContentDialogResult.Secondary:
                        return RecoveryStartupChoice.Discard;
                    default:
                        return RecoveryStartupChoice.Cancel;
                }
            }
            finally
            {
                AppView.currentlyOpenedDialogue = null;
            }
        }

        private void ConfigureCurrentWindow(string activationKind, bool openLoadDialog)
        {
            // Configure title bar
            MainWindow.ExtendsContentIntoTitleBar = true;
            MainWindow.SetTitleBar(AppView.current?.appTitleBar);
            AppView.current?.UsingWindows10();

            // Configure close handling
            MainWindow.Closed += OnWindowClosed;

            var telemetry = GetService<ITelemetryService>();
            if (_telemetryInitializationTask == null)
            {
                _telemetryInitializationTask = telemetry.InitializeAsync();
                ObserveBackgroundOperation(_telemetryInitializationTask, "Failed to initialize telemetry");
            }

            if (!_isWindowInitialized)
            {
                SettingsValues.LoadSettings();
                ThemeSettings.Initialize();
                if (openLoadDialog)
                    LoadProjectDialogue.Open();
                MicrosoftStoreFunctions.InitializeReview();
                ObserveBackgroundOperation(MicrosoftStoreFunctions.CheckForNewUpdateAvailableAsync(), "Failed to check for updates");
                _isWindowInitialized = true;
            }

            telemetry.TrackAppStarted(activationKind);
        }

        private void ObserveBackgroundOperation(Task task, string operationName)
        {
            _ = ObserveBackgroundOperationAsync(task, operationName);
        }

        private static async Task ObserveBackgroundOperationAsync(Task task, string operationName)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                App.TryGetService<Storylines.Services.Interfaces.ILogger>()?.Warning($"{operationName}: {ex.Message}");
            }
        }

        private async Task<bool> LoadLastProjectAsync()
        {
            var fileToken = Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart]?.ToString();
            if (string.IsNullOrWhiteSpace(fileToken))
                return false;

            try
            {
                var file = await ProjectFile.GetProjectFromTokenAsync(fileToken);
                if (file == null)
                    return false;

                GetService<IProjectPersistenceService>().Load(await ProjectFile.LoadExistingAsync(file, fileToken));
                return true;
            }
            catch (Exception ex)
            {
                GetService<Storylines.Services.Interfaces.ILogger>()?.Warning($"Failed to load last project: {ex.Message}");
                return false;
            }
        }

        private Task TryProcessPendingActivationItemAsync()
        {
            var pendingActivatedItem = PendingActivatedItem;
            if (pendingActivatedItem == null || Views.Pages.MainPage.Current == null)
                return Task.CompletedTask;

            PendingActivatedItem = null;
            GetService<IProjectPersistenceService>().DefaultLaunch(pendingActivatedItem);
            return Task.CompletedTask;
        }

        private void OnWindowClosed(object sender, WindowEventArgs e)
        {
            var blockedByUnsavedChanges = TimeTravelSystem.unSavedProgress && SettingsValues.exitDiagEnabled;
            App.TryGetService<ITelemetryService>()?.TrackAppClosingRequested(blockedByUnsavedChanges);

            if (blockedByUnsavedChanges)
            {
                e.Handled = true;
                ObserveBackgroundOperation(ShowUnsavedProgressDialogAsync(), "Failed to display unsaved progress dialog");
            }

            NotificationManager.ClearBadgeNotification();
        }

        private static async Task ShowUnsavedProgressDialogAsync()
        {
            await NotificationManager.DisplayUnsavedProgressDialogue(true);
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            App.TryGetService<ITelemetryService>()?.TrackUnhandledException(e.Exception, e.Message);

            e.Handled = true;
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private enum RecoveryStartupChoice
        {
            Restore,
            Discard,
            Cancel
        }
    }
}
