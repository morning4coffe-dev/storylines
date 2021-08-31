using Storylines.Components;
using Storylines.Scripts.Functions;
using System;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Storylines.Scripts.Services
{
    class Autosave
    {
        private static DispatcherTimer autosaveTimer;

        private static void Do()
        {
            if (SettingsValues.autosaveEnabled && TimeTravelSystem.unSavedProgress)
                SaveSystem.Save();
                //prehraj animaci
        }

        public static void Enable()
        {
            if (!SettingsValues.autosaveEnabled || autosaveTimer == null)
            {
                Do();

                autosaveTimer = new DispatcherTimer();
                autosaveTimer.Tick += OnAutosaveTimer_Tick;
                var interval = SettingsValues.autosaveInterval;
                if (interval >= 1)
                    autosaveTimer.Interval = new TimeSpan(0, (int)SettingsValues.autosaveInterval, 0);
                else
                    autosaveTimer.Interval = new TimeSpan(0, 0, (int)(SettingsValues.autosaveInterval * 10));
                autosaveTimer.Start();

                ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AutosaveEnabled] = true;
            }
        }

        public static void Disable()
        {
            Do();
            autosaveTimer.Stop();

            ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AutosaveEnabled] = false;
        }

        private static void OnAutosaveTimer_Tick(object sender, object e)
        {
            if (SettingsValues.autosaveEnabled)
                Do();
        }
    }
}
