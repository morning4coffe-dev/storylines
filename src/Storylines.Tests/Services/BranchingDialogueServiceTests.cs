using Storylines.Models;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace Storylines.Tests.Services;

public class BranchingDialogueServiceTests
{
    private readonly FakeStore _store = new();
    private readonly FakeEvents _events = new();

    private BranchingDialogueService CreateService()
    {
        return new BranchingDialogueService(_store, _events);
    }

    [Fact]
    public void CreateNode_CreatesGraphAndPublishesEvent()
    {
        var service = CreateService();

        var node = service.CreateNode("chapter-1");

        Assert.NotNull(node);
        Assert.Single(_store.Graphs);
        Assert.Single(_store.Graphs[0].Nodes.Where(n => n.Id == node.Id));
        Assert.Single(_events.GraphChanged);
        Assert.Equal("chapter-1", _events.GraphChanged[0].ChapterId);
    }

    [Fact]
    public void ValidateGraph_ReturnsMissingTargetAndEmptyChoiceAndUnreachableWarnings()
    {
        var service = CreateService();
        var graph = service.GetOrCreateGraph("chapter-1");

        var start = graph.Nodes[0];
        start.Choices.Add(new BranchingDialogueChoiceData { Id = "choice-1", Text = "", TargetNodeId = "missing-node" });
        graph.Nodes.Add(new BranchingDialogueNodeData { Id = "orphan", Text = "orphan", Choices = new List<BranchingDialogueChoiceData>() });

        var result = service.ValidateGraph("chapter-1");

        Assert.Single(result.MissingTargets);
        Assert.Single(result.EmptyChoiceText);
        Assert.Single(result.UnreachableNodes);
        Assert.Equal("orphan", result.UnreachableNodes[0].NodeId);
    }

    [Fact]
    public void ChooseChoice_TraversesAndMarksDeadEnd()
    {
        var service = CreateService();
        var graph = service.GetOrCreateGraph("chapter-1");
        var start = graph.Nodes[0];
        var end = service.CreateNode("chapter-1", "End");

        var choice = service.AddChoice("chapter-1", start.Id, "Go", end.Id);
        var started = service.StartSimulation("chapter-1");
        var advanced = service.ChooseChoice("chapter-1", choice.Id);

        Assert.Equal(start.Id, started.BreadcrumbNodeIds[0]);
        Assert.Equal(end.Id, advanced.CurrentNodeId);
        Assert.True(advanced.IsDeadEnd);
        Assert.Equal(2, advanced.BreadcrumbNodeIds.Count);
        Assert.True(_events.SimulationChanged.Count >= 2);
    }

    [Fact]
    public void DeleteNode_RemovesIncomingChoices()
    {
        var service = CreateService();
        var graph = service.GetOrCreateGraph("chapter-1");
        var start = graph.Nodes[0];
        var destination = service.CreateNode("chapter-1", "Dest");
        service.AddChoice("chapter-1", start.Id, "to-dest", destination.Id);

        var deleted = service.DeleteNode("chapter-1", destination.Id);

        Assert.True(deleted);
        Assert.Empty(start.Choices);
    }

    [Fact]
    public void RestartSimulation_ResetsBreadcrumbToStart()
    {
        var service = CreateService();
        var graph = service.GetOrCreateGraph("chapter-1");
        var start = graph.Nodes[0];
        var end = service.CreateNode("chapter-1", "End");
        var choice = service.AddChoice("chapter-1", start.Id, "Go", end.Id);

        service.StartSimulation("chapter-1");
        service.ChooseChoice("chapter-1", choice.Id);
        var restarted = service.RestartSimulation("chapter-1");

        Assert.Single(restarted.BreadcrumbNodeIds);
        Assert.Equal(start.Id, restarted.BreadcrumbNodeIds[0]);
    }

    [Fact]
    public void ValidateGraph_WithLoop_DoesNotMarkReachableNodesAsUnreachable()
    {
        var service = CreateService();
        var graph = service.GetOrCreateGraph("chapter-1");
        var start = graph.Nodes[0];
        var second = service.CreateNode("chapter-1", "Second");

        service.AddChoice("chapter-1", start.Id, "To second", second.Id);
        service.AddChoice("chapter-1", second.Id, "Back to start", start.Id);

        var result = service.ValidateGraph("chapter-1");

        Assert.Empty(result.UnreachableNodes);
        Assert.Empty(result.MissingTargets);
    }

