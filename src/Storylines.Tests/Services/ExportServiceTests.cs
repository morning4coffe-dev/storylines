using Storylines.Helpers;
using Storylines.Models;
using System.Collections.Generic;
using Xunit;

namespace Storylines.Tests.Services;

public class ExportServiceTests
{
    private BranchingDialogueGraphData CreateTestGraph()
    {
        var start = new BranchingDialogueNodeData
        {
            Id = "start-1",
            Title = "Opening",
            Speaker = "Alice",
            Text = "Hello, welcome!",
            Tags = new List<string> { "intro" },
            PositionX = 100,
            PositionY = 200,
            Choices = new List<BranchingDialogueChoiceData>
            {
                new BranchingDialogueChoiceData { Id = "c1", Text = "Continue", TargetNodeId = "node-2" }
            },
            Actions = new List<BranchingDialogueActionData>()
        };

        var second = new BranchingDialogueNodeData
        {
            Id = "node-2",
            Title = "Response",
            Speaker = "Bob",
            Text = "Nice to meet you.",
            Choices = new List<BranchingDialogueChoiceData>(),
            Actions = new List<BranchingDialogueActionData>()
        };

        return new BranchingDialogueGraphData
        {
            Id = "graph-1",
            ChapterId = "ch-1",
            StartNodeId = "start-1",
            Nodes = new List<BranchingDialogueNodeData> { start, second }
        };
    }

    [Fact]
    public void ConvertGraphToTwee_ProducesPassageHeaders()
    {
        var graph = CreateTestGraph();
        var twee = BranchingDialogueExportHelper.ConvertGraphToTwee(graph);

        Assert.Contains(":: Opening [intro start]", twee);
        Assert.Contains(":: Response", twee);
    }

    [Fact]
    public void ConvertGraphToTwee_IncludesSpeakerTag()
    {
        var graph = CreateTestGraph();
        var twee = BranchingDialogueExportHelper.ConvertGraphToTwee(graph);

        Assert.Contains("[speaker: Alice]", twee);
        Assert.Contains("[speaker: Bob]", twee);
    }

    [Fact]
    public void ConvertGraphToTwee_ProducesLinks()
    {
        var graph = CreateTestGraph();
        var twee = BranchingDialogueExportHelper.ConvertGraphToTwee(graph);

        Assert.Contains("[[Continue->Response]]", twee);
    }

    [Fact]
    public void ConvertGraphToTwee_IncludesPositionMetadata()
    {
        var graph = CreateTestGraph();
        var twee = BranchingDialogueExportHelper.ConvertGraphToTwee(graph);

        Assert.Contains("\"position\":\"100,200\"", twee);
    }

    [Fact]
    public void ConvertGraphToTwee_EmptyGraph_ReturnsEmpty()
    {
        var graph = new BranchingDialogueGraphData { Nodes = new List<BranchingDialogueNodeData>() };
        Assert.Equal(string.Empty, BranchingDialogueExportHelper.ConvertGraphToTwee(graph));
    }

    [Fact]
    public void ConvertGraphToScreenplay_ProducesCharacterLines()
    {
        var graph = CreateTestGraph();
        var screenplay = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

        Assert.Contains("ALICE:", screenplay);
        Assert.Contains("  Hello, welcome!", screenplay);
        Assert.Contains("BOB:", screenplay);
        Assert.Contains("  Nice to meet you.", screenplay);
    }

    [Fact]
    public void ConvertGraphToScreenplay_BfsOrder_StartFirst()
    {
        var graph = CreateTestGraph();
        var screenplay = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

        var aliceIndex = screenplay.IndexOf("ALICE:");
        var bobIndex = screenplay.IndexOf("BOB:");
        Assert.True(aliceIndex < bobIndex);
    }

    [Fact]
    public void ConvertGraphToScreenplay_EmptyGraph_ReturnsEmpty()
    {
        var graph = new BranchingDialogueGraphData { Nodes = new List<BranchingDialogueNodeData>() };
        Assert.Equal(string.Empty, BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph));
    }

    [Fact]
    public void ConvertGraphToScreenplay_NoSpeaker_UsesNarrator()
    {
        var graph = new BranchingDialogueGraphData
        {
            Id = "g1",
            ChapterId = "ch1",
            StartNodeId = "n1",
            Nodes = new List<BranchingDialogueNodeData>
            {
                new BranchingDialogueNodeData
                {
                    Id = "n1",
                    Speaker = null,
                    Text = "Silence.",
                    Choices = new List<BranchingDialogueChoiceData>(),
                    Actions = new List<BranchingDialogueActionData>()
                }
            }
        };

        var screenplay = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);
        Assert.Contains("NARRATOR:", screenplay);
    }
}
