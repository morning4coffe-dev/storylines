using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Constants;
using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;

namespace Storylines.ViewModels
{
    public partial class BranchingDialogueViewModel : ObservableObject
    {
        private readonly ProjectState _projectState;
        private readonly IBranchingDialogueService _service;
        private readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();

        private BranchingDialogueGraphData _activeGraph;

        public ObservableCollection<Chapter> Chapters => _projectState.Chapters;
        public ObservableCollection<BranchingDialogueNodeData> FilteredNodes { get; } = new ObservableCollection<BranchingDialogueNodeData>();
        public ObservableCollection<BranchingDialogueNodeData> AllNodeTargets { get; } = new ObservableCollection<BranchingDialogueNodeData>();
        public ObservableCollection<string> SpeakerFilters { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> TagFilters { get; } = new ObservableCollection<string>();
        public ObservableCollection<BranchingDialogueChoiceData> SimulationChoices { get; } = new ObservableCollection<BranchingDialogueChoiceData>();
        public ObservableCollection<string> CharacterSuggestions { get; } = new ObservableCollection<string>();
        public ObservableCollection<KeyValuePair<string, string>> SimulationVariables { get; } = new ObservableCollection<KeyValuePair<string, string>>();

        public event Action GraphRefreshed;

        [ObservableProperty]
        private Chapter _selectedChapter;

        [ObservableProperty]
        private BranchingDialogueNodeData _selectedNode;

        [ObservableProperty]
        private BranchingDialogueChoiceData _selectedChoice;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private string _selectedSpeakerFilter = "All";

        [ObservableProperty]
        private string _selectedTagFilter = "All";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidationMessages))]
        private string _validationSummary;

        public Visibility HasValidationMessages =>
            string.IsNullOrEmpty(ValidationSummary) ? Visibility.Collapsed : Visibility.Visible;

        [ObservableProperty]
        private bool _isMapModeEnabled;

        [ObservableProperty]
        private string _simulatorBreadcrumb;

        [ObservableProperty]
        private string _simulatorCurrentSpeaker;

        [ObservableProperty]
        private string _simulatorCurrentText;

        [ObservableProperty]
        private string _simulatorStatus;

        [ObservableProperty]
        private bool _isNodeListVisible = true;

        [ObservableProperty]
        private string _chapterStatusText;

        [ObservableProperty]
        private string _chapterWordCountText;

        public bool HasChapters => Chapters.Count > 0;

        private string _pendingChapterToken;

        public BranchingDialogueViewModel(ProjectState projectState = null, IBranchingDialogueService service = null)
        {
            _projectState = projectState ?? App.TryGetService<ProjectState>() ?? new ProjectState();
            _service = service ?? App.TryGetService<IBranchingDialogueService>();

            RefreshCharacterSuggestions();

            if (HasChapters)
            {
                SelectedChapter = Chapters[0];
                LoadSelectedChapterGraph();
            }
            else
            {
                ValidationSummary = _resources.GetString("branchingNoChaptersMessage");
            }
        }

        public void NavigatedTo(string chapterToken)
        {
            _pendingChapterToken = chapterToken;
            if (!string.IsNullOrWhiteSpace(chapterToken))
            {
                var chapter = Chapters.FirstOrDefault(c => c.Token == chapterToken);
                if (chapter != null)
                {
                    SelectedChapter = chapter;
                    _pendingChapterToken = null;
                }
            }
        }

        partial void OnSelectedChapterChanged(Chapter value)
        {
            LoadSelectedChapterGraph();
            UpdateChapterInfo();
        }

        partial void OnSearchTextChanged(string value)
        {
            RefreshFilteredNodes();
        }

        partial void OnSelectedSpeakerFilterChanged(string value)
        {
            RefreshFilteredNodes();
        }

        partial void OnSelectedTagFilterChanged(string value)
        {
            RefreshFilteredNodes();
        }

        partial void OnSelectedNodeChanged(BranchingDialogueNodeData value)
        {
            SelectedChoice = value?.Choices?.FirstOrDefault();
            OnPropertyChanged(nameof(CanEditNode));
            OnPropertyChanged(nameof(CanEditChoices));
        }

        public bool CanEditNode => SelectedNode != null;
        public bool CanEditChoices => SelectedNode != null;

        public void RefreshCharacterSuggestions()
        {
            CharacterSuggestions.Clear();
            if (_projectState?.Characters == null) return;

            foreach (var character in _projectState.Characters)
            {
                if (!string.IsNullOrWhiteSpace(character.Name))
                    CharacterSuggestions.Add(character.Name);
            }
        }

        private void UpdateChapterInfo()
        {
            if (SelectedChapter == null)
            {
                ChapterStatusText = null;
                ChapterWordCountText = null;
                return;
            }

            ChapterStatusText = SelectedChapter.Status.ToString();

            int wordCount = 0;
            if (!string.IsNullOrWhiteSpace(SelectedChapter.Text))
            {
                wordCount = SelectedChapter.Text.Split(
                    new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            }

            var format = _resources.GetString("branchingChapterWordCountFormat") ?? "{0} words";
            ChapterWordCountText = string.Format(format, wordCount);
        }

        /// <summary>
        /// Returns a stable color for a speaker name, used for card color-coding.
        /// </summary>
        public static Windows.UI.Color GetSpeakerColor(string speaker)
        {
            if (string.IsNullOrWhiteSpace(speaker))
                return Windows.UI.Color.FromArgb(255, 128, 128, 128);

            var hash = Math.Abs(speaker.GetHashCode());
            var hue = hash % 360;
            return HslToColor(hue, 0.55, 0.55);
        }

        private static Windows.UI.Color HslToColor(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;
            double r, g, b;

            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Windows.UI.Color.FromArgb(255,
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255));
        }

        #region Node CRUD

        [RelayCommand]
        private void CreateNode()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null)
                return;

            var defaultTitle = _resources.GetString("branchingPassageDefaultTitle");
            var node = _service.CreateNode(chapterId, string.IsNullOrWhiteSpace(defaultTitle) ? null : defaultTitle);
            RefreshFilteredNodes();
            SelectedNode = node;
            ValidateGraph();
        }

        public BranchingDialogueNodeData CreateNodeAtPosition(double x, double y)
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null)
                return null;

            var defaultTitle = _resources.GetString("branchingPassageDefaultTitle");
            var node = _service.CreateNode(chapterId, string.IsNullOrWhiteSpace(defaultTitle) ? null : defaultTitle);
            if (node != null)
            {
                node.PositionX = x;
                node.PositionY = y;
                _service.NotifyGraphChanged(chapterId);
            }
            RefreshFilteredNodes();
            SelectedNode = node;
            ValidateGraph();
            return node;
        }

        [RelayCommand]
        private void DeleteSelectedNode()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null)
                return;

            _service.DeleteNode(chapterId, SelectedNode.Id);
            RefreshFilteredNodes();
            SelectedNode = FilteredNodes.FirstOrDefault();
            ValidateGraph();
        }

        #endregion

        #region Choice CRUD

        [RelayCommand]
        private void AddChoiceWithAutoDestination()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null)
                return;

            var destinationTitle = string.Format(
                _resources.GetString("branchingAutoDestinationNodeTitleFormat") ?? "Outcome {0}",
                (AllNodeTargets.Count + 1));
            var destinationNode = _service.CreateNode(chapterId, destinationTitle);

            var defaultChoiceText = string.Format(
                _resources.GetString("branchingDefaultChoiceTextFormat") ?? "Choice {0}",
                (SelectedNode.Choices?.Count ?? 0) + 1);
            var choice = _service.AddChoice(chapterId, SelectedNode.Id, defaultChoiceText, destinationNode?.Id);

            RefreshFilteredNodes();
            SelectedChoice = choice;
            ValidateGraph();
        }

        [RelayCommand]
        private void RemoveChoice(BranchingDialogueChoiceData choice)
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null || choice == null)
                return;

            _service.RemoveChoice(chapterId, SelectedNode.Id, choice.Id);
            RefreshFilteredNodes();
            SelectedChoice = SelectedNode?.Choices?.FirstOrDefault();
            ValidateGraph();
        }

        #endregion

        #region Condition & Action CRUD

        [RelayCommand]
        private void AddCondition(BranchingDialogueChoiceData choice)
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null || choice == null)
                return;

            _service.AddCondition(chapterId, SelectedNode.Id, choice.Id);
            OnPropertyChanged(nameof(SelectedNode));
        }

        [RelayCommand]
        private void RemoveCondition(BranchingDialogueChoiceData choice)
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null || choice?.Conditions == null || choice.Conditions.Count == 0)
                return;

            _service.RemoveCondition(chapterId, SelectedNode.Id, choice.Id, choice.Conditions.Count - 1);
            OnPropertyChanged(nameof(SelectedNode));
        }

        [RelayCommand]
        private void AddAction()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null)
                return;

            _service.AddAction(chapterId, SelectedNode.Id);
            OnPropertyChanged(nameof(SelectedNode));
        }

        [RelayCommand]
        private void RemoveAction(int actionIndex)
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null)
                return;

            _service.RemoveAction(chapterId, SelectedNode.Id, actionIndex);
            OnPropertyChanged(nameof(SelectedNode));
        }

        #endregion

        #region Save / Start / Validate

        [RelayCommand]
        private void SaveNodeEdits()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null)
                return;

            // Auto-resolve character token from speaker name
            if (!string.IsNullOrWhiteSpace(SelectedNode.Speaker))
            {
                SelectedNode.CharacterToken = SpeakerResolver.ResolveToken(
                    SelectedNode.Speaker, _projectState.Characters);
            }

            _service.NotifyGraphChanged(chapterId);
            RefreshFilteredNodes();
            ValidateGraph();
        }

        [RelayCommand]
        private void SetStartNodeFromSelection()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null)
                return;

            _service.SetStartNode(chapterId, SelectedNode.Id);
            ValidateGraph();
        }

        [RelayCommand]
        private void ValidateGraph()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null)
            {
                ValidationSummary = _resources.GetString("branchingNoChaptersMessage");
                return;
            }

            var knownSpeakers = SpeakerResolver.GetKnownSpeakers(_projectState.Characters);
            var result = _service.ValidateGraph(chapterId, knownSpeakers);

            var format = _resources.GetString("branchingValidationSummaryExtendedFormat")
                ?? "Missing targets: {0}, unreachable: {1}, empty choices: {2}, unknown speakers: {3}, orphaned conditions: {4}";
            ValidationSummary = string.Format(format,
                result.MissingTargets.Count,
                result.UnreachableNodes.Count,
                result.EmptyChoiceText.Count,
                result.UnknownSpeakers.Count,
                result.OrphanedConditions.Count);
        }

        #endregion

        #region Import / Export

        [RelayCommand]
        private void ImportFromChapterText()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedChapter == null)
                return;

            var chapterText = SelectedChapter.Text;
            if (string.IsNullOrWhiteSpace(chapterText))
            {
                ValidationSummary = _resources.GetString("branchingNoDialoguesFound");
                return;
            }

            var characterNames = _projectState.Characters
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => c.Name)
                .ToList();

            var dialogues = Dialogue.GetFromCharactersFromString(chapterText, characterNames);
            if (dialogues == null || dialogues.Count == 0)
            {
                ValidationSummary = _resources.GetString("branchingNoDialoguesFound");
                return;
            }

            BranchingDialogueNodeData prevNode = null;
            int col = 0;
            foreach (var dialogue in dialogues)
            {
                var node = _service.CreateNode(chapterId, dialogue.Name, dialogue.Name, dialogue.Text);
                if (node != null)
                {
                    node.CharacterToken = SpeakerResolver.ResolveToken(dialogue.Name, _projectState.Characters);
                    node.PositionX = LayoutConstants.NodeAutoPlaceOffsetX + (col % LayoutConstants.NodeAutoPlaceColumnsPerRow) * LayoutConstants.NodeAutoPlaceColumnWidth;
                    node.PositionY = LayoutConstants.NodeAutoPlaceOffsetY + (col / LayoutConstants.NodeAutoPlaceColumnsPerRow) * LayoutConstants.NodeAutoPlaceRowHeight;
                    col++;

                    if (prevNode != null)
                    {
                        _service.AddChoice(chapterId, prevNode.Id, "→", node.Id);
                    }
                    prevNode = node;
                }
            }

            _service.NotifyGraphChanged(chapterId);
            RefreshFilteredNodes();
            ValidateGraph();

            var msg = _resources.GetString("branchingImportedNodesFormat");
            ValidationSummary = string.Format(msg ?? "Imported {0} passages from chapter text.", dialogues.Count);
        }

        [RelayCommand]
        private void SyncPassagesToChapterText()
        {
            if (_activeGraph?.Nodes == null || _activeGraph.Nodes.Count == 0 || SelectedChapter == null)
                return;

            var text = BuildLinearDialogueText();
            if (string.IsNullOrWhiteSpace(text))
            {
                ValidationSummary = _resources.GetString("branchingNoDialoguesFound");
                return;
            }

            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);

            var msg = _resources.GetString("branchingSyncedToChapterFormat");
            ValidationSummary = string.Format(msg ?? "Copied {0} passage(s) to clipboard.", _activeGraph.Nodes.Count);
        }

        private string BuildLinearDialogueText()
        {
            if (_activeGraph?.Nodes == null || _activeGraph.Nodes.Count == 0)
                return string.Empty;

            var nodeById = _activeGraph.Nodes.ToDictionary(n => n.Id, n => n);
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            var lines = new List<string>();

            var startId = _activeGraph.StartNodeId ?? _activeGraph.Nodes[0].Id;
            queue.Enqueue(startId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                if (!visited.Add(currentId) || !nodeById.TryGetValue(currentId, out var node))
                    continue;

                var speaker = !string.IsNullOrWhiteSpace(node.Speaker) ? node.Speaker : null;
                var text = node.Text?.Trim() ?? string.Empty;

                if (speaker != null && !string.IsNullOrWhiteSpace(text))
                    lines.Add($"{speaker}: {text}");
                else if (!string.IsNullOrWhiteSpace(text))
                    lines.Add(text);

                if (node.Choices != null)
                {
                    // Add choice labels if branching
                    if (node.Choices.Count > 1)
                    {
                        foreach (var choice in node.Choices)
                        {
                            if (!string.IsNullOrWhiteSpace(choice.Text) && choice.Text != "→")
                                lines.Add($"  > {choice.Text}");
                        }
                    }

                    foreach (var choice in node.Choices)
                    {
                        if (!string.IsNullOrWhiteSpace(choice.TargetNodeId))
                            queue.Enqueue(choice.TargetNodeId);
                    }
                }
            }

            return string.Join("\n", lines);
        }

        [RelayCommand]
        private async void ExportGraphJson()
        {
            if (_activeGraph == null)
                return;

            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
            picker.SuggestedFileName = $"dialogue-{SelectedChapter?.Name ?? "graph"}";

            var file = await picker.PickSaveFileAsync();
            if (file == null)
                return;

            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_activeGraph, Newtonsoft.Json.Formatting.Indented);
                await FileIO.WriteTextAsync(file, json);
                ValidationSummary = _resources.GetString("branchingExportedJson") ?? "Exported as JSON.";
            }
            catch (Exception ex)
            {
                ValidationSummary = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private async void ExportGraphTwee()
        {
            if (_activeGraph == null)
                return;

            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Twee", new List<string> { ".twee", ".tw" });
            picker.SuggestedFileName = $"dialogue-{SelectedChapter?.Name ?? "graph"}";

            var file = await picker.PickSaveFileAsync();
            if (file == null)
                return;

            try
            {
                var twee = BranchingDialogueExportHelper.ConvertGraphToTwee(_activeGraph);
                await FileIO.WriteTextAsync(file, twee);
                ValidationSummary = _resources.GetString("branchingExportedTwee") ?? "Exported as Twee.";
            }
            catch (Exception ex)
            {
                ValidationSummary = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private async void ExportGraphScreenplay()
        {
            if (_activeGraph == null)
                return;

            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Text", new List<string> { ".txt" });
            picker.SuggestedFileName = $"screenplay-{SelectedChapter?.Name ?? "graph"}";

            var file = await picker.PickSaveFileAsync();
            if (file == null)
                return;

            try
            {
                var screenplay = BranchingDialogueExportHelper.ConvertGraphToScreenplay(_activeGraph);
                await FileIO.WriteTextAsync(file, screenplay);
                ValidationSummary = _resources.GetString("branchingExportedScreenplay") ?? "Exported as screenplay.";
            }
            catch (Exception ex)
            {
                ValidationSummary = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private async void ImportGraphJson()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null)
                return;

            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".json");

            var file = await picker.PickSingleFileAsync();
            if (file == null)
                return;

            try
            {
                var json = await FileIO.ReadTextAsync(file);
                var imported = ExportService.ImportBranchingDialogueJson(json);
                if (imported == null || imported.Nodes == null || imported.Nodes.Count == 0)
                {
                    ValidationSummary = _resources.GetString("branchingImportFailed") ?? "Import failed: invalid JSON or no passages found.";
                    return;
                }

                // Remap IDs to avoid collision with existing nodes
                var idMap = new Dictionary<string, string>();
                foreach (var node in imported.Nodes)
                {
                    var oldId = node.Id;
                    node.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                    idMap[oldId] = node.Id;
                }

                // Update choice target references
                foreach (var node in imported.Nodes)
                {
                    if (node.Choices == null) continue;
                    foreach (var choice in node.Choices)
                    {
                        if (!string.IsNullOrWhiteSpace(choice.TargetNodeId) && idMap.TryGetValue(choice.TargetNodeId, out var newId))
                            choice.TargetNodeId = newId;

                        // Remap choice IDs too
                        choice.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                    }
                }

                // Update start node reference
                if (!string.IsNullOrWhiteSpace(imported.StartNodeId) && idMap.TryGetValue(imported.StartNodeId, out var newStartId))
                    imported.StartNodeId = newStartId;

                // Merge imported nodes into current graph
                foreach (var node in imported.Nodes)
                    _activeGraph.Nodes.Add(node);

                _service.NotifyGraphChanged(chapterId);
                RefreshFilteredNodes();
                ValidateGraph();

                var msg = _resources.GetString("branchingImportedNodesFormat");
                ValidationSummary = string.Format(msg ?? "Imported {0} passages.", imported.Nodes.Count);
            }
            catch (Exception ex)
            {
                ValidationSummary = string.Format(
                    _resources.GetString("branchingImportFailed") ?? "Import failed: {0}",
                    ex.Message);
            }
        }

        #endregion

        #region Simulation

        [RelayCommand]
        private void StartSimulation()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null)
                return;

            UpdateSimulationUi(_service.StartSimulation(chapterId));
        }

        [RelayCommand]
        private void RestartSimulation()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null)
                return;

            UpdateSimulationUi(_service.RestartSimulation(chapterId));
        }

        [RelayCommand]
        private void StopSimulation()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null)
                return;

            _service.StopSimulation(chapterId);
            SimulationChoices.Clear();
            SimulationVariables.Clear();
            SimulatorStatus = _resources.GetString("branchingSimulationStopped");
        }

        [RelayCommand]
        private void ChooseSimulationChoice(BranchingDialogueChoiceData choice)
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || choice == null)
                return;

            UpdateSimulationUi(_service.ChooseChoice(chapterId, choice.Id));
        }

        #endregion

        #region Navigation

        [RelayCommand]
        private void ToggleNodeList()
        {
            IsNodeListVisible = !IsNodeListVisible;
        }

        [RelayCommand]
        private void NavigateToChapter()
        {
            if (SelectedChapter == null)
                return;

            AppView.current.ChangePage(AppView.Pages.MainPage);
        }

        [RelayCommand]
        private void NavigateToCharacter()
        {
            if (SelectedNode == null)
                return;

            var character = SpeakerResolver.Resolve(SelectedNode, _projectState.Characters);
            if (character != null)
            {
                var nav = App.TryGetService<INavigationService>();
                nav?.NavigateTo(NavigationTarget.Characters, character.Token);
            }
        }

        #endregion

        public void LoadSelectedChapterGraph()
        {
            var chapterId = GetSelectedChapterId();
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                _activeGraph = null;
                FilteredNodes.Clear();
                AllNodeTargets.Clear();
                SpeakerFilters.Clear();
                TagFilters.Clear();
                SimulationChoices.Clear();
                SimulationVariables.Clear();
                SelectedNode = null;
                ValidationSummary = _resources.GetString("branchingNoChaptersMessage");
                return;
            }

            _activeGraph = _service?.GetOrCreateGraph(chapterId);
            if (_activeGraph == null)
            {
                ValidationSummary = "Failed to initialize graph.";
                return;
            }

            RefreshCharacterSuggestions();
            RefreshFilteredNodes();

            SelectedNode = _activeGraph.Nodes?.FirstOrDefault(node => node?.Id == _activeGraph.StartNodeId)
                ?? _activeGraph.Nodes?.FirstOrDefault();

            ValidateGraph();
            StartSimulation();
        }

        public void RefreshFilteredNodes()
        {
            FilteredNodes.Clear();
            AllNodeTargets.Clear();
            SpeakerFilters.Clear();
            TagFilters.Clear();

            SpeakerFilters.Add("All");
            TagFilters.Add("All");

            if (_activeGraph?.Nodes == null || _activeGraph.Nodes.Count == 0)
            {
                GraphRefreshed?.Invoke();
                return;
            }

            var allNodes = _activeGraph.Nodes.Where(n => n != null).ToList();

            foreach (var node in allNodes)
                AllNodeTargets.Add(node);

            foreach (var speaker in allNodes
                .Where(n => !string.IsNullOrWhiteSpace(n.Speaker))
                .Select(n => n.Speaker)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(speaker))
                    SpeakerFilters.Add(speaker);
            }

            // Collect all distinct tags
            foreach (var tag in allNodes
                .Where(n => n.Tags != null)
                .SelectMany(n => n.Tags)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase))
            {
                TagFilters.Add(tag);
            }

            var query = (SearchText ?? string.Empty).Trim();
            var speakerFilter = SelectedSpeakerFilter ?? "All";
            var tagFilter = SelectedTagFilter ?? "All";

            var filtered = allNodes.Where(node =>
            {
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var text = string.Join(" ", node.Title ?? "", node.Speaker ?? "", node.Text ?? "", node.Notes ?? "");
                    if (text.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0)
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(speakerFilter)
                    && !string.Equals(speakerFilter, "All", StringComparison.CurrentCultureIgnoreCase)
                    && !string.Equals(node.Speaker, speakerFilter, StringComparison.CurrentCultureIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(tagFilter)
                    && !string.Equals(tagFilter, "All", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (node.Tags == null || !node.Tags.Any(t =>
                        string.Equals(t, tagFilter, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        return false;
                    }
                }

                return true;
            });

            foreach (var node in filtered)
                FilteredNodes.Add(node);

            GraphRefreshed?.Invoke();
        }

        private void UpdateSimulationUi(BranchingDialogueSimulationState? state)
        {
            SimulationVariables.Clear();

            if (state == null || _activeGraph?.Nodes == null)
            {
                SimulatorCurrentSpeaker = null;
                SimulatorCurrentText = null;
                SimulatorBreadcrumb = null;
                SimulatorStatus = _resources.GetString("branchingSimulationStopped") ?? "Stopped";
                return;
            }

            var nodeById = _activeGraph.Nodes
                .Where(n => n != null)
                .ToDictionary(n => n.Id ?? string.Empty, n => n);

            if (string.IsNullOrWhiteSpace(state.CurrentNodeId) || !nodeById.TryGetValue(state.CurrentNodeId, out var currentNode))
            {
                SimulatorCurrentSpeaker = null;
                SimulatorCurrentText = null;
                SimulatorStatus = _resources.GetString("branchingSimulationStopped") ?? "Stopped";
                return;
            }

            SimulatorCurrentSpeaker = currentNode.Speaker;
            SimulatorCurrentText = currentNode.Text;

            var breadcrumbTitles = (state.BreadcrumbNodeIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id) && nodeById.ContainsKey(id))
                .Select(id => nodeById[id]?.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t));

            SimulatorBreadcrumb = string.Join(" → ", breadcrumbTitles);
            SimulatorStatus = state.IsDeadEnd
                ? _resources.GetString("branchingSimulationDeadEnd") ?? "Dead-end"
                : _resources.GetString("branchingSimulationActive") ?? "Active";

            // Show only choices whose conditions are met
            SimulationChoices.Clear();
            var availableChoices = BranchingDialogueService.GetAvailableChoices(currentNode, state);
            foreach (var choice in availableChoices)
                if (choice != null)
                    SimulationChoices.Add(choice);

            // Show current variables
            if (state.Variables != null)
            {
                foreach (var kvp in state.Variables.OrderBy(v => v.Key))
                    SimulationVariables.Add(kvp);
            }
        }

        private string? GetSelectedChapterId()
        {
            return SelectedChapter?.Token;
        }
    }
}