        [Fact]
        public void RenameNode_ExistingNode_UpdatesTitleAndPublishesEvent()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];

            var result = service.RenameNode("chapter-1", start.Id, "Renamed Title");

            Assert.True(result);
            Assert.Equal("Renamed Title", start.Title);
            Assert.True(_events.GraphChanged.Count >= 1);
        }

        [Fact]
        public void RenameNode_NonExistentNode_ReturnsFalse()
        {
            var service = CreateService();
            service.GetOrCreateGraph("chapter-1");

            var result = service.RenameNode("chapter-1", "no-such-node", "New Title");

            Assert.False(result);
        }

        [Fact]
        public void RemoveChoice_ExistingChoice_RemovesItAndPublishesEvent()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];
            var choice = service.AddChoice("chapter-1", node.Id, "Option A");
            var countBefore = _events.GraphChanged.Count;

            var removed = service.RemoveChoice("chapter-1", node.Id, choice.Id);

            Assert.True(removed);
            Assert.Empty(node.Choices);
            Assert.True(_events.GraphChanged.Count > countBefore);
        }

        [Fact]
        public void RemoveChoice_NonExistentChoice_ReturnsFalse()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];

            var result = service.RemoveChoice("chapter-1", node.Id, "missing-choice");

            Assert.False(result);
        }

        [Fact]
        public void ReorderChoices_MovesChoiceToNewPosition()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];
            service.AddChoice("chapter-1", node.Id, "First");
            service.AddChoice("chapter-1", node.Id, "Second");
            service.AddChoice("chapter-1", node.Id, "Third");

            var result = service.ReorderChoices("chapter-1", node.Id, 0, 2);

            Assert.True(result);
            Assert.Equal("Second", node.Choices[0].Text);
            Assert.Equal("Third", node.Choices[1].Text);
            Assert.Equal("First", node.Choices[2].Text);
        }

        [Fact]
        public void ReorderChoices_OutOfBoundsIndex_ReturnsFalse()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];
            service.AddChoice("chapter-1", node.Id, "Only choice");

            var result = service.ReorderChoices("chapter-1", node.Id, 0, 5);

            Assert.False(result);
        }

        [Fact]
        public void ReorderChoices_SameIndex_ReturnsTrueWithNoChange()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];
            service.AddChoice("chapter-1", node.Id, "Alpha");

            var result = service.ReorderChoices("chapter-1", node.Id, 0, 0);

            Assert.True(result);
            Assert.Equal("Alpha", node.Choices[0].Text);
        }

        [Fact]
        public void SetChoiceTarget_UpdatesTargetNodeId()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            var dest = service.CreateNode("chapter-1", "Dest");
            var choice = service.AddChoice("chapter-1", start.Id, "Go");

            var result = service.SetChoiceTarget("chapter-1", start.Id, choice.Id, dest.Id);

            Assert.True(result);
            Assert.Equal(dest.Id, choice.TargetNodeId);
        }

        [Fact]
        public void SetStartNode_ChangesStartNodeId()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var originalStart = graph.StartNodeId;
            var second = service.CreateNode("chapter-1", "Second");

            var result = service.SetStartNode("chapter-1", second.Id);

            Assert.True(result);
            Assert.Equal(second.Id, graph.StartNodeId);
            Assert.NotEqual(originalStart, graph.StartNodeId);
        }

        [Fact]
        public void SetStartNode_NonExistentNodeId_ReturnsFalse()
        {
            var service = CreateService();
            service.GetOrCreateGraph("chapter-1");

            var result = service.SetStartNode("chapter-1", "node-that-does-not-exist");

            Assert.False(result);
        }

        [Fact]
        public void GetOrCreateGraph_NullChapterId_ThrowsArgumentException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentException>(() => service.GetOrCreateGraph(null));
        }

        [Fact]
        public void GetOrCreateGraph_EmptyChapterId_ThrowsArgumentException()
        {
            var service = CreateService();
            Assert.Throws<ArgumentException>(() => service.GetOrCreateGraph("   "));
        }

        [Fact]
        public void MultipleChapters_HaveIndependentGraphs()
        {
            var service = CreateService();
            var graph1 = service.GetOrCreateGraph("chapter-1");
            var graph2 = service.GetOrCreateGraph("chapter-2");

            service.CreateNode("chapter-1", "Node for ch1");

            // chapter-2's graph should still have only the default start node
            Assert.Equal(2, graph1.Nodes.Count);
            Assert.Single(graph2.Nodes);
        }

        [Fact]
        public void GetOrCreateGraph_CalledTwice_ReturnsSameGraph()
        {
            var service = CreateService();
            var first = service.GetOrCreateGraph("chapter-1");
            var second = service.GetOrCreateGraph("chapter-1");

            Assert.Same(first, second);
        }

        [Fact]
        public void ValidateGraph_CleanGraph_HasNoWarnings()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            var dest = service.CreateNode("chapter-1", "End");
            service.AddChoice("chapter-1", start.Id, "Continue", dest.Id);

            var result = service.ValidateGraph("chapter-1");

            Assert.False(result.HasWarnings);
        }

        [Fact]
        public void CreateNode_WithNullOrWhitespaceTitle_UsesDefault()
        {
            var service = CreateService();
            var node1 = service.CreateNode("chapter-1", null);
            var node2 = service.CreateNode("chapter-1", "   ");

            Assert.NotNull(node1.Title);
            Assert.NotNull(node2.Title);
            Assert.NotEqual(node1.Title, node2.Title);
        }

        [Fact]
        public void AddChoice_WithNullTargetNodeId_AllowsDeadEnd()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];

            var choice = service.AddChoice("chapter-1", node.Id, "Dead end choice", null);

            Assert.NotNull(choice);
            Assert.Null(choice.TargetNodeId);
        }

        [Fact]
        public void ValidateGraph_DetectsMultipleIssueTypes()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            var orphan = service.CreateNode("chapter-1", "Orphan");

            service.AddChoice("chapter-1", start.Id, "", "missing-id");
            service.AddChoice("chapter-1", start.Id, "", "another-missing");

            var result = service.ValidateGraph("chapter-1");

            Assert.Equal(2, result.MissingTargets.Count);
            Assert.Equal(1, result.UnreachableNodes.Count);
            Assert.Equal(2, result.EmptyChoiceText.Count);
            Assert.True(result.HasWarnings);
        }

        [Fact]
        public void DeleteNode_RemovesAllReferencesAndPublishesEvent()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            var middle = service.CreateNode("chapter-1", "Middle");
            var end = service.CreateNode("chapter-1", "End");

            var c1 = service.AddChoice("chapter-1", start.Id, "A", middle.Id);
            var c2 = service.AddChoice("chapter-1", middle.Id, "B", end.Id);
            var c3 = service.AddChoice("chapter-1", start.Id, "C", middle.Id);

            var countBefore = _events.GraphChanged.Count;
            var deleted = service.DeleteNode("chapter-1", middle.Id);

            Assert.True(deleted);
            Assert.Empty(start.Choices.Where(c => c.TargetNodeId == middle.Id));
            Assert.True(_events.GraphChanged.Count > countBefore);
        }

        [Fact]
        public void ChooseChoice_WithMissingTargetNode_MarksDeadEnd()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];

            var choice = new BranchingDialogueChoiceData
            {
                Id = "broken-choice",
                Text = "Go nowhere",
                TargetNodeId = "missing-node"
            };
            start.Choices.Add(choice);

            var started = service.StartSimulation("chapter-1");
            var state = service.ChooseChoice("chapter-1", choice.Id);

            Assert.NotNull(state);
            Assert.True(state.IsDeadEnd);
        }

        [Fact]
        public void SimulationState_PreservesVariablesAcrossChoices()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            var end = service.CreateNode("chapter-1", "End");

            service.AddChoice("chapter-1", start.Id, "Go", end.Id);

            var state1 = service.StartSimulation("chapter-1");
            state1.Variables["test"] = "value";

            var choice = start.Choices[0];
            var state2 = service.ChooseChoice("chapter-1", choice.Id);

            Assert.Equal("value", state2?.Variables["test"]);
        }

        [Fact]
        public void SetStartNode_ToInvalidNode_ReturnsFalse()
        {
            var service = CreateService();
            service.GetOrCreateGraph("chapter-1");

            var result = service.SetStartNode("chapter-1", "does-not-exist");

            Assert.False(result);
        }

        [Fact]
        public void ReorderChoices_WithInvalidIndices_ReturnsFalseWithoutMutation()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];
            service.AddChoice("chapter-1", node.Id, "Only choice");

            var result1 = service.ReorderChoices("chapter-1", node.Id, 0, 10);
            var result2 = service.ReorderChoices("chapter-1", node.Id, -1, 0);
            var result3 = service.ReorderChoices("chapter-1", node.Id, 5, 0);

            Assert.False(result1);
            Assert.False(result2);
            Assert.False(result3);
            Assert.Single(node.Choices);
        }

        [Fact]
        public void ComplexGraph_WithMultipleBranches_ValidatesCorrectly()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            var a = service.CreateNode("chapter-1", "A");
            var b = service.CreateNode("chapter-1", "B");
            var c = service.CreateNode("chapter-1", "C");
            var d = service.CreateNode("chapter-1", "D");

            service.AddChoice("chapter-1", start.Id, "To A", a.Id);
            service.AddChoice("chapter-1", start.Id, "To B", b.Id);
            service.AddChoice("chapter-1", a.Id, "To C", c.Id);
            service.AddChoice("chapter-1", b.Id, "To D", d.Id);
            service.AddChoice("chapter-1", c.Id, "To A", a.Id);
            service.AddChoice("chapter-1", d.Id, "Loop to B", b.Id);

            var result = service.ValidateGraph("chapter-1");

            Assert.False(result.HasWarnings);
            Assert.Empty(result.UnreachableNodes);
        }

        #region Condition and Action Tests

        [Fact]
        public void AddCondition_CreatesConditionOnChoice()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];
            var choice = service.AddChoice("chapter-1", node.Id, "Go");

            var condition = service.AddCondition("chapter-1", node.Id, choice.Id, "visited", ConditionOperator.Equals, "true");

            Assert.NotNull(condition);
            Assert.Equal("visited", condition.Flag);
            Assert.Equal(ConditionOperator.Equals, condition.Operator);
            Assert.Equal("true", condition.Value);
            Assert.Single(choice.Conditions);
        }

        [Fact]
        public void RemoveCondition_RemovesConditionByIndex()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];
            var choice = service.AddChoice("chapter-1", node.Id, "Go");
            service.AddCondition("chapter-1", node.Id, choice.Id, "flag1");
            service.AddCondition("chapter-1", node.Id, choice.Id, "flag2");

            var result = service.RemoveCondition("chapter-1", node.Id, choice.Id, 0);

            Assert.True(result);
            Assert.Single(choice.Conditions);
            Assert.Equal("flag2", choice.Conditions[0].Flag);
        }

        [Fact]
        public void RemoveCondition_InvalidIndex_ReturnsFalse()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];
            var choice = service.AddChoice("chapter-1", node.Id, "Go");

            var result = service.RemoveCondition("chapter-1", node.Id, choice.Id, 5);

            Assert.False(result);
        }

        [Fact]
        public void AddAction_CreatesActionOnNode()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];

            var action = service.AddAction("chapter-1", node.Id, "hasKey", "true");

            Assert.NotNull(action);
            Assert.Equal("hasKey", action.Flag);
            Assert.Equal("true", action.Value);
            Assert.Single(node.Actions);
        }

        [Fact]
        public void RemoveAction_RemovesActionByIndex()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];
            service.AddAction("chapter-1", node.Id, "flag1", "v1");
            service.AddAction("chapter-1", node.Id, "flag2", "v2");

            var result = service.RemoveAction("chapter-1", node.Id, 0);

            Assert.True(result);
            Assert.Single(node.Actions);
            Assert.Equal("flag2", node.Actions[0].Flag);
        }

        [Fact]
        public void RemoveAction_InvalidIndex_ReturnsFalse()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var node = graph.Nodes[0];

            var result = service.RemoveAction("chapter-1", node.Id, 0);

            Assert.False(result);
        }

        [Fact]
        public void Simulation_ExecutesActionsOnEntry()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            start.Actions = new List<BranchingDialogueActionData>
            {
                new BranchingDialogueActionData { Flag = "entered", Value = "yes" }
            };

            var state = service.StartSimulation("chapter-1");

            Assert.True(state.Variables.ContainsKey("entered"));
            Assert.Equal("yes", state.Variables["entered"]);
        }

        [Fact]
        public void Simulation_ConditionFiltersAvailableChoices()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            var nodeA = service.CreateNode("chapter-1", "A");
            var nodeB = service.CreateNode("chapter-1", "B");

            var choiceA = service.AddChoice("chapter-1", start.Id, "Go A", nodeA.Id);
            var choiceB = service.AddChoice("chapter-1", start.Id, "Go B (locked)", nodeB.Id);
            choiceB.Conditions = new List<BranchingDialogueConditionData>
            {
                new BranchingDialogueConditionData { Flag = "key", Operator = ConditionOperator.Equals, Value = "true" }
            };

            var state = service.StartSimulation("chapter-1");

            // Without the key variable, only choiceA should be available
            var available = BranchingDialogueService.GetAvailableChoices(start, state);
            Assert.Single(available);
            Assert.Equal(choiceA.Id, available[0].Id);
        }

        [Fact]
        public void Simulation_ConditionIsSet_AllowsWhenVariableExists()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            start.Actions = new List<BranchingDialogueActionData>
            {
                new BranchingDialogueActionData { Flag = "key", Value = "true" }
            };
            var nodeA = service.CreateNode("chapter-1", "A");
            var choiceA = service.AddChoice("chapter-1", start.Id, "Go A", nodeA.Id);
            choiceA.Conditions = new List<BranchingDialogueConditionData>
            {
                new BranchingDialogueConditionData { Flag = "key", Operator = ConditionOperator.IsSet }
            };

            var state = service.StartSimulation("chapter-1");
            var available = BranchingDialogueService.GetAvailableChoices(start, state);

            Assert.Single(available);
        }

        [Fact]
        public void ValidateGraph_DetectsUnknownSpeakers()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            start.Speaker = "UnknownPerson";

            var knownSpeakers = new List<string> { "Alice", "Bob" };
            var result = service.ValidateGraph("chapter-1", knownSpeakers);

            Assert.Single(result.UnknownSpeakers);
            Assert.Contains("UnknownPerson", result.UnknownSpeakers[0].Message);
        }

        [Fact]
        public void ValidateGraph_DetectsOrphanedConditions()
        {
            var service = CreateService();
            var graph = service.GetOrCreateGraph("chapter-1");
            var start = graph.Nodes[0];
            var nodeB = service.CreateNode("chapter-1", "B");
            var choice = service.AddChoice("chapter-1", start.Id, "Go B", nodeB.Id);

            // Add a condition referencing flag "key" but no node sets it
            choice.Conditions = new List<BranchingDialogueConditionData>
            {
                new BranchingDialogueConditionData { Flag = "key", Operator = ConditionOperator.Equals, Value = "true" }
            };

            var result = service.ValidateGraph("chapter-1");

            Assert.Single(result.OrphanedConditions);
            Assert.Contains("key", result.OrphanedConditions[0].Message);
        }

        #endregion

        private sealed class FakeStore : IBranchingDialogueStore
    {
        public List<BranchingDialogueGraphData> Graphs { get; } = new();

        public List<BranchingDialogueGraphData> BranchingDialogues => Graphs;

        public BranchingDialogueGraphData GetOrCreateGraph(string chapterId)
        {
            var found = Graphs.FirstOrDefault(g => g.ChapterId == chapterId);
            if (found != null)
            {
                found.EnsureValid();
                return found;
            }

            var graph = new BranchingDialogueGraphData
            {
                Id = "graph-" + chapterId,
                ChapterId = chapterId,
                Nodes = new List<BranchingDialogueNodeData>()
            };
            graph.EnsureValid();
            Graphs.Add(graph);
            return graph;
        }
    }

    private sealed class FakeEvents : IBranchingDialogueEventPublisher
    {
        public List<(string ChapterId, string? GraphId)> GraphChanged { get; } = new();
        public List<(string ChapterId, BranchingDialogueSimulationState? State)> SimulationChanged { get; } = new();

        public void PublishGraphChanged(string chapterId, string? graphId)
        {
            GraphChanged.Add((chapterId, graphId));
        }

        public void PublishSimulationStateChanged(string chapterId, BranchingDialogueSimulationState? state)
        {
            SimulationChanged.Add((chapterId, state));
        }
    }
}
