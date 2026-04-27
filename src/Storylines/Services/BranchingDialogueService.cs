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
                Choices = new List<BranchingDialogueChoiceData>(),
                Actions = new List<BranchingDialogueActionData>()
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

        #region Condition & Action CRUD

        public BranchingDialogueConditionData AddCondition(string chapterId, string nodeId, string choiceId,
            string? flag = null, ConditionOperator op = ConditionOperator.Equals, string? value = null)
        {
            var node = FindNode(chapterId, nodeId, out var graph);
            var choice = node?.Choices?.FirstOrDefault(c => c.Id == choiceId);
            if (choice == null || graph == null)
                return null;

            choice.Conditions ??= new List<BranchingDialogueConditionData>();
            var condition = new BranchingDialogueConditionData
            {
                Flag = flag ?? string.Empty,
                Operator = op,
                Value = value
            };
            choice.Conditions.Add(condition);
            PublishGraphChanged(chapterId, graph);
            return condition;
        }

        public bool RemoveCondition(string chapterId, string nodeId, string choiceId, int conditionIndex)
        {
            var node = FindNode(chapterId, nodeId, out var graph);
            var choice = node?.Choices?.FirstOrDefault(c => c.Id == choiceId);
            if (choice?.Conditions == null || conditionIndex < 0 || conditionIndex >= choice.Conditions.Count)
                return false;

            choice.Conditions.RemoveAt(conditionIndex);
            PublishGraphChanged(chapterId, graph);
            return true;
        }

        public BranchingDialogueActionData AddAction(string chapterId, string nodeId,
            string? flag = null, string? value = null)
        {
            var node = FindNode(chapterId, nodeId, out var graph);
            if (node == null || graph == null)
                return null;

            node.Actions ??= new List<BranchingDialogueActionData>();
            var action = new BranchingDialogueActionData
            {
                Flag = flag ?? string.Empty,
                Value = value
            };
            node.Actions.Add(action);
            PublishGraphChanged(chapterId, graph);
            return action;
        }

        public bool RemoveAction(string chapterId, string nodeId, int actionIndex)
        {
            var node = FindNode(chapterId, nodeId, out var graph);
            if (node?.Actions == null || actionIndex < 0 || actionIndex >= node.Actions.Count)
                return false;

            node.Actions.RemoveAt(actionIndex);
            PublishGraphChanged(chapterId, graph);
            return true;
        }

        #endregion

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

        public BranchingDialogueValidationResult ValidateGraph(string chapterId, IEnumerable<string> knownSpeakers = null)
        {
            var graph = GetOrCreateGraph(chapterId);
            var result = new BranchingDialogueValidationResult();
            var nodeIds = new HashSet<string>(graph.Nodes.Select(n => n.Id));

            // Collect all flags set by any node action (for orphaned condition check)
            var allSetFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in graph.Nodes)
            {
                if (node.Actions != null)
                {
                    foreach (var action in node.Actions)
                    {
                        if (!string.IsNullOrWhiteSpace(action.Flag))
                            allSetFlags.Add(action.Flag);
                    }
                }
            }

            // Speaker validation set
            HashSet<string> speakerSet = null;
            if (knownSpeakers != null)
                speakerSet = new HashSet<string>(knownSpeakers, StringComparer.CurrentCultureIgnoreCase);

            foreach (var node in graph.Nodes)
            {
                // Unknown speaker validation
                if (speakerSet != null && !string.IsNullOrWhiteSpace(node.Speaker) && !speakerSet.Contains(node.Speaker))
                {
                    result.UnknownSpeakers.Add(new BranchingDialogueValidationIssue
                    {
                        NodeId = node.Id,
                        Message = $"Speaker \"{node.Speaker}\" does not match any character."
                    });
                }

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

                    // Orphaned conditions check
                    if (choice.Conditions != null)
                    {
                        foreach (var cond in choice.Conditions)
                        {
                            if (!string.IsNullOrWhiteSpace(cond.Flag)
                                && cond.Operator != ConditionOperator.IsNotSet
                                && !allSetFlags.Contains(cond.Flag))
                            {
                                result.OrphanedConditions.Add(new BranchingDialogueValidationIssue
                                {
                                    NodeId = node.Id,
                                    ChoiceId = choice.Id,
                                    Message = $"Condition references flag \"{cond.Flag}\" which is never set by any node."
                                });
                            }
                        }
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

            // Execute actions on the start node
            var startNode = graph.Nodes.FirstOrDefault(n => n?.Id == state.CurrentNodeId);
            ExecuteNodeActions(startNode, state);

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

            // Execute actions on the entered node
            ExecuteNodeActions(nextNode, state);

            // Check for dead-end considering condition filtering
            var availableChoices = GetAvailableChoices(nextNode, state);
            state.IsDeadEnd = availableChoices.Count == 0;

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
                _simulationByChapter.Remove(chapterId);
                PublishSimulationState(chapterId, state);
            }
        }

        public BranchingDialogueSimulationState? GetSimulationState(string chapterId)
        {
            _simulationByChapter.TryGetValue(chapterId, out var state);
            return state;
        }

        #region Helpers

        public static List<BranchingDialogueChoiceData> GetAvailableChoices(
            BranchingDialogueNodeData node, BranchingDialogueSimulationState state)
        {
            if (node?.Choices == null)
                return new List<BranchingDialogueChoiceData>();

            var variables = state?.Variables ?? new Dictionary<string, string>();
            return node.Choices.Where(c =>
            {
                if (c?.Conditions == null || c.Conditions.Count == 0)
                    return true;
                return c.Conditions.All(cond => cond.Evaluate(variables));
            }).ToList();
        }

        private static void ExecuteNodeActions(BranchingDialogueNodeData node, BranchingDialogueSimulationState state)
        {
            if (node?.Actions == null || state == null)
                return;

            state.Variables ??= new Dictionary<string, string>();
            foreach (var action in node.Actions)
                action.Execute(state.Variables);
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

        #endregion
    }
}