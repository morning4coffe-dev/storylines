using Windows.UI;

namespace Storylines.Services.Interfaces;

public interface IAppSettingsService
{
    SettingsValues.SelectedTheme SelectedTheme { get; set; }
    SettingsValues.SelectedAccent SelectedAccent { get; set; }
    Color CustomAccentColor { get; set; }

    string ChapterName { get; set; }
    string DefaultChapterName { get; }
    bool ExitDialogueEnabled { get; set; }
    bool LoadLastProjectOnStart { get; set; }
    bool AutosaveEnabled { get; set; }
    double AutosaveInterval { get; set; }
    int DailyWordGoal { get; set; }
    int WritingStreakDays { get; set; }
    bool ExperimentalFeaturesEnabled { get; set; }

    bool AddChapterOnPageDownEnabled { get; set; }
    string EditorFontFamily { get; set; }
    double EditorFontSize { get; set; }
    double EditorZoom { get; set; }

    string UserLanguage { get; set; }
    double ReadAloudVolume { get; set; }
    string ReadAloudVoiceId { get; set; }
    double ReadAloudRate { get; set; }
    double ReadAloudPitch { get; set; }
    string DictationLanguage { get; set; }
    bool TextBoxSolidBackground { get; set; }
    bool DialogueModeEnabled { get; set; }

    void ResetChapterName();
    bool IsUserLanguageSupported();
    bool LanguageTagsMatch(string left, string right);
}