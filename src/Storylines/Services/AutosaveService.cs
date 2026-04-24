using Storylines.Views.Controls;
using Storylines.Helpers;
using System;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Storylines.Services
{
    class AutosaveService
    {
        private static DispatcherTimer autosaveTimer;

        private static void Do()
        {
            if (SettingsValues.autosaveEnabled && TimeTravelSystem.unSavedProgress && SaveSystem.currentProject?.file != null)
                SaveSystem.Save();
                //TODO: Play save animation
        }

        public static void Enable()
        {
            // Dispose any existing timer first
            if (autosaveTimer != null)
            {
                autosaveTimer.Tick -= OnAutosaveTimer_Tick;
                autosaveTimer.Stop();
                autosaveTimer = null;
            }

            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AutosaveEnabled] = true;

            Do();
            autosaveTimer = new DispatcherTimer();
            autosaveTimer.Tick += OnAutosaveTimer_Tick;
            var interval = SettingsValues.autosaveInterval;
            if (interval >= 1)
                autosaveTimer.Interval = new TimeSpan(0, (int)interval, 0);
            else
                autosaveTimer.Interval = new TimeSpan(0, 0, (int)(interval * 60));
            autosaveTimer.Start();
        }

        public static void Disable()
        {
            if (autosaveTimer != null)
            {
                autosaveTimer.Tick -= OnAutosaveTimer_Tick;
                autosaveTimer.Stop();
                autosaveTimer = null;
            }

            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AutosaveEnabled] = false;
        }

        private static void OnAutosaveTimer_Tick(object sender, object e)
        {
            if (SettingsValues.autosaveEnabled)
                Do();
        }
    }
}
