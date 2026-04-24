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
}
