using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.Storage;
using Windows.UI;

namespace Storylines.Scripts.Services
{
    class SettingsValues
    {
        public enum SelectedTheme { Light, Dark, System };
        public static SelectedTheme selectedTheme = SelectedTheme.System;

        public enum SelectedAccent { System, App, Custom };
        private static SelectedAccent _selectedAccent;
        public static SelectedAccent selectedAccent 
        {
            set
            {
                _selectedAccent = value;
                ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AppAccent] = (int)selectedAccent;
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
                ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AppCustomAccent] = customAccentColor.ToHex();
            }
            get
            {
                return _customAccentColor;
            }
        }

        public enum ReviewPrompt { SuccessfullyRated, NeverShowAgain, NotYet };

        public static string chapterName
        {
            //set
            //{
            //    //_selectedAccent = value;
            //    ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ChapterName] = chapterName;
            //}
            get
            {
                var ch = ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ChapterName] == null ? 
                    ResourceLoader.GetForViewIndependentUse().GetString("chapterName") : ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ChapterName].ToString();
                return ch;
            }
        }

        public static bool exitDiagEnabled => Convert.ToBoolean(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ExitDialogueOn] ?? true);

        public static bool autosaveEnabled => Convert.ToBoolean(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AutosaveEnabled] ?? false);

        public static double autosaveInterval => Convert.ToDouble(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AutosaveInterval] ?? 2);

        public static bool whiteTextBackground => Convert.ToBoolean(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.TextBoxSolidBackground] ?? false);
        public static bool newChapterShortcut => Convert.ToBoolean(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.OnPageDownNewChapterEnabled] ?? true);
        public static string language => string.IsNullOrEmpty((string)ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.UserLanguage]) ? "" : (string)ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.UserLanguage];

        public static int dailyWordGoal => Convert.ToInt32(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.DailyWordGoal] ?? 500);

        public static string editorFontFamily => (ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.EditorFontFamily] as string) ?? "Calibri";
        public static double editorFontSize => Convert.ToDouble(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.EditorFontSize] ?? 11.0);
        public static double editorLineSpacing => Convert.ToDouble(ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.EditorLineSpacing] ?? 1.2);

        public static void LoadSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            ThemeSettings.ChangeTheme(Convert.ToInt32(localSettings.Values[SettingsValueStrings.AppTheme] ?? 2), ThemeSettings.themeListener.CurrentTheme.ToElementTheme());
            selectedAccent = (SelectedAccent)(localSettings.Values[SettingsValueStrings.AppAccent] ?? 1);
            customAccentColor = Microsoft.Toolkit.Uwp.Helpers.ColorHelper.ToColor((ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.AppCustomAccent] ?? appAccentColor.ToHex()).ToString());
            ServiceLocator.Events.Publish(new SettingChangedEvent
            {
                SettingKey = SettingsValueStrings.TextBoxSolidBackground,
                Value = Convert.ToBoolean(localSettings.Values[SettingsValueStrings.TextBoxSolidBackground] ?? false)
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
        public static string ChapterTagPresets { get; } = "ChapterTagPresets";
        public static string FirstRunCompleted { get; } = "FirstRunCompleted";

        // Font customisation
        public static string EditorFontFamily { get; } = "EditorFontFamily";
        public static string EditorFontSize { get; } = "EditorFontSize";
        public static string EditorLineSpacing { get; } = "EditorLineSpacing";
    }
}

