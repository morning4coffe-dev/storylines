using CommunityToolkit.WinUI.Helpers;
using Storylines.Services.Interfaces;
using System;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.UI;

namespace Storylines.Services
{
    public static class SettingsValues
    {
        private static IPreferencesService Preferences => App.GetService<IPreferencesService>();

        public enum SelectedTheme { Light, Dark, System };
        public static SelectedTheme selectedTheme = SelectedTheme.System;

        public enum SelectedAccent { System, App, Custom };
        private static SelectedAccent _selectedAccent;
        public static SelectedAccent selectedAccent 
        {
            set
            {
                _selectedAccent = value;
                Preferences.Set(SettingsValueStrings.AppAccent, (int)selectedAccent);
            }
            get
            {
                return _selectedAccent;
            }
        }

        public static Color appAccentColor { get; } = Color.FromArgb(255, 190, 90, 0);
        private static Color _customAccentColor;
        public static Color customAccentColor
        {
            set
            {
                _customAccentColor = value;
                Preferences.Set(SettingsValueStrings.AppCustomAccent, customAccentColor.ToHex());
            }
            get
            {
                return _customAccentColor;
            }
        }

        public enum ReviewPrompt { SuccessfullyRated, NeverShowAgain, NotYet };

        public static string chapterName
        {
            get
            {
                var ch = Preferences.Contains(SettingsValueStrings.ChapterName) == false
                    ? ResourceLoader.GetForViewIndependentUse().GetString("chapterName")
                    : Preferences.Get<string>(SettingsValueStrings.ChapterName);
                return ch;
            }
        }

        public static bool exitDiagEnabled => Preferences.Get(SettingsValueStrings.ExitDialogueOn, true);

        public static bool autosaveEnabled => Preferences.Get(SettingsValueStrings.AutosaveEnabled, false);

        public static double autosaveInterval => Preferences.Get(SettingsValueStrings.AutosaveInterval, 2.0);

        public static bool whiteTextBackground => Preferences.Get(SettingsValueStrings.TextBoxSolidBackground, false);
        public static bool newChapterShortcut => Preferences.Get(SettingsValueStrings.OnPageDownNewChapterEnabled, true);
        public static string language => Preferences.Get<string>(SettingsValueStrings.UserLanguage) ?? "";

        public static int dailyWordGoal => Preferences.Get(SettingsValueStrings.DailyWordGoal, 500);

        public static string editorFontFamily => Preferences.Get(SettingsValueStrings.EditorFontFamily, "Segoe UI");
        public static double editorFontSize => Preferences.Get(SettingsValueStrings.EditorFontSize, 14.0);
        public static double editorLineSpacing => Preferences.Get(SettingsValueStrings.EditorLineSpacing, 1.2);

        public static bool experimentalFeaturesEnabled => Preferences.Get(SettingsValueStrings.ExperimentalFeaturesEnabled, false);

        public static bool dialogueModeEnabled => Preferences.Get(SettingsValueStrings.DialogueModeEnabled, false);
        public static bool dialogueTeachingTipShown => Preferences.Get(SettingsValueStrings.DialogueTeachingTipShown, false);

        public static void LoadSettings()
        {
            ThemeSettings.ChangeTheme(Preferences.Get(SettingsValueStrings.AppTheme, 2), ThemeSettings.themeListener.CurrentTheme.ToElementTheme());
            selectedAccent = (SelectedAccent)Preferences.Get(SettingsValueStrings.AppAccent, 1);
            customAccentColor = CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(Preferences.Get(SettingsValueStrings.AppCustomAccent, appAccentColor.ToHex()));
            App.GetService<EventAggregator>().Publish(new SettingChangedEvent
            {
                SettingKey = SettingsValueStrings.TextBoxSolidBackground,
                Value = Preferences.Get(SettingsValueStrings.TextBoxSolidBackground, false)
            });
        }

        public static bool IsUserLanguageSupported()
        {
            var supportedLanguages = ApplicationLanguages.ManifestLanguages;
            string currentLang = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
            for (int i = 0; i < supportedLanguages.Count; i++)
                if (LanguageTagsMatch(currentLang, supportedLanguages[i]))
                    return true;

            return false;
        }

        public static bool LanguageTagsMatch(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                return true;

            string leftPrimary = GetPrimaryLanguageTag(left);
            string rightPrimary = GetPrimaryLanguageTag(right);

            return string.Equals(leftPrimary, rightPrimary, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetPrimaryLanguageTag(string languageTag)
            => languageTag?.Split('-').FirstOrDefault() ?? string.Empty;

        public static bool IsCurrentVersionGreater(string currentVersion, string supportedVersion)
        {
            Version version1 = new Version(currentVersion);
            Version version2 = new Version(supportedVersion);

            var result = version1.CompareTo(version2);
            if (result > 0)
                return true;
            else if (result < 0)
                return false;
            else
                return true;
        }

        public static bool IsStringSaveable(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Contains("/") || text.Contains(@"\") || text.Contains("\"") || text.Contains(":") || text.Contains("*") || text.Contains("?") || text.Contains("<") || text.Contains(">") || text.Contains("|") || text.Length > 255)
                return false;
            else
                return true;
         }
     }

    public static class SettingsValueStrings
    {
        public static string UserLanguage { get; } = "UserLang";

        public static string AppTheme { get; } = "AppTheme";

        public static string AppAccent { get; } = "AppAccentColor";
        public static string AppCustomAccent { get; } = "AppCustomAccentColor";

        public static string ReviewPrompt { get; } = "ReviewPrompt";

        public static string ChapterName { get; } = "ChapterName";
        public static string ExitDialogueOn { get; } = "ExitDialogue";
        public static string LoadLastProjectOnStart { get; } = "LoadLastProjectOnStart";
        public static string AutosaveEnabled { get; } = "AutosaveEnabled";
        public static string AutosaveInterval { get; } = "AutosaveIntervalMinutes";

        public static string ReadAloudVolume { get; } = "ReadAloudVolume";
        public static string ReadAloudVoice { get; } = "ReadAloudVoiceId";
        public static string TextBoxSolidBackground { get; } = "SolidBackground";
        public static string OnPageDownNewChapterEnabled { get; } = "OnPageDownNewChapterEnabled";

        public static string AppLanguage { get; } = "AppLanguage";

        public static string ZoomValue { get; } = "TextBoxZoomValue";

        // Writing session & goals
        public static string DailyWordGoal { get; } = "DailyWordGoal";
        public static string WritingStreakDays { get; } = "WritingStreakDays";
        public static string ChapterTagPresets { get; } = "ChapterTagPresets";
        public static string FirstRunCompleted { get; } = "FirstRunCompleted";

        // Font customisation
        public static string EditorFontFamily { get; } = "EditorFontFamily";
        public static string EditorFontSize { get; } = "EditorFontSize";
        public static string EditorLineSpacing { get; } = "EditorLineSpacing";

        // Experimental features
        public static string ExperimentalFeaturesEnabled { get; } = "ExperimentalFeaturesEnabled";

        // Dialogue mode
        public static string DialogueModeEnabled { get; } = "DialogueModeEnabled";
        public static string DialogueTeachingTipShown { get; } = "DialogueTeachingTipShown";
    }
}
