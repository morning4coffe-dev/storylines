using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
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
    public sealed partial class App : Application, INotifyPropertyChanged
    {
        public static IStorageItem item;
        private ApplicationViewTitleBar titleBar;

        public static new App Current => Application.Current as App;

        public IServiceProvider Services { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;

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
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            UnhandledException += App_UnhandledException;

            LanguageCheck();

            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    _ = rootFrame.Navigate(typeof(AppView), e.Arguments);
                }

                SystemInformation.Instance.TrackAppUse(e);

                Start("launch");

                _ = LoadLastProject();
            }
        }

        private void LanguageCheck()
        {
            if (!string.IsNullOrEmpty(SettingsValues.language))
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = SettingsValues.language;
            else
            {
                string preferredLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
                if (!SettingsValues.IsUserLanguageSupported())
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en";
                else
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = preferredLanguage;
            }
        }

        private void Start(string activationKind)
        {
            Window.Current.Activate();

            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            ApplicationView.GetForCurrentView().IsScreenCaptureEnabled = true;

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

            UISettings uiSettings = new UISettings();
            titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;

            SettingsValues.LoadSettings();

            var telemetry = App.GetService<ITelemetryService>();
            _ = telemetry.InitializeAsync();

            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += OnLayoutMetricsChanged;

            Window.Current.SetTitleBar(AppView.current.appTitleBar);

            LoadProjectDialogue.Open();

            ThemeSettings.Initialize();

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequest;

            telemetry.TrackAppStarted(activationKind);

            _ = MicrosoftStoreFunctions.CheckForNewUpdateAvailableAsync();

            MicrosoftStoreFunctions.InitializeReview();

            AppView.current.UsingWindows10();
        }

        private async Task LoadLastProject()
        {
            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] != null)
            {
                try
                {
                    var fileToken = Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.LoadLastProjectOnStart].ToString();
                    var file = await ProjectFile.GetProjectFromTokenAsync(fileToken);
                    if(file != null)
                        GetService<IProjectPersistenceService>().Load(await ProjectFile.LoadExistingAsync(file, fileToken));
                }
                catch (Exception ex)
                {
                    GetService<Storylines.Services.Interfaces.ILogger>()?.Warning($"Failed to load last project: {ex.Message}");
                }
            }
        }

        private void OnCloseRequest(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            var blockedByUnsavedChanges = TimeTravelSystem.unSavedProgress && SettingsValues.exitDiagEnabled;
            App.GetService<ITelemetryService>().TrackAppClosingRequested(blockedByUnsavedChanges);

            if (blockedByUnsavedChanges)
            {
                e.Handled = true;
                _ = NotificationManager.DisplayUnsavedProgressDialogue(true);
            }
            
            NotificationManager.ClearBadgeNotification();
        }

        private void OnLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object e)
        {
            UpdateLayoutMetrics();
        }

        private void UpdateLayoutMetrics()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("CoreTitleBarHeight"));
                PropertyChanged(this, new PropertyChangedEventArgs("CoreTitleBarPadding"));
            }
        }

        private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            App.GetService<ITelemetryService>().TrackUnhandledException(e.Exception, e.Message);

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

            try
            {
                if (TimeTravelSystem.unSavedProgress)
                    await RecoveryService.CacheCurrentStateAsync();
            }
            finally
            {
                deferral.Complete();
            }
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            item = args.Files.First();

            LanguageCheck();

            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(AppView));

                if (!rootFrame.Navigate(typeof(AppView)))
                {
                    throw new Exception("Failed to create initial page (OnFileActivated)");
                }
            }

            SystemInformation.Instance.TrackAppUse(args);

            Start("file_activation");
        }
    }
}
