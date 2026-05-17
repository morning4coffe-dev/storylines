using System.Text;

namespace Storylines.ViewModels;

/// <summary>
/// ViewModel for the ProjectStatsDialogue.
/// Computes word counts, chapter stats, and word frequency distribution.
/// </summary>
public partial class ProjectStatsViewModel : ObservableObject
{
    private readonly ProjectState _projectState;
    private readonly ResourceLoader _resources;

    [ObservableProperty] private string _storyStatsText = string.Empty;
    [ObservableProperty] private string _charactersStatsText = string.Empty;
    [ObservableProperty] private string _chaptersStatsText = string.Empty;
    [ObservableProperty] private string _currentChapterStatsText = string.Empty;
    [ObservableProperty] private string _wordDistributionText = string.Empty;

    /// <summary>Per-chapter stats for bar chart rendering.</summary>
    public List<ChapterWordStat> ChapterWordStats { get; private set; } = new();

    public ProjectStatsViewModel(
        ProjectState projectState)
    {
        _projectState = projectState;
        _resources = ResourceLoader.GetForViewIndependentUse();
    }

    public void ComputeStats(string currentChapterPlainText)
    {
        var txt = (currentChapterPlainText ?? string.Empty).ToLower();

        int charactersCount = _projectState.Characters.Count;
        string txtWithoutSpace = txt.Replace(" ", "");
        int wordCount = txt.Split([' ', (char)13], StringSplitOptions.RemoveEmptyEntries).Length;
        int paragraphCount = ParagraphCountRegex().Matches(txt).Count;

        string storyText = GetTextFromAllChapters();
        int storyCharCount = storyText.Length > 1 ? storyText.Length - 2 : storyText.Length;
        int storyWords = storyText.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;
        int readMinutes = Math.Max(1, (int)Math.Ceiling(storyWords / 200.0));
        int chapterCount = _projectState.Chapters.Count;
        int draftCount = _projectState.Chapters.Count(c => c.Status == ChapterStatus.Draft);
        int writingCount = _projectState.Chapters.Count(c => c.Status == ChapterStatus.Writing);
        int revisionCount = _projectState.Chapters.Count(c => c.Status == ChapterStatus.Revision);
        int doneCount = _projectState.Chapters.Count(c => c.Status == ChapterStatus.Final);

        StoryStatsText = $"{_resources.GetString("charactersStory")}: {storyCharCount}\n{_resources.GetString("words")}: {storyWords}\n{_resources.GetString("estimatedReadTime")}: {readMinutes} {_resources.GetString("min")}\n{_resources.GetString("estimatedPageCount")}: {storyCharCount / 3838}";
        CharactersStatsText = $"{_resources.GetString("characters")}: {charactersCount}";
        ChaptersStatsText = $"{_resources.GetString("chapters")}: {chapterCount}\n{_resources.GetString("done")}: {doneCount}\n{_resources.GetString("projectStatsWritingLabel")}: {writingCount}\n{_resources.GetString("projectStatsRevisionLabel")}: {revisionCount}\n{_resources.GetString("projectStatsDraftLabel")}: {draftCount}";
        CurrentChapterStatsText = $"{_resources.GetString("charactersStory")} ({_resources.GetString("withoutSpaces")}): {txt.Length - 1}\n{_resources.GetString("charactersStory")} ({_resources.GetString("withSpaces")}): {txtWithoutSpace.Length - 1}\n{_resources.GetString("paragraphs")}: {paragraphCount}\n{_resources.GetString("words")}: {wordCount}";

        ComputeWordFrequency(txt);
        ComputeChapterBars();
    }

    private void ComputeWordFrequency(string txt)
    {
        var sb = new StringBuilder();
        var wordFrequency = Regex.Matches(txt, @"\b[\w]*\b")
            .Where(m => m.Length > 0)
            .GroupBy(m => m.Value)
            .OrderByDescending(m => m.Count())
            .ThenBy(m => m.Key);

        foreach (var item in wordFrequency)
        {
            if (item is not null)
                sb.AppendLine($"{item.Key}: {item.Count()}");
        }

        WordDistributionText = sb.Length > 0 ? sb.ToString() : string.Empty;
    }

    private void ComputeChapterBars()
    {
        var stats = new List<ChapterWordStat>();
        int maxWords = 1;

        foreach (var chapter in _projectState.Chapters)
        {
            string plain = RtfHelper.ConvertToPlainText(chapter.Text);
            int words = plain.Split(new char[] { ' ', (char)13 }, StringSplitOptions.RemoveEmptyEntries).Length;
            stats.Add(new ChapterWordStat { Name = chapter.Name, WordCount = words });
            if (words > maxWords) maxWords = words;
        }

        foreach (var stat in stats)
            stat.NormalizedWidth = Math.Max(2, (double)stat.WordCount / maxWords);

        ChapterWordStats = stats;
        OnPropertyChanged(nameof(ChapterWordStats));
    }

    private string GetTextFromAllChapters()
    {
        string storyCharacterCount = "";
        foreach (var chapter in _projectState.Chapters)
        {
            storyCharacterCount += RtfHelper.ConvertToPlainText(chapter.Text);
        }
        return storyCharacterCount;
    }

    [GeneratedRegex(@"[^\r\n]*[^ \r\n]+[^\r\n]*((\r|\n|\r\n)[^\r\n]*[^ \r\n]+[^\r\n]*)*")]
    private static partial Regex ParagraphCountRegex();
}

public class ChapterWordStat
{
    public string Name { get; set; }
    public int WordCount { get; set; }
    /// <summary>Fraction of maximum (0..1) for bar width calculation.</summary>
    public double NormalizedWidth { get; set; }
}
