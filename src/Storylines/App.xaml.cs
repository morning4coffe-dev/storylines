using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.Helpers;
using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Storylines
{
    public sealed partial class App : Application, INotifyPropertyChanged
    {
        public static Windows.Storage.IStorageItem item;
        private ApplicationViewTitleBar titleBar;

        public event PropertyChangedEventHandler PropertyChanged;
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        /// <param name="e">Podrobnosti o žádosti o spuštění a procesu</param>
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
                    //TODO: Načíst stav z dříve pozastavené aplikace
                }

                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    _ = rootFrame.Navigate(typeof(AppView), e.Arguments);
                }

                Start();

                SystemInformation.Instance.TrackAppUse(e);

                _ = LoadLastProject();
            }
        }

        private void LanguageCheck()
        {
            if (!SettingsValues.IsUserLanguageSupported())
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en";
            else
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
        }

        private void Start()
        {
            Window.Current.Activate();

            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            ApplicationView.GetForCurrentView().IsScreenCaptureEnabled = true;

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

            UISettings uiSettings = new UISettings();
            titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;

            if (MicrosoftStoreAndAppCenterFunctions.AppCenterKey.Length > 0)
            {
                AppCenter.Start(MicrosoftStoreAndAppCenterFunctions.AppCenterKey, typeof(Analytics), typeof(Crashes));
                AppCenter.SetEnabledAsync(MicrosoftStoreAndAppCenterFunctions.AppCenterEnabled);
            }

            SettingsValues.LoadSettings();

            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += OnLayoutMetricsChanged;

            Window.Current.SetTitleBar(AppView.current.appTitleBar);

            LoadProjectDialogue.Open();

            ThemeSettings.Initialize();

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequest;

            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData_OnStart();

            _ = MicrosoftStoreAndAppCenterFunctions.CheckForNewUpdateAvailableAsync();

            MicrosoftStoreAndAppCenterFunctions.InitializeReview();

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
                        SaveSystem.Load(ProjectFile.LoadExisting(file, fileToken));
                }
                catch { }
            }
        }

        private void OnCloseRequest(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            MicrosoftStoreAndAppCenterFunctions.SendAnalyticData_OnLeave();

            if (TimeTravelSystem.unSavedProgress && SettingsValues.exitDiagEnabled)
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
            MicrosoftStoreAndAppCenterFunctions.SendCrashData_OnUnhandledException(e);

            e.Handled = true;
        }

        /// <param name="sender">Objekt Frame, u kterého selhala navigace</param>
        /// <param name="e">Podrobnosti o chybě navigace</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <param name="sender">Zdroj žádosti o pozastavení</param>
        /// <param name="e">Podrobnosti žádosti o pozastavení</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Uložit stav aplikace a zastavit jakoukoliv aktivitu na pozadí
            deferral.Complete();
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

            Start();
        }
    }
}
