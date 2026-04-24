using System.Collections.Generic;
using Storylines.Services.Serializers;
using Storylines.Models;
using Xunit;

namespace Storylines.Tests.Serializers;

public class JsonSaveSerializerTests
{
    private readonly JsonSaveSerializer _serializer = new();

    [Fact]
    public void RoundTrip_AllFields_ArePreserved()
    {
        var original = new ProjectData
        {
            Name = "My Novel",
            Version = "0.8.0.0",
            LastOpenedChapter = 2,
            Chapters = new List<ChapterData>
            {
                new() { Name = "Prologue", Text = "It began...", Notes = "revise this", Synopsis = "The setup.", WordCountGoal = 300 },
                new() { Name = "Chapter 1", Text = "The hero walks in.", Notes = string.Empty }
            },
            Characters = new List<CharacterData>
            {
                new() { Name = "Alice", Description = "The protagonist.", Role = "Hero", Age = "28", Appearance = "Tall", Traits = new List<string> { "brave", "clever" } }
            }
        };

        var json = _serializer.Serialize(original);
        var result = _serializer.Deserialize(json);

        Assert.Equal("My Novel", result.Name);
        Assert.Equal("0.8.0.0", result.Version);
        Assert.Equal(2, result.LastOpenedChapter);

        Assert.Equal(2, result.Chapters.Count);
        Assert.Equal("Prologue", result.Chapters[0].Name);
        Assert.Equal("It began...", result.Chapters[0].Text);
        Assert.Equal("revise this", result.Chapters[0].Notes);
        Assert.Equal("The setup.", result.Chapters[0].Synopsis);
        Assert.Equal(300, result.Chapters[0].WordCountGoal);

        Assert.Equal("Chapter 1", result.Chapters[1].Name);
        Assert.Equal(string.Empty, result.Chapters[1].Notes);

        Assert.Single(result.Characters);
        Assert.Equal("Alice", result.Characters[0].Name);
        Assert.Equal("Hero", result.Characters[0].Role);
        Assert.Equal("28", result.Characters[0].Age);
        Assert.Equal("Tall", result.Characters[0].Appearance);
        Assert.Equal(new[] { "brave", "clever" }, result.Characters[0].Traits);
    }

    [Fact]
    public void Serialize_ProducesJsonStartingWithBrace()
    {
        var json = _serializer.Serialize(new ProjectData { Name = "Test" });
        Assert.True(json.TrimStart().StartsWith("{"));
    }

    [Fact]
    public void Serialize_ProducesIndentedJson()
    {
        var json = _serializer.Serialize(new ProjectData { Name = "Test" });
        Assert.Contains("\n", json);
    }

    [Fact]
    public void CanDeserialize_ValidJson_ReturnsTrue()
    {
        var json = _serializer.Serialize(new ProjectData());
        Assert.True(_serializer.CanDeserialize(json));
    }

