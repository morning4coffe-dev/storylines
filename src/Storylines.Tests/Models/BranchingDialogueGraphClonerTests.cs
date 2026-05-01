using System.Collections.Generic;
using System.Linq;
using Storylines.Models;
using Xunit;

namespace Storylines.Tests.Models;

public class BranchingDialogueGraphClonerTests
{
    [Fact]
    public void Clone_PreservesNodeContentAndNestedCollections()
    {
        var source = new BranchingDialogueGraphData
        {
            Id = "graph-1",
            ChapterId = "chapter-1",
            StartNodeId = "node-1",
            Nodes = new List<BranchingDialogueNodeData>
            {
                new BranchingDialogueNodeData
                {
                    Id = "node-1",
                    Title = "Intro",
                    Speaker = "Narrator",
                    CharacterToken = "char-1",
                    Text = "Hello",
                    Notes = "Remember this",
                    Tags = new List<string> { "entry" },
                    Metadata = new Dictionary<string, string> { ["mood"] = "tense" },
                    Actions = new List<BranchingDialogueActionData>
                    {
                        new BranchingDialogueActionData { Flag = "trust", Value = "+1" }
                    },
                    Choices = new List<BranchingDialogueChoiceData>
                    {
                        new BranchingDialogueChoiceData
                        {
                            Id = "choice-1",
                            Text = "Continue",
                            TargetNodeId = "node-2",
                            Metadata = new Dictionary<string, string> { ["style"] = "primary" },
                            Conditions = new List<BranchingDialogueConditionData>
                            {
                                new BranchingDialogueConditionData { Flag = "trust", Operator = ConditionOperator.Equals, Value = "1" }
                            }
                        }
                    }
                },
                new BranchingDialogueNodeData
                {
                    Id = "node-2",
                    Title = "Next",
                    Text = "World"
                }
            }
        };

        var clone = BranchingDialogueGraphCloner.Clone(source);

        Assert.NotNull(clone);
        Assert.Equal(source.Id, clone.Id);
        Assert.Equal(source.ChapterId, clone.ChapterId);
        Assert.Equal(source.StartNodeId, clone.StartNodeId);

        var clonedNode = clone.Nodes.Single(node => node.Id == "node-1");
        Assert.Equal("char-1", clonedNode.CharacterToken);
        Assert.Equal("Remember this", clonedNode.Notes);
        Assert.Equal(new[] { "entry" }, clonedNode.Tags);
        Assert.Equal("tense", clonedNode.Metadata["mood"]);
        Assert.Single(clonedNode.Actions);
        Assert.Equal("trust", clonedNode.Actions[0].Flag);
        Assert.Single(clonedNode.Choices);
        Assert.Equal("primary", clonedNode.Choices[0].Metadata["style"]);
        Assert.Single(clonedNode.Choices[0].Conditions);
        Assert.Equal(ConditionOperator.Equals, clonedNode.Choices[0].Conditions[0].Operator);

        Assert.NotSame(source.Nodes[0], clonedNode);
        Assert.NotSame(source.Nodes[0].Actions, clonedNode.Actions);
        Assert.NotSame(source.Nodes[0].Choices, clonedNode.Choices);
        Assert.NotSame(source.Nodes[0].Tags, clonedNode.Tags);
        Assert.NotSame(source.Nodes[0].Metadata, clonedNode.Metadata);
    }

    [Fact]
    public void Clone_WithRegeneratedIds_RemapsGraphAndChoiceTargets()
    {
        var source = new BranchingDialogueGraphData
        {
            Id = "graph-1",
            ChapterId = "chapter-1",
            StartNodeId = "node-1",
            Nodes = new List<BranchingDialogueNodeData>
            {
                new BranchingDialogueNodeData
                {
                    Id = "node-1",
                    Title = "Start",
                    Choices = new List<BranchingDialogueChoiceData>
                    {
                        new BranchingDialogueChoiceData
                        {
                            Id = "choice-1",
                            Text = "Go",
                            TargetNodeId = "node-2"
                        }
                    }
                },
                new BranchingDialogueNodeData
                {
                    Id = "node-2",
                    Title = "End"
                }
            }
        };

        var clone = BranchingDialogueGraphCloner.Clone(source, chapterIdOverride: "chapter-2", regenerateIds: true);

        Assert.NotNull(clone);
        Assert.Equal("chapter-2", clone.ChapterId);
        Assert.NotEqual(source.Id, clone.Id);
        Assert.Equal(2, clone.Nodes.Count);
        Assert.DoesNotContain(clone.Nodes, node => node.Id == "node-1" || node.Id == "node-2");
        Assert.DoesNotContain(clone.Nodes.SelectMany(node => node.Choices), choice => choice.Id == "choice-1");

        var clonedStart = clone.Nodes.Single(node => node.Id == clone.StartNodeId);
        var targetId = clonedStart.Choices.Single().TargetNodeId;

        Assert.Contains(clone.Nodes, node => node.Id == targetId);
        Assert.NotEqual("node-2", targetId);
    }
}