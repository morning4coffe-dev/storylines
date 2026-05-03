using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Storylines.Models;

namespace Storylines
{
    public sealed partial class App : Application
    {
        private Task _telemetryInitializationTask;
        private bool _isWindowInitialized;

        public static new App Current => Application.Current as App;

        internal static IStorageItem PendingActivatedItem { get; set; }

        public IServiceProvider Services { get; }

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
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

        /// <param name="e">Details about the launch request and process</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = EnsureRootFrame();
            if (e.PrelaunchActivated)
                return;

            var hasRecoveryData = RecoveryService.HasRecoveryData();

            EnsureShell(rootFrame, e.Arguments, "OnLaunched");
            await ActivateAsync(e, "launch", !hasRecoveryData);

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
            if (Window.Current.Content is Frame existingRootFrame)
                return existingRootFrame;

            var rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            Window.Current.Content = rootFrame;
            return rootFrame;
        }

        private void EnsureShell(Frame rootFrame, string arguments, string activationSource)
        {
            if (rootFrame.Content != null)
                return;

            if (!rootFrame.Navigate(typeof(AppView), arguments))
                throw new Exception($"Failed to create initial page ({activationSource})");
        }

        private async Task ActivateAsync(IActivatedEventArgs activationArgs, string activationKind, bool openLoadDialog = true)
        {
            LanguageCheck();

            Window.Current.Activate();
            SystemInformation.Instance.TrackAppUse(activationArgs);

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
            var applicationView = ApplicationView.GetForCurrentView();
            applicationView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            applicationView.IsScreenCaptureEnabled = true;
            applicationView.TitleBar.ButtonBackgroundColor = Colors.Transparent;

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(AppView.current?.appTitleBar);
            AppView.current?.UsingWindows10();

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
                SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequest;
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

        private void OnCloseRequest(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
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

        private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            App.TryGetService<ITelemetryService>()?.TrackUnhandledException(e.Exception, e.Message);

            e.Handled = true;
        }

        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <param name="sender">The source of the suspend request</param>
        /// <param name="e">Details about the suspend request</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            RecoveryService.Stop();

            try
            {
                if (TimeTravelSystem.unSavedProgress)
                    await RecoveryService.CacheCurrentStateAsync();
            }
            catch (Exception ex)
            {
                GetService<Storylines.Services.Interfaces.ILogger>()?.Warning($"Recovery cache on suspend failed: {ex.Message}");
            }
            finally
            {
                deferral.Complete();
            }
        }

        protected override async void OnFileActivated(FileActivatedEventArgs args)
        {
            PendingActivatedItem = args.Files.FirstOrDefault();

            var rootFrame = EnsureRootFrame();
            EnsureShell(rootFrame, string.Empty, "OnFileActivated");
            await ActivateAsync(args, "file_activation");
        }

        private enum RecoveryStartupChoice
        {
            Restore,
            Discard,
            Cancel
        }
    }
}