    [Fact]
    public void CanDeserialize_EmptyString_ReturnsFalse()
    {
        Assert.False(_serializer.CanDeserialize(string.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void CanDeserialize_NullOrWhitespace_ReturnsFalse(string? content)
    {
        Assert.False(_serializer.CanDeserialize(content!));
    }

    [Fact]
    public void CanDeserialize_LegacySrlContent_ReturnsFalse()
    {
        // Legacy format does not start with '{'
        const string legacyContent = "version>[Y\u2267g&<0.5.0@N*\u221b$\nname>[Y\u2267g&<My Story@N*\u221b$\n";
        Assert.False(_serializer.CanDeserialize(legacyContent));
    }

    [Fact]
    public void Serialize_NullSynopsisAndWordCountGoal_AreOmittedFromJson()
    {
        var data = new ProjectData
        {
            Chapters = new List<ChapterData>
            {
                new() { Name = "Ch", Text = "T", Notes = "N", Synopsis = null, WordCountGoal = null }
            }
        };

        var json = _serializer.Serialize(data);

        Assert.DoesNotContain("synopsis", json);
        Assert.DoesNotContain("wordCountGoal", json);
    }

    [Fact]
    public void Serialize_NullCharacterOptionalFields_AreOmittedFromJson()
    {
        var data = new ProjectData
        {
            Characters = new List<CharacterData>
            {
                new() { Name = "Bob", Description = "A guy.", Role = null, Age = null, Appearance = null, Traits = null }
            }
        };

        var json = _serializer.Serialize(data);

        Assert.DoesNotContain("\"role\"", json);
        Assert.DoesNotContain("\"age\"", json);
        Assert.DoesNotContain("\"traits\"", json);
    }

    [Fact]
    public void RoundTrip_EmptyProject_DoesNotThrow()
    {
        var data = new ProjectData();
        var json = _serializer.Serialize(data);
        var result = _serializer.Deserialize(json);
        Assert.Empty(result.Chapters);
        Assert.Empty(result.Characters);
    }

    [Fact]
    public void Serialize_DefaultFormat_IsJsonV1()
    {
        var json = _serializer.Serialize(new ProjectData());
        Assert.Contains("\"json-v1\"", json);
    }

    [Fact]
    public void RoundTrip_BranchingDialogues_PreservesIdentityAndLinks()
    {
        var chapterId = "chapter-1";
        var graphId = "graph-1";
        var startNodeId = "node-start";

        var original = new ProjectData
        {
            Chapters = new List<ChapterData>
            {
                new()
                {
                    Id = chapterId,
                    Name = "Chapter 1",
                    Text = "text",
                    Notes = "notes",
                    BranchingDialogueGraphId = graphId
                }
            },
            BranchingDialogues = new List<BranchingDialogueGraphData>
            {
                new()
                {
                    Id = graphId,
                    ChapterId = chapterId,
                    StartNodeId = startNodeId,
                    Nodes = new List<BranchingDialogueNodeData>
                    {
                        new()
                        {
                            Id = startNodeId,
                            Speaker = "Guide",
                            Text = "Welcome",
                            Choices = new List<BranchingDialogueChoiceData>
                            {
                                new()
                                {
                                    Id = "choice-1",
                                    Text = "Continue",
                                    TargetNodeId = "node-2"
                                }
                            }
                        },
                        new()
                        {
                            Id = "node-2",
                            Speaker = "Guide",
                            Text = "Next",
                            Choices = new List<BranchingDialogueChoiceData>()
                        }
                    }
                }
            }
        };

        var json = _serializer.Serialize(original);
        var result = _serializer.Deserialize(json);

        Assert.Single(result.Chapters);
        Assert.Single(result.BranchingDialogues);

        Assert.Equal(chapterId, result.Chapters[0].Id);
        Assert.Equal(graphId, result.Chapters[0].BranchingDialogueGraphId);

        var graph = result.BranchingDialogues[0];
        Assert.Equal(graphId, graph.Id);
        Assert.Equal(chapterId, graph.ChapterId);
        Assert.Equal(startNodeId, graph.StartNodeId);
        Assert.Equal(2, graph.Nodes.Count);
        Assert.Equal("choice-1", graph.Nodes[0].Choices[0].Id);
        Assert.Equal("node-2", graph.Nodes[0].Choices[0].TargetNodeId);
    }

    [Fact]
    public void Deserialize_LegacyJsonWithoutBranchingFields_RemainsCompatible()
    {
        const string legacyJson = "{\"format\":\"json-v1\",\"version\":\"1.0.0\",\"name\":\"Legacy\",\"lastOpenedChapter\":0,\"chapters\":[{\"name\":\"Chapter\",\"text\":\"Hello\",\"notes\":\"\"}],\"characters\":[]}";

        var result = _serializer.Deserialize(legacyJson);

        Assert.NotNull(result);
        Assert.Single(result.Chapters);
        Assert.Null(result.BranchingDialogues);
        Assert.Null(result.Chapters[0].Id);
        Assert.Null(result.Chapters[0].BranchingDialogueGraphId);
    }

    [Fact]
    public void RoundTrip_PlotThreads_ArePreserved()
        {
            var original = new ProjectData
            {
                PlotThreads = new List<string> { "Main quest", "Romance arc", "Redemption" }
            };

            var json = _serializer.Serialize(original);
            var result = _serializer.Deserialize(json);

            Assert.NotNull(result.PlotThreads);
            Assert.Equal(3, result.PlotThreads.Count);
            Assert.Equal("Main quest", result.PlotThreads[0]);
            Assert.Equal("Romance arc", result.PlotThreads[1]);
            Assert.Equal("Redemption", result.PlotThreads[2]);
        }

        [Fact]
        public void Serialize_NullPlotThreads_OmittedFromJson()
        {
            var data = new ProjectData { PlotThreads = null };
            var json = _serializer.Serialize(data);
            Assert.DoesNotContain("plotThreads", json);
        }

        [Fact]
        public void RoundTrip_PinboardConnections_ArePreserved()
        {
            var original = new ProjectData
            {
                Chapters = new List<ChapterData>
                {
                    new() { Name = "A", Text = "", Notes = "" },
                    new() { Name = "B", Text = "", Notes = "" },
                    new() { Name = "C", Text = "", Notes = "" }
                },
                PinboardConnections = new List<PinboardConnectionData>
                {
                    new() { FromIndex = 0, ToIndex = 2, Label = "foreshadowing" },
                    new() { FromIndex = 1, ToIndex = 2 }
                }
            };

            var json = _serializer.Serialize(original);
            var result = _serializer.Deserialize(json);

            Assert.NotNull(result.PinboardConnections);
            Assert.Equal(2, result.PinboardConnections.Count);
            Assert.Equal(0, result.PinboardConnections[0].FromIndex);
            Assert.Equal(2, result.PinboardConnections[0].ToIndex);
            Assert.Equal("foreshadowing", result.PinboardConnections[0].Label);
            Assert.Equal(1, result.PinboardConnections[1].FromIndex);
            Assert.Null(result.PinboardConnections[1].Label);
        }

        [Fact]
        public void Serialize_NullPinboardConnections_OmittedFromJson()
        {
            var data = new ProjectData { PinboardConnections = null };
            var json = _serializer.Serialize(data);
            Assert.DoesNotContain("pinboardConnections", json);
        }

        [Fact]
        public void RoundTrip_ChapterExtendedFields_ArePreserved()
        {
            var original = new ProjectData
            {
                Chapters = new List<ChapterData>
                {
                    new()
                    {
                        Name = "Prologue", Text = "text", Notes = "note",
                        Tags = new List<string> { "Flashback", "Emotional" },
                        PinboardX = 125.5,
                        PinboardY = 300.0,
                        Status = "InProgress",
                        Location = "Castle",
                        PlotThreads = new List<string> { "Main quest" }
                    }
                }
            };

            var json = _serializer.Serialize(original);
            var result = _serializer.Deserialize(json);

            var ch = result.Chapters[0];
            Assert.NotNull(ch.Tags);
            Assert.Equal(new[] { "Flashback", "Emotional" }, ch.Tags);
            Assert.Equal(125.5, ch.PinboardX);
            Assert.Equal(300.0, ch.PinboardY);
            Assert.Equal("InProgress", ch.Status);
            Assert.Equal("Castle", ch.Location);
            Assert.NotNull(ch.PlotThreads);
            Assert.Equal(new[] { "Main quest" }, ch.PlotThreads);
        }

        [Fact]
        public void Serialize_NullChapterExtendedFields_OmittedFromJson()
        {
            var data = new ProjectData
            {
                Chapters = new List<ChapterData>
                {
                    new() { Name = "Ch", Text = "T", Notes = "", Tags = null, Status = null, Location = null, PlotThreads = null }
                }
            };

            var json = _serializer.Serialize(data);

            Assert.DoesNotContain("\"tags\"", json);
            Assert.DoesNotContain("\"status\"", json);
            Assert.DoesNotContain("\"location\"", json);
            Assert.DoesNotContain("\"plotThreads\"", json);
        }
    }
