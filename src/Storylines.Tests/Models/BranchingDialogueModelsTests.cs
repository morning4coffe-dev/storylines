using Storylines.Models;
using System.Collections.Generic;
using Xunit;

namespace Storylines.Tests.Models;

/// <summary>
/// Tests for BranchingDialogueGraphData.EnsureValid, BranchingDialogueNodeData.EnsureValid,
/// and BranchingDialogueChoiceData.EnsureValid — the self-healing invariants that run on every
/// load and after every mutation.  Regressions here tend to cause silent data corruption.
/// </summary>
public class BranchingDialogueModelsTests
{
    #region BranchingDialogueGraphData.EnsureValid

    [Fact]
    public void EnsureValid_EmptyGraph_CreatesDefaultStartNode()
    {
        var graph = new BranchingDialogueGraphData { Id = "g1", ChapterId = "ch1" };
        // Nodes is already an empty list by default

        graph.EnsureValid();

        Assert.Single(graph.Nodes);
        Assert.Equal("Start", graph.Nodes[0].Title);
    }

    [Fact]
    public void EnsureValid_EmptyGraph_SetsStartNodeId()
    {
        var graph = new BranchingDialogueGraphData { Id = "g1", ChapterId = "ch1" };

        graph.EnsureValid();

        Assert.Equal(graph.Nodes[0].Id, graph.StartNodeId);
    }

    [Fact]
    public void EnsureValid_NullNodes_InitializesAndCreatesStartNode()
    {
        var graph = new BranchingDialogueGraphData { Id = "g1", ChapterId = "ch1", Nodes = null };

        graph.EnsureValid();

        Assert.NotNull(graph.Nodes);
        Assert.Single(graph.Nodes);
    }

    [Fact]
    public void EnsureValid_MissingId_AssignsId()
    {
        var graph = new BranchingDialogueGraphData { Id = null, ChapterId = "ch1" };

        graph.EnsureValid();

        Assert.False(string.IsNullOrWhiteSpace(graph.Id));
    }

    [Fact]
    public void EnsureValid_InvalidStartNodeId_FixesToFirstNode()
    {
        var node = new BranchingDialogueNodeData
        {
            Id = "node-1",
            Text = "First",
            Choices = new List<BranchingDialogueChoiceData>()
        };
        var graph = new BranchingDialogueGraphData
        {
            Id = "g1",
            ChapterId = "ch1",
            StartNodeId = "non-existent",
            Nodes = new List<BranchingDialogueNodeData> { node }
        };

        graph.EnsureValid();

        Assert.Equal("node-1", graph.StartNodeId);
    }

    [Fact]
    public void EnsureValid_ValidStartNodeId_IsPreserved()
    {
        var node1 = new BranchingDialogueNodeData { Id = "n1", Text = "", Choices = new() };
        var node2 = new BranchingDialogueNodeData { Id = "n2", Text = "", Choices = new() };
        var graph = new BranchingDialogueGraphData
        {
            Id = "g1",
            ChapterId = "ch1",
            StartNodeId = "n2",
            Nodes = new List<BranchingDialogueNodeData> { node1, node2 }
        };

        graph.EnsureValid();

        Assert.Equal("n2", graph.StartNodeId);
    }

    [Fact]
    public void EnsureValid_PropagatesEnsureValidToNodes()
    {
        // A node with a null Text and no Id should be fixed by EnsureValid
        var node = new BranchingDialogueNodeData { Id = null, Text = null, Choices = null };
        var graph = new BranchingDialogueGraphData
        {
            Id = "g1",
            ChapterId = "ch1",
            Nodes = new List<BranchingDialogueNodeData> { node }
        };

        graph.EnsureValid();

        Assert.False(string.IsNullOrWhiteSpace(node.Id));
        Assert.NotNull(node.Text);
        Assert.NotNull(node.Choices);
    }

    #endregion

