using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Models.Dialogue;
using Storylines.Services.Interfaces;
using Storylines.Services;

namespace Storylines.ViewModels.Dialogue
{
    public partial class BranchingDialogueEditorViewModel : ObservableObject
    {
        private readonly IDialogueExportService _exportService;
        private readonly IGraphConsistencyService _consistencyService;

        public DialogueGraph Graph { get; private set; }

        public ObservableCollection<DialogueNodeViewModel> Nodes { get; } = new ObservableCollection<DialogueNodeViewModel>();
        public ObservableCollection<TagItem> TagSource { get; } = new ObservableCollection<TagItem>();

        [ObservableProperty]
        private DialogueNodeViewModel selectedNode;

        public BranchingDialogueEditorViewModel()
        {
            _exportService = new DialogueExportService();
            _consistencyService = new GraphConsistencyService();
            InitializeDefaultTags();
            LoadGraph(new DialogueGraph());
        }

        private void InitializeDefaultTags()
        {
            TagSource.Add(new TagItem { Name = "Player", Category = "Character" });
            TagSource.Add(new TagItem { Name = "NPC_Merchant", Category = "Character" });
            TagSource.Add(new TagItem { Name = "GoldCoin", Category = "Item" });
            TagSource.Add(new TagItem { Name = "Castle", Category = "Location" });
        }

        public void LoadGraph(DialogueGraph graph)
        {
            Graph = graph;
            Nodes.Clear();

            foreach (var node in Graph.Nodes)
            {
                var nodeVm = new DialogueNodeViewModel(node);
                Nodes.Add(nodeVm);
            }

            foreach (var choice in Graph.Choices)
            {
                var sourceNodeVm = Nodes.FirstOrDefault(n => n.Id == choice.SourceNodeId);
                if (sourceNodeVm != null)
                {
                    sourceNodeVm.Choices.Add(new DialogueChoiceViewModel(choice));
                }
            }

            if (Nodes.Any())
            {
                SelectedNode = Nodes.First();
            }
        }

        [RelayCommand]
        public void AddNode()
        {
            var newNode = new DialogueNode { Speaker = "New Speaker" };
            Graph.Nodes.Add(newNode);
            var nodeVm = new DialogueNodeViewModel(newNode);
            Nodes.Add(nodeVm);
            SelectedNode = nodeVm;
        }

        [RelayCommand]
        public void RemoveNode(DialogueNodeViewModel nodeVm)
        {
            if (nodeVm == null) return;

            _consistencyService.RemoveNode(Graph, nodeVm.Id);
            Nodes.Remove(nodeVm);

            // Clean up choice ViewModels targeting this node
            foreach (var node in Nodes)
            {
                var choicesToRemove = node.Choices.Where(c => c.Choice.TargetNodeId == nodeVm.Id).ToList();
                foreach (var c in choicesToRemove)
                {
                    node.Choices.Remove(c);
                }
            }

            if (SelectedNode == nodeVm)
            {
                SelectedNode = Nodes.FirstOrDefault();
            }
        }

        [RelayCommand]
        public void AddChoice()
        {
            if (SelectedNode == null) return;

            var newChoice = new DialogueChoice
            {
                SourceNodeId = SelectedNode.Id,
                ChoiceText = "New Choice"
            };
            Graph.Choices.Add(newChoice);
            SelectedNode.Choices.Add(new DialogueChoiceViewModel(newChoice));
        }

        [RelayCommand]
        public void RemoveChoice(DialogueChoiceViewModel choiceVm)
        {
            if (SelectedNode == null || choiceVm == null) return;

            Graph.Choices.Remove(choiceVm.Choice);
            SelectedNode.Choices.Remove(choiceVm);
        }

        public string GetExportJson()
        {
            return _exportService.ExportToJson(Graph);
        }

        public string GetExportPlainText()
        {
            return _exportService.ExportToPlainText(Graph);
        }
    }
}
