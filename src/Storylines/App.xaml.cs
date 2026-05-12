using Microsoft.Extensions.DependencyInjection;
using Storylines.Views.Dialogs;
using Windows.ApplicationModel.Activation;
using Microsoft.Windows.AppLifecycle;

namespace Storylines;

public partial class App : Application
{
    private Task _telemetryInitializationTask;
    private bool _isAppActivationSubscribed;

    public static new App Current => Application.Current as App;

    public static Window MainWindow =>
        Current?.Services?.GetService<IWindowManager>()?.Current?.Window;

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

        var scopedServices = Current.Services.GetService<IWindowManager>()?.Current?.Services;
        if (scopedServices is not null && scopedServices.GetService<T>() is T scopedService)
            return scopedService;

        return Current.Services.GetRequiredService<T>();
    }

    public static T TryGetService<T>() where T : class
    {
        var scopedServices = Current?.Services?.GetService<IWindowManager>()?.Current?.Services;
        return scopedServices?.GetService<T>() ?? Current?.Services?.GetService<T>();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        EnsureAppActivationSubscription();
        LanguageCheck();
        var hasRecoveryData = RecoveryService.HasRecoveryData();
        var pendingItem = GetActivatedStorageItem(AppInstance.GetCurrent().GetActivatedEventArgs());
        var windowManager = Services.GetRequiredService<IWindowManager>();
        var context = windowManager.CreateDocumentWindow(pendingItem, "OnLaunched");

        await windowManager.RunAsync(context, async () =>
        {
            await ActivateAsync(context, "launch", !hasRecoveryData && pendingItem is null);

            if (hasRecoveryData)
            {
                if (!await TryRestoreRecoveryAsync() && !await LoadLastProjectAsync())
                    GetService<IDialogService>().OpenLoadDialogue();

                return;
            }

            if (pendingItem is null)
                _ = LoadLastProjectAsync();
        });
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

    private void EnsureAppActivationSubscription()
    {
        if (_isAppActivationSubscribed)
            return;

        AppInstance.GetCurrent().Activated += OnAppActivated;
        _isAppActivationSubscribed = true;
    }

    private void OnAppActivated(object sender, AppActivationArguments args)
    {
        var pendingItem = GetActivatedStorageItem(args);
        if (pendingItem is null)
            return;

        var windowManager = Services.GetRequiredService<IWindowManager>();
        var context = windowManager.CreateDocumentWindow(pendingItem, "FileActivated");
        _ = windowManager.RunAsync(context, () => ActivateAsync(context, "file", false));
    }

    private static IStorageItem GetActivatedStorageItem(AppActivationArguments activatedArgs)
    {
        if (activatedArgs?.Kind != ExtendedActivationKind.File)
            return null;

        var fileArgs = activatedArgs.Data as IFileActivatedEventArgs;
        return fileArgs?.Files?.FirstOrDefault();
    }

    private async Task ActivateAsync(WindowContext context, string activationKind, bool openLoadDialog = true)
    {
        context.Window.Activate();

        ConfigureCurrentWindow(context, activationKind, openLoadDialog);
        await TryProcessPendingActivationItemAsync(context);
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
        switch (await GetService<IDialogService>().ShowMessageAsync(new DialogDefinition
        {
            Title = resources.GetString("recoveryRestoreDialogTitle"),
            Content = resources.GetString("recoveryRestoreDialogDescription"),
            PrimaryButtonText = resources.GetString("recoveryRestoreDialogRestore"),
            SecondaryButtonText = resources.GetString("recoveryRestoreDialogDiscard"),
            CloseButtonText = resources.GetString("recoveryRestoreDialogCancel"),
            DefaultButton = ContentDialogButton.Primary,
        }))
        {
            case ContentDialogResult.Primary:
                return RecoveryStartupChoice.Restore;
            case ContentDialogResult.Secondary:
                return RecoveryStartupChoice.Discard;
            default:
                return RecoveryStartupChoice.Cancel;
        }
    }

    private void ConfigureCurrentWindow(WindowContext context, string activationKind, bool openLoadDialog)
    {
        // Configure title bar
        context.Window.ExtendsContentIntoTitleBar = true;
        context.Window.SetTitleBar(context.AppView?.appTitleBar);
        context.AppView?.UsingWindows10();

        var telemetry = GetService<ITelemetryService>();
        if (_telemetryInitializationTask is null)
        {
            _telemetryInitializationTask = telemetry.InitializeAsync();
            ObserveBackgroundOperation(_telemetryInitializationTask, "Failed to initialize telemetry");
        }

        if (!context.IsInitialized)
        {
            SettingsValues.LoadSettings();
            ThemeSettings.Initialize();
            if (openLoadDialog)
                GetService<IDialogService>().OpenLoadDialogue();
            MicrosoftStoreFunctions.InitializeReview();
            ObserveBackgroundOperation(MicrosoftStoreFunctions.CheckForNewUpdateAvailableAsync(), "Failed to check for updates");
            context.IsInitialized = true;
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
        var fileToken = App.GetService<Storylines.Services.Interfaces.IPreferencesService>().Get<string>(SettingsValueStrings.LoadLastProjectOnStart);
        if (string.IsNullOrWhiteSpace(fileToken))
            return false;

        try
        {
            var file = await ProjectFile.GetProjectFromTokenAsync(fileToken);
            if (file is null)
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

    private Task TryProcessPendingActivationItemAsync(WindowContext context)
    {
        var pendingActivatedItem = context.PendingActivatedItem ?? PendingActivatedItem;
        if (pendingActivatedItem is null || context.MainPage is null)
            return Task.CompletedTask;

        context.PendingActivatedItem = null;
        if (ReferenceEquals(PendingActivatedItem, pendingActivatedItem))
            PendingActivatedItem = null;
        GetService<IProjectPersistenceService>().DefaultLaunch(pendingActivatedItem);
        return Task.CompletedTask;
    }

    internal void OnWindowClosed(WindowContext context, WindowEventArgs e)
    {
        var windowManager = Services.GetRequiredService<IWindowManager>();
        using (windowManager.Enter(context))
        {
            var undoRedo = GetService<IUndoRedoService>();
            var hasUnsavedChanges = undoRedo.IsDirty;
            var shouldAutosaveOnClose = hasUnsavedChanges && SettingsValues.autosaveEnabled;
            var blockedByUnsavedChanges = hasUnsavedChanges && !shouldAutosaveOnClose && SettingsValues.exitDiagEnabled;
            App.TryGetService<ITelemetryService>()?.TrackAppClosingRequested(blockedByUnsavedChanges);

            if (shouldAutosaveOnClose)
            {
                e.Handled = true;
                GetService<IProjectPersistenceService>().SaveAndExitOrClearAll(true);
                return;
            }

            if (blockedByUnsavedChanges)
            {
                e.Handled = true;
                ObserveBackgroundOperation(ShowUnsavedProgressDialogAsync(), "Failed to display unsaved progress dialog");
                return;
            }

            NotificationManager.ClearBadgeNotification();
        }

        (windowManager as WindowManager)?.Remove(context);
    }

    private static async Task ShowUnsavedProgressDialogAsync()
    {
        await App.GetService<IDialogService>().ShowUnsavedProgressDialogueAsync(true);
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        App.TryGetService<ITelemetryService>()?.TrackUnhandledException(e.Exception, e.Message);

        e.Handled = true;
    }

    private enum RecoveryStartupChoice
    {
        Restore,
        Discard,
        Cancel
    }
}
