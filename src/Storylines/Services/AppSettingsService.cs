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
        private readonly ApplicationDataContainer _localSettings;
        private readonly ResourceLoader _resources;

        public AppSettingsService(EventAggregator events, IProjectPersistenceService persistence)
        {
            _events = events;
            _persistence = persistence;
            _localSettings = ApplicationData.Current.LocalSettings;
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
            set => _localSettings.Values[SettingsValueStrings.ChapterName] = value ?? DefaultChapterName;
        }

        public string DefaultChapterName => _resources.GetString("chapterName");

        public bool ExitDialogueEnabled
        {
            get => SettingsValues.exitDiagEnabled;
            set => _localSettings.Values[SettingsValueStrings.ExitDialogueOn] = value;
        }

        public bool LoadLastProjectOnStart
        {
            get => _localSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] != null;
            set => _localSettings.Values[SettingsValueStrings.LoadLastProjectOnStart] = value
                ? _persistence.CurrentProject?.Token
                : null;
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

                if (_persistence.CurrentProject?.file == null)
                    return;

                _persistence.EnableAutosave();
            }
        }

        public double AutosaveInterval
        {
            get => SettingsValues.autosaveInterval;
            set
            {
                _localSettings.Values[SettingsValueStrings.AutosaveInterval] = value;

                if (AutosaveEnabled)
                    _persistence.RefreshAutosave();
            }
        }

        public int DailyWordGoal
        {
            get => SettingsValues.dailyWordGoal;
            set => _localSettings.Values[SettingsValueStrings.DailyWordGoal] = Math.Max(0, value);
        }

        public int WritingStreakDays
        {
            get => Convert.ToInt32(_localSettings.Values[SettingsValueStrings.WritingStreakDays] ?? 0);
            set => _localSettings.Values[SettingsValueStrings.WritingStreakDays] = Math.Max(0, value);
        }

        public bool ExperimentalFeaturesEnabled
        {
            get => SettingsValues.experimentalFeaturesEnabled;
            set
            {
                _localSettings.Values[SettingsValueStrings.ExperimentalFeaturesEnabled] = value;
                Publish(SettingsValueStrings.ExperimentalFeaturesEnabled, value);
            }
        }

        public bool AddChapterOnPageDownEnabled
        {
            get => SettingsValues.newChapterShortcut;
            set => _localSettings.Values[SettingsValueStrings.OnPageDownNewChapterEnabled] = value;
        }

        public string EditorFontFamily
        {
            get => SettingsValues.editorFontFamily;
            set
            {
                string fontFamily = string.IsNullOrWhiteSpace(value) ? "Segoe UI" : value;
                _localSettings.Values[SettingsValueStrings.EditorFontFamily] = fontFamily;
                Publish(SettingsValueStrings.EditorFontFamily, fontFamily);
            }
        }

        public double EditorFontSize
        {
            get => SettingsValues.editorFontSize;
            set
            {
                double size = Clamp(value, 8, 24);
                _localSettings.Values[SettingsValueStrings.EditorFontSize] = size;
                Publish(SettingsValueStrings.EditorFontSize, size);
            }
        }

        public double EditorZoom
        {
            get => Convert.ToDouble(_localSettings.Values[SettingsValueStrings.ZoomValue] ?? 25d);
            set
            {
                double zoom = Clamp(value, 13, 100);
                _localSettings.Values[SettingsValueStrings.ZoomValue] = zoom;
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
                _localSettings.Values[SettingsValueStrings.UserLanguage] = languageTag;
            }
        }

        public double ReadAloudVolume
        {
            get => Convert.ToDouble(_localSettings.Values[SettingsValueStrings.ReadAloudVolume] ?? 75d);
            set => _localSettings.Values[SettingsValueStrings.ReadAloudVolume] = Clamp(value, 0, 100);
        }

        public string ReadAloudVoiceId
        {
            get => _localSettings.Values[SettingsValueStrings.ReadAloudVoice]?.ToString();
            set => _localSettings.Values[SettingsValueStrings.ReadAloudVoice] = value;
        }

        public bool TextBoxSolidBackground
        {
            get => SettingsValues.whiteTextBackground;
            set
            {
                _localSettings.Values[SettingsValueStrings.TextBoxSolidBackground] = value;
                Publish(SettingsValueStrings.TextBoxSolidBackground, value);
            }
        }

        public bool DialogueModeEnabled
        {
            get => SettingsValues.dialogueModeEnabled;
            set => _localSettings.Values[SettingsValueStrings.DialogueModeEnabled] = value;
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