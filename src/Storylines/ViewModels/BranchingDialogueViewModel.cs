using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Models;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.Resources;

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
        public ObservableCollection<BranchingDialogueChoiceData> SimulationChoices { get; } = new ObservableCollection<BranchingDialogueChoiceData>();

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
        private string _validationSummary;

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

        public bool HasChapters => Chapters.Count > 0;

        public BranchingDialogueViewModel(ProjectState projectState = null, IBranchingDialogueService service = null)
        {
            _projectState = projectState ?? App.TryGetService<ProjectState>() ?? new ProjectState();
            _service = service ?? App.TryGetService<IBranchingDialogueService>();

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

        partial void OnSelectedChapterChanged(Chapter value)
        {
            LoadSelectedChapterGraph();
        }

        partial void OnSearchTextChanged(string value)
        {
            RefreshFilteredNodes();
        }

        partial void OnSelectedSpeakerFilterChanged(string value)
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

        [RelayCommand]
        private void CreateNode()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null)
                return;

            var defaultTitle = _resources.GetString("branchingDefaultNodeTitle");
            var node = _service.CreateNode(chapterId, string.IsNullOrWhiteSpace(defaultTitle) ? null : defaultTitle);
            RefreshFilteredNodes();
            SelectedNode = node;
            ValidateGraph();
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

        [RelayCommand]
        private void SaveNodeEdits()
        {
            var chapterId = GetSelectedChapterId();
            if (chapterId == null || SelectedNode == null)
                return;

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

            var result = _service.ValidateGraph(chapterId);
            ValidationSummary = string.Format(
                _resources.GetString("branchingValidationSummaryFormat") ?? "Missing targets: {0}, unreachable nodes: {1}, empty choices: {2}",
                result.MissingTargets.Count,
                result.UnreachableNodes.Count,
                result.EmptyChoiceText.Count);
        }

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

        public void LoadSelectedChapterGraph()
        {
            var chapterId = GetSelectedChapterId();
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                _activeGraph = null;
                FilteredNodes.Clear();
                AllNodeTargets.Clear();
                SpeakerFilters.Clear();
                SimulationChoices.Clear();
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

            SpeakerFilters.Add("All");

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

            var query = (SearchText ?? string.Empty).Trim();
            var speakerFilter = SelectedSpeakerFilter ?? "All";

            var filtered = allNodes.Where(node =>
            {
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var text = string.Join(" ", node.Title ?? "", node.Speaker ?? "", node.Text ?? "");
                    if (text.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0)
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(speakerFilter)
                    && !string.Equals(speakerFilter, "All", StringComparison.CurrentCultureIgnoreCase)
                    && !string.Equals(node.Speaker, speakerFilter, StringComparison.CurrentCultureIgnoreCase))
                {
                    return false;
                }

                return true;
            });

            foreach (var node in filtered)
                FilteredNodes.Add(node);

            GraphRefreshed?.Invoke();
        }

        private void UpdateSimulationUi(BranchingDialogueSimulationState? state)
        {
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

            SimulationChoices.Clear();
            foreach (var choice in currentNode.Choices ?? Enumerable.Empty<BranchingDialogueChoiceData>())
                if (choice != null)
                    SimulationChoices.Add(choice);
        }

        private string? GetSelectedChapterId()
        {
            return SelectedChapter?.Token;
        }
    }
}
