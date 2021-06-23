using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using System;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Storylines
{
    /// <summary>
    /// Poskytuje chování specifické pro aplikaci, které doplňuje výchozí třídu Application.
    /// </summary>
    sealed partial class App : Application, INotifyPropertyChanged
    {
        public static Windows.Storage.IStorageItem item;
        private ApplicationViewTitleBar titleBar;

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Inicializuje objekt aplikace typu singleton. Jedná se o první řádek spuštěného vytvořeného kódu,
        /// který je proto logickým ekvivalentem metod main() nebo WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        /// <summary>
        /// Vyvolá se při normálním spuštění aplikace koncovým uživatelem. Ostatní vstupní body
        /// se použijí například při spuštění aplikace za účelem otevření konkrétního souboru.
        /// </summary>
        /// <param name="e">Podrobnosti o žádosti o spuštění a procesu</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            // Neopakovat inicializaci aplikace, pokud už má okno obsah,
            // jenom ověřit, jestli je toto okno aktivní
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Vytvořit objekt Frame, který bude fungovat jako kontext navigace, a spustit procházení první stránky
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Načíst stav z dříve pozastavené aplikace
                }

                // Umístit rámec do aktuálního objektu Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }

                Start();
            }
        }

        public void Start()
        {
            Window.Current.Activate();

            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            ApplicationView.GetForCurrentView().IsScreenCaptureEnabled = true;

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

            UISettings uiSettings = new UISettings();
            titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;

            MainPage.LoadSettings();

            uiSettings.ColorValuesChanged += ColorValuesChanged;
            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += OnLayoutMetricsChanged;

            Window.Current.SetTitleBar(MainPage.mainPage.titleBar);

            LoadFileDialogue.Open();
        }

        private void ColorValuesChanged(UISettings e, object sender)
        {
            if (SettingsPage.selectedTheme == SettingsPage.CurrentSelectedTheme.System)
            {
                SettingsPage.UpdateSystemTheme();
            }

            if (!SettingsPage.appColorEnabled)
            {
                try
                {
                    SettingsPage.UpdateSystemColor(SettingsPage.appColorEnabled);
                }
                catch
                {
                    //RestartAppForAccentChangeAsync();
                }
            }
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

        /// <summary>
        /// Vyvolá se, když selže přechod na určitou stránku.
        /// </summary>
        /// <param name="sender">Objekt Frame, u kterého selhala navigace</param>
        /// <param name="e">Podrobnosti o chybě navigace</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Vyvolá se při pozastavení běhu aplikace. Stav aplikace se uloží, i když
        /// bez informace, jestli se aplikace ukončí nebo obnoví s obsahem
        /// obsahem paměti.
        /// </summary>
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

            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage));
            }

            Start();
        }
    }
}
