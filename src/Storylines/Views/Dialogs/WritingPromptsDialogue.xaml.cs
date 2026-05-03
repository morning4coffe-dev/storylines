using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Storylines.Models;
using Windows.ApplicationModel.DataTransfer;
using Storylines.Services.Interfaces;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Storylines.Views.Dialogs
{
    public sealed partial class WritingPromptsDialogue : StorylinesContentDialog
    {
        private static readonly Random _rng = new Random();
        private readonly IChapterWorkflowService _chapterWorkflow;
        private readonly INavigationService _navigation;
        private readonly ProjectState _projectState;
        private readonly ResourceLoader _resources;
        private string _selectedCategory;

        private static readonly Dictionary<string, List<string>> Prompts = new Dictionary<string, List<string>>
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

        public WritingPromptsDialogue()
        {
            InitializeComponent();

            _chapterWorkflow = App.GetService<IChapterWorkflowService>();
            _navigation = App.GetService<INavigationService>();
            _projectState = App.GetService<ProjectState>();
            _resources = ResourceLoader.GetForViewIndependentUse();

            categoryComboBox.Items.Add(_resources.GetString("writingPromptsAllCategories") ?? "All categories");
            foreach (var cat in Prompts.Keys)
                categoryComboBox.Items.Add(cat);
            categoryComboBox.SelectedIndex = 0;

            ShowRandomPrompt();
        }

        public static async Task OpenAsync()
        {
            try
            {
                await App.GetService<IDialogService>().ShowAsync(new WritingPromptsDialogue());
            }
            catch (Exception ex)
            {
                App.TryGetService<ILogger>()?.Warning($"Failed to open writing prompts dialog: {ex.Message}");
            }
        }

        private void ShowRandomPrompt()
        {
            var pool = string.IsNullOrEmpty(_selectedCategory)
                ? Prompts.Values.SelectMany(p => p).ToList()
                : Prompts.ContainsKey(_selectedCategory) ? Prompts[_selectedCategory] : new List<string>();

            promptActionStatusText.Text = string.Empty;

            if (pool.Count > 0)
                promptText.Text = pool[_rng.Next(pool.Count)];
            else
                promptText.Text = _resources.GetString("noPromptsAvailable");
        }

        private void OnShuffle_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomPrompt();
        }

        private void OnCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categoryComboBox.SelectedIndex <= 0)
                _selectedCategory = null;
            else
                _selectedCategory = categoryComboBox.SelectedItem as string;

            ShowRandomPrompt();
        }

        private void OnCopyPrompt_Click(object sender, RoutedEventArgs e)
        {
            var prompt = promptText.Text?.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
                return;

            var package = new DataPackage();
            package.SetText(prompt);
            Clipboard.SetContent(package);

            promptActionStatusText.Text = _resources.GetString("writingPromptCopiedStatus") ?? "Prompt copied to clipboard.";
        }

        private void OnCreateChapterFromPrompt_Click(object sender, RoutedEventArgs e)
        {
            var prompt = promptText.Text?.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
                return;

            var beforeCount = _projectState.Chapters.Count;
            var chapterNameSeed = !string.IsNullOrWhiteSpace(_selectedCategory)
                ? string.Format(_resources.GetString("writingPromptCategoryChapterSeedFormat") ?? "{0} prompt", _selectedCategory)
                : (_resources.GetString("writingPromptDefaultChapterSeed") ?? "Writing prompt");

            _chapterWorkflow.CreateChapterWithContent(chapterNameSeed, prompt);

            var createdChapter = beforeCount < _projectState.Chapters.Count
                ? _projectState.Chapters[beforeCount]
                : _projectState.Chapters.LastOrDefault();

            if (createdChapter != null && AppView.current?.page != AppView.Pages.MainPage)
                _navigation?.NavigateTo(NavigationTarget.MainPage, createdChapter.Token);

            Hide();
        }
    }
}
