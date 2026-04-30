using Storylines.Services;
using Storylines.Services.Interfaces;
using Windows.UI;

namespace Storylines.Tests.Stubs;

/// <summary>
/// In-memory stub of <see cref="IAppSettingsService"/> for tests. Avoids touching the real
/// <c>ApplicationData.Current.LocalSettings</c> store, which is unavailable in xUnit.
/// </summary>
internal sealed class AppSettingsServiceStub : IAppSettingsService
{
    public SettingsValues.SelectedTheme SelectedTheme { get; set; } = SettingsValues.SelectedTheme.System;
    public SettingsValues.SelectedAccent SelectedAccent { get; set; } = SettingsValues.SelectedAccent.App;
    public Color CustomAccentColor { get; set; }
    public string ChapterName { get; set; } = "Chapter";
    public string DefaultChapterName => "Chapter";
    public bool ExitDialogueEnabled { get; set; } = true;
    public bool LoadLastProjectOnStart { get; set; }
    public bool AutosaveEnabled { get; set; }
    public double AutosaveInterval { get; set; } = 2;
    public int DailyWordGoal { get; set; }
    public int WritingStreakDays { get; set; }
    public bool ExperimentalFeaturesEnabled { get; set; }
    public bool AddChapterOnPageDownEnabled { get; set; } = true;
    public string EditorFontFamily { get; set; } = "Segoe UI";
    public double EditorFontSize { get; set; } = 14;
    public double EditorZoom { get; set; } = 25;
    public string UserLanguage { get; set; } = "";
    public double ReadAloudVolume { get; set; } = 75;
    public string ReadAloudVoiceId { get; set; }
    public bool TextBoxSolidBackground { get; set; }
    public bool DialogueModeEnabled { get; set; }

    public void ResetChapterName() => ChapterName = DefaultChapterName;

    public bool IsUserLanguageSupported() => true;

    public bool LanguageTagsMatch(string left, string right)
        => string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
}
