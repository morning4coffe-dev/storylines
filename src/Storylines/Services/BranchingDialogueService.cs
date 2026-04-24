using Storylines.Models;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Storylines.Services
{
    public class BranchingDialogueService : IBranchingDialogueService
    {
        private readonly IBranchingDialogueStore _store;
        private readonly IBranchingDialogueEventPublisher _events;
        private readonly Dictionary<string, BranchingDialogueSimulationState> _simulationByChapter = new Dictionary<string, BranchingDialogueSimulationState>();

        public BranchingDialogueService(IBranchingDialogueStore store, IBranchingDialogueEventPublisher events)
        {
            _store = store;
            _events = events;
        }

        public BranchingDialogueGraphData GetOrCreateGraph(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
                throw new ArgumentException("Chapter id is required.", nameof(chapterId));

            var graph = _store.GetOrCreateGraph(chapterId);
            graph.EnsureValid();
            return graph;
        }

        public BranchingDialogueNodeData? CreateNode(string chapterId, string? title = null, string? speaker = null, string? text = null)
        {
            var graph = GetOrCreateGraph(chapterId);
            if (graph?.Nodes == null)
                return null;

            var node = new BranchingDialogueNodeData
            {
                Id = Guid.NewGuid().ToString(),
                Title = string.IsNullOrWhiteSpace(title) ? $"Node {graph.Nodes.Count + 1}" : title,
                Speaker = speaker,
                Text = text ?? string.Empty,
                Choices = new List<BranchingDialogueChoiceData>()
            };

            graph.Nodes.Add(node);
            if (string.IsNullOrWhiteSpace(graph.StartNodeId))
                graph.StartNodeId = node.Id;

            graph.EnsureValid();
            PublishGraphChanged(chapterId, graph);
            return node;
        }

        public bool RenameNode(string chapterId, string nodeId, string? newTitle)
        {
            var node = FindNode(chapterId, nodeId, out var graph);
            if (node == null || graph == null)
                return false;

            node.Title = newTitle ?? string.Empty;
            PublishGraphChanged(chapterId, graph);
            return true;
        }

        public bool DeleteNode(string chapterId, string nodeId)
        {
            var graph = GetOrCreateGraph(chapterId);
            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null)
                return false;

            graph.Nodes.Remove(node);
            foreach (var n in graph.Nodes)
            {
                n.Choices?.RemoveAll(c => c.TargetNodeId == nodeId);
            }

            graph.EnsureValid();
            PublishGraphChanged(chapterId, graph);
            return true;
        }

        public BranchingDialogueChoiceData? AddChoice(string chapterId, string nodeId, string? text = null, string? targetNodeId = null)
        {
            var node = FindNode(chapterId, nodeId, out var graph);
            if (node == null || graph == null)
                return null;

            var choice = new BranchingDialogueChoiceData
            {
                Id = Guid.NewGuid().ToString(),
                Text = text ?? string.Empty,
                TargetNodeId = targetNodeId,
                Conditions = new List<BranchingDialogueConditionData>()
            };

            node.Choices ??= new List<BranchingDialogueChoiceData>();
            node.Choices.Add(choice);

            graph.EnsureValid();
            PublishGraphChanged(chapterId, graph);
            return choice;
        }

        public bool RemoveChoice(string chapterId, string nodeId, string choiceId)
        {
            var node = FindNode(chapterId, nodeId, out var graph);
            if (node?.Choices == null)
                return false;

            var removed = node.Choices.RemoveAll(c => c.Id == choiceId) > 0;
            if (!removed)
                return false;

            PublishGraphChanged(chapterId, graph);
            return true;
        }

        public bool ReorderChoices(string chapterId, string nodeId, int fromIndex, int toIndex)
        {
            var node = FindNode(chapterId, nodeId, out var graph);
            if (node?.Choices == null)
                return false;
            if (fromIndex < 0 || fromIndex >= node.Choices.Count || toIndex < 0 || toIndex >= node.Choices.Count)
                return false;
            if (fromIndex == toIndex)
                return true;

            var item = node.Choices[fromIndex];
            node.Choices.RemoveAt(fromIndex);
            node.Choices.Insert(toIndex, item);
            PublishGraphChanged(chapterId, graph);
            return true;
        }

        public bool SetChoiceTarget(string chapterId, string nodeId, string choiceId, string? targetNodeId)
        {
            var node = FindNode(chapterId, nodeId, out var graph);
            var choice = node?.Choices?.FirstOrDefault(c => c.Id == choiceId);
            if (choice == null || graph == null)
                return false;

            choice.TargetNodeId = targetNodeId;
            PublishGraphChanged(chapterId, graph);
            return true;
        }

        public bool SetStartNode(string chapterId, string nodeId)
        {
            var graph = GetOrCreateGraph(chapterId);
            if (graph.Nodes.All(n => n.Id != nodeId))
                return false;

            graph.StartNodeId = nodeId;
            graph.EnsureValid();
            PublishGraphChanged(chapterId, graph);
            return true;
        }

        public void NotifyGraphChanged(string chapterId)
        {
            var graph = GetOrCreateGraph(chapterId);
            graph.EnsureValid();
            PublishGraphChanged(chapterId, graph);
        }

        public BranchingDialogueValidationResult ValidateGraph(string chapterId)
        {
            var graph = GetOrCreateGraph(chapterId);
            var result = new BranchingDialogueValidationResult();
            var nodeIds = new HashSet<string>(graph.Nodes.Select(n => n.Id));

            foreach (var node in graph.Nodes)
            {
                foreach (var choice in node.Choices ?? Enumerable.Empty<BranchingDialogueChoiceData>())
                {
                    if (string.IsNullOrWhiteSpace(choice.Text))
                    {
                        result.EmptyChoiceText.Add(new BranchingDialogueValidationIssue
                        {
                            NodeId = node.Id,
                            ChoiceId = choice.Id,
                            Message = "Choice text is empty."
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(choice.TargetNodeId) && !nodeIds.Contains(choice.TargetNodeId))
                    {
                        result.MissingTargets.Add(new BranchingDialogueValidationIssue
                        {
                            NodeId = node.Id,
                            ChoiceId = choice.Id,
                            Message = "Choice target node is missing."
                        });
                    }
                }
            }

            var reachable = GetReachableNodeIds(graph);
            foreach (var node in graph.Nodes)
            {
                if (!reachable.Contains(node.Id))
                {
                    result.UnreachableNodes.Add(new BranchingDialogueValidationIssue
                    {
                        NodeId = node.Id,
                        Message = "Node is unreachable from start node."
                    });
                }
            }

            return result;
        }

        public BranchingDialogueSimulationState? StartSimulation(string chapterId)
        {
            var graph = GetOrCreateGraph(chapterId);
            if (graph?.StartNodeId == null)
                return null;

            var state = CreateSimulationState(chapterId, graph, graph.StartNodeId);
            _simulationByChapter[chapterId] = state;
            PublishSimulationState(chapterId, state);
            return state;
        }

        public BranchingDialogueSimulationState? ChooseChoice(string chapterId, string choiceId)
        {
            var graph = GetOrCreateGraph(chapterId);
            if (!_simulationByChapter.TryGetValue(chapterId, out var state) || state == null || !state.IsActive)
                state = StartSimulation(chapterId);

            if (state == null || graph?.Nodes == null)
                return state;

            var currentNode = graph.Nodes.FirstOrDefault(n => n?.Id == state.CurrentNodeId);
            var choice = currentNode?.Choices?.FirstOrDefault(c => c?.Id == choiceId);

            if (choice == null || string.IsNullOrWhiteSpace(choice.TargetNodeId))
            {
                state.IsDeadEnd = true;
                PublishSimulationState(chapterId, state);
                return state;
            }

            var nextNode = graph.Nodes.FirstOrDefault(n => n?.Id == choice.TargetNodeId);
            if (nextNode == null)
            {
                state.IsDeadEnd = true;
                PublishSimulationState(chapterId, state);
                return state;
            }

            state.CurrentNodeId = nextNode.Id;
            state.BreadcrumbNodeIds.Add(nextNode.Id);
            state.IsDeadEnd = (nextNode.Choices == null || nextNode.Choices.Count == 0);
            PublishSimulationState(chapterId, state);
            return state;
        }

        public BranchingDialogueSimulationState? RestartSimulation(string chapterId)
        {
            return StartSimulation(chapterId);
        }

        public void StopSimulation(string chapterId)
        {
            if (_simulationByChapter.TryGetValue(chapterId, out var state))
            {
                state.IsActive = false;
                PublishSimulationState(chapterId, state);
            }
        }

        public BranchingDialogueSimulationState? GetSimulationState(string chapterId)
        {
            _simulationByChapter.TryGetValue(chapterId, out var state);
            return state;
        }

        private static HashSet<string> GetReachableNodeIds(BranchingDialogueGraphData graph)
        {
            var reachable = new HashSet<string>();
            var toVisit = new Stack<string>();

            if (!string.IsNullOrWhiteSpace(graph.StartNodeId))
                toVisit.Push(graph.StartNodeId);

            while (toVisit.Count > 0)
            {
                var current = toVisit.Pop();
                if (!reachable.Add(current))
                    continue;

                var node = graph.Nodes.FirstOrDefault(n => n.Id == current);
                if (node?.Choices == null)
                    continue;

                foreach (var target in node.Choices
                    .Where(c => !string.IsNullOrWhiteSpace(c.TargetNodeId))
                    .Select(c => c.TargetNodeId))
                {
                    toVisit.Push(target);
                }
            }

            return reachable;
        }

        private static BranchingDialogueSimulationState CreateSimulationState(string chapterId, BranchingDialogueGraphData graph, string? startNodeId)
        {
            if (string.IsNullOrWhiteSpace(startNodeId))
                startNodeId = graph?.StartNodeId;

            var currentNode = string.IsNullOrWhiteSpace(startNodeId) ? null : graph?.Nodes?.FirstOrDefault(n => n?.Id == startNodeId);
            var state = new BranchingDialogueSimulationState
            {
                ChapterId = chapterId,
                GraphId = graph?.Id,
                CurrentNodeId = startNodeId,
                IsActive = true,
                IsDeadEnd = currentNode == null || currentNode.Choices == null || currentNode.Choices.Count == 0,
                BreadcrumbNodeIds = new List<string> { startNodeId ?? string.Empty }
            };

            return state;
        }

        private BranchingDialogueNodeData? FindNode(string chapterId, string nodeId, out BranchingDialogueGraphData? graph)
        {
            graph = GetOrCreateGraph(chapterId);
            return graph?.Nodes?.FirstOrDefault(n => n?.Id == nodeId);
        }

        private void PublishGraphChanged(string chapterId, BranchingDialogueGraphData? graph)
        {
            _events.PublishGraphChanged(chapterId, graph?.Id);
        }

        private void PublishSimulationState(string chapterId, BranchingDialogueSimulationState? state)
        {
            _events.PublishSimulationStateChanged(chapterId, state);
        }
    }
}