    #region BranchingDialogueNodeData.EnsureValid

    [Fact]
    public void NodeEnsureValid_MissingId_AssignsId()
    {
        var node = new BranchingDialogueNodeData { Id = null, Text = "Hello", Choices = new() };
        node.EnsureValid();
        Assert.False(string.IsNullOrWhiteSpace(node.Id));
    }

    [Fact]
    public void NodeEnsureValid_NullText_SetsEmptyString()
    {
        var node = new BranchingDialogueNodeData { Id = "n1", Text = null, Choices = new() };
        node.EnsureValid();
        Assert.Equal(string.Empty, node.Text);
    }

    [Fact]
    public void NodeEnsureValid_NullChoices_InitializesEmptyList()
    {
        var node = new BranchingDialogueNodeData { Id = "n1", Text = "Hi", Choices = null };
        node.EnsureValid();
        Assert.NotNull(node.Choices);
        Assert.Empty(node.Choices);
    }

    [Fact]
    public void NodeEnsureValid_PropagatesEnsureValidToChoices()
    {
        var choice = new BranchingDialogueChoiceData { Id = null, Text = null };
        var node = new BranchingDialogueNodeData
        {
            Id = "n1",
            Text = "Hi",
            Choices = new List<BranchingDialogueChoiceData> { choice }
        };

        node.EnsureValid();

        Assert.False(string.IsNullOrWhiteSpace(choice.Id));
        Assert.Equal(string.Empty, choice.Text);
    }

    #endregion

    #region BranchingDialogueChoiceData.EnsureValid

    [Fact]
    public void ChoiceEnsureValid_MissingId_AssignsId()
    {
        var choice = new BranchingDialogueChoiceData { Id = null, Text = "Go left" };
        choice.EnsureValid();
        Assert.False(string.IsNullOrWhiteSpace(choice.Id));
    }

    [Fact]
    public void ChoiceEnsureValid_NullText_SetsEmptyString()
    {
        var choice = new BranchingDialogueChoiceData { Id = "c1", Text = null };
        choice.EnsureValid();
        Assert.Equal(string.Empty, choice.Text);
    }

    [Fact]
    public void ChoiceEnsureValid_NullConditions_InitializesEmptyList()
    {
        var choice = new BranchingDialogueChoiceData { Id = "c1", Text = "Go", Conditions = null };
        choice.EnsureValid();
        Assert.NotNull(choice.Conditions);
        Assert.Empty(choice.Conditions);
    }

    [Fact]
    public void ChoiceEnsureValid_ExistingId_PreservesIt()
    {
        var choice = new BranchingDialogueChoiceData { Id = "existing-id", Text = "Go" };
        choice.EnsureValid();
        Assert.Equal("existing-id", choice.Id);
    }

    #endregion

    #region BranchingDialogueValidationResult

    [Fact]
    public void ValidationResult_HasWarnings_FalseWhenEmpty()
    {
        var result = new BranchingDialogueValidationResult();
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void ValidationResult_HasWarnings_TrueWhenMissingTargets()
    {
        var result = new BranchingDialogueValidationResult();
        result.MissingTargets.Add(new BranchingDialogueValidationIssue { NodeId = "n1", Message = "missing" });
        Assert.True(result.HasWarnings);
    }

    [Fact]
    public void ValidationResult_HasWarnings_TrueWhenUnreachableNodes()
    {
        var result = new BranchingDialogueValidationResult();
        result.UnreachableNodes.Add(new BranchingDialogueValidationIssue { NodeId = "n1" });
        Assert.True(result.HasWarnings);
    }

    [Fact]
    public void ValidationResult_HasWarnings_TrueWhenEmptyChoiceText()
    {
        var result = new BranchingDialogueValidationResult();
        result.EmptyChoiceText.Add(new BranchingDialogueValidationIssue { ChoiceId = "c1" });
        Assert.True(result.HasWarnings);
    }

    #endregion
}
