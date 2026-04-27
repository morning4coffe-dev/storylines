using Storylines.Helpers;
using Storylines.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace Storylines.Tests.Helpers
{
    public class BranchingDialogueExportHelperTests
    {
        #region ConvertGraphToScreenplay

        [Fact]
        public void ConvertGraphToScreenplay_NullGraph_ReturnsEmpty()
        {
            var result = BranchingDialogueExportHelper.ConvertGraphToScreenplay(null);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertGraphToScreenplay_EmptyNodes_ReturnsEmpty()
        {
            var graph = new BranchingDialogueGraphData
            {
                Nodes = new List<BranchingDialogueNodeData>()
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertGraphToScreenplay_NullNodes_ReturnsEmpty()
        {
            var graph = new BranchingDialogueGraphData { Nodes = null };

            var result = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertGraphToScreenplay_SingleNode_OutputsSpeakerAndText()
        {
            var graph = CreateLinearGraph("Alice", "Hello world");

            var result = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

            Assert.Contains("ALICE:", result);
            Assert.Contains("Hello world", result);
        }

        [Fact]
        public void ConvertGraphToScreenplay_NoSpeaker_DefaultsToNarrator()
        {
            var graph = new BranchingDialogueGraphData
            {
                StartNodeId = "n1",
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData { Id = "n1", Text = "Once upon a time" }
                }
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

            Assert.Contains("NARRATOR:", result);
        }

        [Fact]
        public void ConvertGraphToScreenplay_LinearChain_OutputsInOrder()
        {
            var graph = new BranchingDialogueGraphData
            {
                StartNodeId = "n1",
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData
                    {
                        Id = "n1",
                        Speaker = "Alice",
                        Text = "First",
                        Choices = new List<BranchingDialogueChoiceData>
                        {
                            new BranchingDialogueChoiceData { TargetNodeId = "n2" }
                        }
                    },
                    new BranchingDialogueNodeData
                    {
                        Id = "n2",
                        Speaker = "Bob",
                        Text = "Second"
                    }
                }
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

            var alicePos = result.IndexOf("ALICE:", StringComparison.Ordinal);
            var bobPos = result.IndexOf("BOB:", StringComparison.Ordinal);
            Assert.True(alicePos < bobPos, "Alice should appear before Bob");
        }

        [Fact]
        public void ConvertGraphToScreenplay_CyclicGraph_DoesNotInfiniteLoop()
        {
            var graph = new BranchingDialogueGraphData
            {
                StartNodeId = "n1",
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData
                    {
                        Id = "n1",
                        Speaker = "Alice",
                        Text = "Loop start",
                        Choices = new List<BranchingDialogueChoiceData>
                        {
                            new BranchingDialogueChoiceData { TargetNodeId = "n2" }
                        }
                    },
                    new BranchingDialogueNodeData
                    {
                        Id = "n2",
                        Speaker = "Bob",
                        Text = "Loop end",
                        Choices = new List<BranchingDialogueChoiceData>
                        {
                            new BranchingDialogueChoiceData { TargetNodeId = "n1" }
                        }
                    }
                }
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

            Assert.Contains("ALICE:", result);
            Assert.Contains("BOB:", result);
            // Each node visited exactly once (BFS with visited set)
            Assert.Equal(1, CountOccurrences(result, "ALICE:"));
            Assert.Equal(1, CountOccurrences(result, "BOB:"));
        }

        [Fact]
        public void ConvertGraphToScreenplay_OrphanNode_NotReachableFromStart()
        {
            var graph = new BranchingDialogueGraphData
            {
                StartNodeId = "n1",
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData
                    {
                        Id = "n1",
                        Speaker = "Alice",
                        Text = "Start"
                    },
                    new BranchingDialogueNodeData
                    {
                        Id = "orphan",
                        Speaker = "Ghost",
                        Text = "I should not appear"
                    }
                }
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

            Assert.Contains("ALICE:", result);
            Assert.DoesNotContain("GHOST:", result);
        }

        [Fact]
        public void ConvertGraphToScreenplay_NoStartNodeId_ReturnsEmpty()
        {
            var graph = new BranchingDialogueGraphData
            {
                StartNodeId = null,
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData { Id = "n1", Speaker = "Alice", Text = "Hello" }
                }
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph);

            Assert.Equal(string.Empty, result);
        }

        #endregion

        #region ConvertGraphToTwee

        [Fact]
        public void ConvertGraphToTwee_NullGraph_ReturnsEmpty()
        {
            var result = BranchingDialogueExportHelper.ConvertGraphToTwee(null);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertGraphToTwee_EmptyNodes_ReturnsEmpty()
        {
            var graph = new BranchingDialogueGraphData { Nodes = new List<BranchingDialogueNodeData>() };

            var result = BranchingDialogueExportHelper.ConvertGraphToTwee(graph);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertGraphToTwee_SingleNode_OutputsPassageHeader()
        {
            var graph = new BranchingDialogueGraphData
            {
                StartNodeId = "n1",
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData
                    {
                        Id = "n1",
                        Title = "Introduction",
                        Speaker = "Alice",
                        Text = "Hello"
                    }
                }
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToTwee(graph);

            Assert.Contains(":: Introduction", result);
            Assert.Contains("[start]", result);
            Assert.Contains("[speaker: Alice]", result);
            Assert.Contains("Hello", result);
        }

        [Fact]
        public void ConvertGraphToTwee_NodeWithoutTitle_UsesId()
        {
            var graph = new BranchingDialogueGraphData
            {
                StartNodeId = "node-123",
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData
                    {
                        Id = "node-123",
                        Text = "Some text"
                    }
                }
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToTwee(graph);

            Assert.Contains(":: node-123", result);
        }

        [Fact]
        public void ConvertGraphToTwee_ChoicesCreateLinks()
        {
            var graph = new BranchingDialogueGraphData
            {
                StartNodeId = "n1",
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData
                    {
                        Id = "n1",
                        Title = "Start",
                        Text = "Choose",
                        Choices = new List<BranchingDialogueChoiceData>
                        {
                            new BranchingDialogueChoiceData
                            {
                                Text = "Go left",
                                TargetNodeId = "n2"
                            }
                        }
                    },
                    new BranchingDialogueNodeData
                    {
                        Id = "n2",
                        Title = "LeftPath",
                        Text = "You went left"
                    }
                }
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToTwee(graph);

            Assert.Contains("[[Go left->LeftPath]]", result);
        }

        [Fact]
        public void ConvertGraphToTwee_NodeWithPosition_IncludesPositionMetadata()
        {
            var graph = new BranchingDialogueGraphData
            {
                StartNodeId = "n1",
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData
                    {
                        Id = "n1",
                        Title = "Start",
                        PositionX = 100.5,
                        PositionY = 200.7
                    }
                }
            };

            var result = BranchingDialogueExportHelper.ConvertGraphToTwee(graph);

            Assert.Contains("\"position\":\"100,200\"", result);
        }

        #endregion

        #region ImportFromJson

        [Fact]
        public void ImportFromJson_NullInput_ReturnsNull()
        {
            var result = BranchingDialogueExportHelper.ImportFromJson(null);

            Assert.Null(result);
        }

        [Fact]
        public void ImportFromJson_EmptyString_ReturnsNull()
        {
            var result = BranchingDialogueExportHelper.ImportFromJson("");

            Assert.Null(result);
        }

        [Fact]
        public void ImportFromJson_InvalidJson_ReturnsNull()
        {
            var result = BranchingDialogueExportHelper.ImportFromJson("not json at all");

            Assert.Null(result);
        }

        [Fact]
        public void ImportFromJson_ValidJson_ReturnsGraphData()
        {
            var json = "{\"id\":\"g1\",\"startNodeId\":\"n1\",\"nodes\":[{\"id\":\"n1\",\"text\":\"Hello\"}]}";

            var result = BranchingDialogueExportHelper.ImportFromJson(json);

            Assert.NotNull(result);
            Assert.Equal("g1", result.Id);
            Assert.Single(result.Nodes);
        }

        #endregion

        #region Helpers

        private static BranchingDialogueGraphData CreateLinearGraph(string speaker, string text)
        {
            return new BranchingDialogueGraphData
            {
                StartNodeId = "n1",
                Nodes = new List<BranchingDialogueNodeData>
                {
                    new BranchingDialogueNodeData { Id = "n1", Speaker = speaker, Text = text }
                }
            };
        }

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }

        #endregion
    }
}
