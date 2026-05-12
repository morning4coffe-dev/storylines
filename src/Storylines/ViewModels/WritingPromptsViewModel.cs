
namespace Storylines.ViewModels;

/// <summary>
/// ViewModel for the WritingPromptsDialogue.
/// Manages prompt categories, random prompt display, copy and chapter creation.
/// </summary>
public partial class WritingPromptsViewModel : ObservableObject
{
    private static readonly Random _rng = new();
    private readonly IChapterWorkflowService _chapterWorkflow;
    private readonly INavigationService _navigation;
    private readonly ProjectState _projectState;
    private readonly ResourceLoader _resources;

    [ObservableProperty] private string _promptText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _selectedCategory;
    [ObservableProperty] private int _selectedCategoryIndex;

    public List<string> Categories { get; }

    private static readonly Dictionary<string, List<string>> Prompts = new()
    {
        ["Character"] = new List<string>
        {
            "Your protagonist discovers a letter they wrote to themselves ten years ago. What does it say?",
            "A character must explain a lie they told years ago — but the truth is even stranger.",
            "Write a scene where two characters meet for the first time, but one of them is hiding something.",
            "Your character wakes up with a skill they never had before. How does it change their day?",
            "A villain explains why they believe they are the hero of the story.",
            "Write a conversation between your protagonist and their childhood self.",
            "A character receives a gift from someone they thought had forgotten about them.",
            "Your character has to make an impossible choice — and both options have consequences."
        },
        ["Setting"] = new List<string>
        {
            "Describe a place that feels safe at first but slowly becomes unsettling.",
            "Your character arrives at a town where everyone seems to know their name.",
            "Write a scene set during a storm that mirrors the characters' emotional state.",
            "Describe a room that tells a story without any characters present.",
            "A familiar location has changed dramatically since your character last visited.",
            "Set a pivotal scene in the most mundane location possible."
        },
        ["Conflict"] = new List<string>
        {
            "Two allies realise they have fundamentally different goals.",
            "A secret is revealed at the worst possible moment.",
            "Your character must work with someone they deeply distrust.",
            "A plan goes perfectly — and that's exactly the problem.",
            "Write a scene where the real conflict is what remains unsaid.",
            "A character's greatest strength becomes their biggest obstacle.",
            "Someone offers help, but accepting it comes with strings attached."
        },
        ["Emotion"] = new List<string>
        {
            "Write a scene that captures the feeling of returning home after a long time away.",
            "A character tries to comfort someone but only makes things worse.",
            "Capture the moment just before a character makes a decision that will change everything.",
            "Write about a small, ordinary moment that a character will remember forever.",
            "A character laughs at something they really shouldn't find funny.",
            "Write a farewell scene where neither character says goodbye directly."
        },
        ["Dialogue"] = new List<string>
        {
            "Write a conversation where both characters want something from each other.",
            "Two characters argue, but we slowly realise they're actually arguing about something else entirely.",
            "Write a scene composed entirely of dialogue — no action, no description.",
            "A character says 'I'm fine' — write the scene so the reader knows they are absolutely not fine.",
            "Two characters communicate without speaking a single word.",
            "Write a conversation that starts lighthearted and gradually becomes serious."
        }
    };

    public WritingPromptsViewModel(
        IChapterWorkflowService chapterWorkflow,
        INavigationService navigation,
        ProjectState projectState)
    {
        _chapterWorkflow = chapterWorkflow;
        _navigation = navigation;
        _projectState = projectState;
        _resources = ResourceLoader.GetForViewIndependentUse();

        Categories = new List<string> { _resources.GetString("writingPromptsAllCategories") ?? "All categories" };
        Categories.AddRange(Prompts.Keys);

        SelectedCategoryIndex = 0;
        ShowRandomPrompt();
    }

    partial void OnSelectedCategoryIndexChanged(int value)
    {
        SelectedCategory = value <= 0 ? null : Categories[value];
        ShowRandomPrompt();
    }

    [RelayCommand]
    public void ShowRandomPrompt()
    {
        StatusText = string.Empty;

        var pool = string.IsNullOrEmpty(SelectedCategory)
            ? Prompts.Values.SelectMany(p => p).ToList()
            : Prompts.ContainsKey(SelectedCategory) ? Prompts[SelectedCategory] : new List<string>();

        PromptText = pool.Count > 0
            ? pool[_rng.Next(pool.Count)]
            : _resources.GetString("noPromptsAvailable");
    }

    [RelayCommand]
    private void CopyPrompt()
    {
        var prompt = PromptText?.Trim();
        if (string.IsNullOrWhiteSpace(prompt)) return;

        var package = new DataPackage();
        package.SetText(prompt);
        Clipboard.SetContent(package);

        StatusText = _resources.GetString("writingPromptCopiedStatus") ?? "Prompt copied to clipboard.";
    }

    [RelayCommand]
    private void CreateChapterFromPrompt()
    {
        var prompt = PromptText?.Trim();
        if (string.IsNullOrWhiteSpace(prompt)) return;

        var beforeCount = _projectState.Chapters.Count;
        var chapterNameSeed = !string.IsNullOrWhiteSpace(SelectedCategory)
            ? string.Format(_resources.GetString("writingPromptCategoryChapterSeedFormat") ?? "{0} prompt", SelectedCategory)
            : (_resources.GetString("writingPromptDefaultChapterSeed") ?? "Writing prompt");

        _chapterWorkflow.CreateChapterWithContent(chapterNameSeed, prompt);

        var createdChapter = beforeCount < _projectState.Chapters.Count
            ? _projectState.Chapters[beforeCount]
            : _projectState.Chapters.LastOrDefault();

        if (createdChapter is not null)
            _navigation?.NavigateTo(NavigationTarget.MainPage, createdChapter.Token);

        CloseRequested?.Invoke();
    }

    public event Action CloseRequested;
}
