using Storylines.Services.Interfaces;
using System;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.Storage;
using Windows.UI;

namespace Storylines.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly EventAggregator _events;
        private readonly IProjectPersistenceService _persistence;
        private readonly IPreferencesService _preferences;
        private readonly ResourceLoader _resources;

        public AppSettingsService(EventAggregator events, IProjectPersistenceService persistence, IPreferencesService preferences)
        {
            _events = events;
            _persistence = persistence;
            _preferences = preferences;
            _resources = ResourceLoader.GetForViewIndependentUse();
        }

        public SettingsValues.SelectedTheme SelectedTheme
        {
            get => SettingsValues.selectedTheme;
            set => ThemeSettings.ChangeTheme((int)value, ThemeSettings.themeListener.CurrentTheme.ToElementTheme());
        }

        public SettingsValues.SelectedAccent SelectedAccent
        {
            get => SettingsValues.selectedAccent;
            set
            {
                SettingsValues.selectedAccent = value;
                ThemeSettings.InitializeAppAccentColor();
            }
        }

        public Color CustomAccentColor
        {
            get => SettingsValues.customAccentColor;
            set
            {
                SettingsValues.customAccentColor = value;
                ThemeSettings.InitializeAppAccentColor();
            }
        }

        public string ChapterName
        {
            get => SettingsValues.chapterName;
            set => _preferences.Set(SettingsValueStrings.ChapterName, value ?? DefaultChapterName);
        }

        public string DefaultChapterName => _resources.GetString("chapterName");

        public bool ExitDialogueEnabled
        {
            get => SettingsValues.exitDiagEnabled;
            set => _preferences.Set(SettingsValueStrings.ExitDialogueOn, value);
        }

        public bool LoadLastProjectOnStart
        {
            get => _preferences.Contains(SettingsValueStrings.LoadLastProjectOnStart);
            set
            {
                if (value)
                    _preferences.Set(SettingsValueStrings.LoadLastProjectOnStart, _persistence.CurrentProject?.Token);
                else
                    _preferences.Remove(SettingsValueStrings.LoadLastProjectOnStart);
            }
        }

        public bool AutosaveEnabled
        {
            get => SettingsValues.autosaveEnabled;
            set
            {
                if (!value)
                {
                    _persistence.DisableAutosave();
                    return;
                }

                if (_persistence.CurrentProject?.file is null)
                    return;

                _persistence.EnableAutosave();
            }
        }

        public double AutosaveInterval
        {
            get => SettingsValues.autosaveInterval;
            set
            {
                _preferences.Set(SettingsValueStrings.AutosaveInterval, value);

                if (AutosaveEnabled)
                    _persistence.RefreshAutosave();
            }
        }

        public int DailyWordGoal
        {
            get => SettingsValues.dailyWordGoal;
            set => _preferences.Set(SettingsValueStrings.DailyWordGoal, Math.Max(0, value));
        }

        public int WritingStreakDays
        {
            get => _preferences.Get(SettingsValueStrings.WritingStreakDays, 0);
            set => _preferences.Set(SettingsValueStrings.WritingStreakDays, Math.Max(0, value));
        }

        public bool ExperimentalFeaturesEnabled
        {
            get => SettingsValues.experimentalFeaturesEnabled;
            set
            {
                _preferences.Set(SettingsValueStrings.ExperimentalFeaturesEnabled, value);
                Publish(SettingsValueStrings.ExperimentalFeaturesEnabled, value);
            }
        }

        public bool AddChapterOnPageDownEnabled
        {
            get => SettingsValues.newChapterShortcut;
            set => _preferences.Set(SettingsValueStrings.OnPageDownNewChapterEnabled, value);
        }

        public string EditorFontFamily
        {
            get => SettingsValues.editorFontFamily;
            set
            {
                string fontFamily = string.IsNullOrWhiteSpace(value) ? "Segoe UI" : value;
                _preferences.Set(SettingsValueStrings.EditorFontFamily, fontFamily);
                Publish(SettingsValueStrings.EditorFontFamily, fontFamily);
            }
        }

        public double EditorFontSize
        {
            get => SettingsValues.editorFontSize;
            set
            {
                double size = Clamp(value, 8, 24);
                _preferences.Set(SettingsValueStrings.EditorFontSize, size);
                Publish(SettingsValueStrings.EditorFontSize, size);
            }
        }

        public double EditorZoom
        {
            get => _preferences.Get(SettingsValueStrings.ZoomValue, 25d);
            set
            {
                double zoom = Clamp(value, 13, 100);
                _preferences.Set(SettingsValueStrings.ZoomValue, zoom);
                Publish(SettingsValueStrings.ZoomValue, zoom);
            }
        }

        public string UserLanguage
        {
            get => SettingsValues.language;
            set
            {
                string languageTag = value ?? string.Empty;
                ApplicationLanguages.PrimaryLanguageOverride = languageTag;
                _preferences.Set(SettingsValueStrings.UserLanguage, languageTag);
            }
        }

        public double ReadAloudVolume
        {
            get => _preferences.Get(SettingsValueStrings.ReadAloudVolume, 75d);
            set => _preferences.Set(SettingsValueStrings.ReadAloudVolume, Clamp(value, 0, 100));
        }

        public string ReadAloudVoiceId
        {
            get => _preferences.Get<string>(SettingsValueStrings.ReadAloudVoice);
            set => _preferences.Set(SettingsValueStrings.ReadAloudVoice, value);
        }

        public double ReadAloudRate
        {
            get => _preferences.Get(SettingsValueStrings.ReadAloudRate, 1.0d);
            set => _preferences.Set(SettingsValueStrings.ReadAloudRate, Clamp(value, 0.5, 2.0));
        }

        public double ReadAloudPitch
        {
            get => _preferences.Get(SettingsValueStrings.ReadAloudPitch, 1.0d);
            set => _preferences.Set(SettingsValueStrings.ReadAloudPitch, Clamp(value, 0.0, 2.0));
        }

        public string DictationLanguage
        {
            get => _preferences.Get<string>(SettingsValueStrings.DictationLanguage) ?? string.Empty;
            set => _preferences.Set(SettingsValueStrings.DictationLanguage, value ?? string.Empty);
        }

        public bool TextBoxSolidBackground
        {
            get => SettingsValues.whiteTextBackground;
            set
            {
                _preferences.Set(SettingsValueStrings.TextBoxSolidBackground, value);
                Publish(SettingsValueStrings.TextBoxSolidBackground, value);
            }
        }

        public bool DialogueModeEnabled
        {
            get => SettingsValues.dialogueModeEnabled;
            set => _preferences.Set(SettingsValueStrings.DialogueModeEnabled, value);
        }

        public void ResetChapterName()
        {
            ChapterName = DefaultChapterName;
        }

        public bool IsUserLanguageSupported() => SettingsValues.IsUserLanguageSupported();

        public bool LanguageTagsMatch(string left, string right) => SettingsValues.LanguageTagsMatch(left, right);

        private void Publish(string settingKey, object value)
        {
            _events.Publish(new SettingChangedEvent
            {
                SettingKey = settingKey,
                Value = value
            });
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }
}