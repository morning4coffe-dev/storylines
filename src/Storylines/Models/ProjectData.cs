using Newtonsoft.Json;

namespace Storylines.Models;

public record class ProjectData
{
    [JsonProperty("format")]
    public string Format { get; set; } = "json-v1";

    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("lastOpenedChapter")]
    public int LastOpenedChapter { get; set; }

    [JsonProperty("chapters")]
    public List<ChapterData> Chapters { get; set; } = new List<ChapterData>();

    [JsonProperty("characters")]
    public List<CharacterData> Characters { get; set; } = new List<CharacterData>();

    [JsonProperty("pinboardConnections", NullValueHandling = NullValueHandling.Ignore)]
    public List<PinboardConnectionData> PinboardConnections { get; set; }

    [JsonProperty("plotThreads", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> PlotThreads { get; set; }

}

public record class ChapterData
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("lastCaretPosition", NullValueHandling = NullValueHandling.Ignore)]
    public int? LastCaretPosition { get; set; }

    [JsonProperty("lastVerticalOffset", NullValueHandling = NullValueHandling.Ignore)]
    public double? LastVerticalOffset { get; set; }

    [JsonProperty("notes")]
    public string Notes { get; set; }

    [JsonProperty("synopsis", NullValueHandling = NullValueHandling.Ignore)]
    public string Synopsis { get; set; }

    [JsonProperty("wordCountGoal", NullValueHandling = NullValueHandling.Ignore)]
    public int? WordCountGoal { get; set; }

    [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Tags { get; set; }

    [JsonProperty("pinboardX", NullValueHandling = NullValueHandling.Ignore)]
    public double? PinboardX { get; set; }

    [JsonProperty("pinboardY", NullValueHandling = NullValueHandling.Ignore)]
    public double? PinboardY { get; set; }

    [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
    public string Status { get; set; }

    [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
    public string Location { get; set; }

    [JsonProperty("plotThreads", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> PlotThreads { get; set; }

}

public record class PinboardConnectionData
{
    [JsonProperty("from")]
    public int FromIndex { get; set; }

    [JsonProperty("to")]
    public int ToIndex { get; set; }

    [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
    public string Label { get; set; }
}

public record class CharacterData
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("pictureFileName")]
    public string PictureFileName { get; set; }

    [JsonProperty("role", NullValueHandling = NullValueHandling.Ignore)]
    public string Role { get; set; }

    [JsonProperty("age", NullValueHandling = NullValueHandling.Ignore)]
    public string Age { get; set; }

    [JsonProperty("appearance", NullValueHandling = NullValueHandling.Ignore)]
    public string Appearance { get; set; }

    [JsonProperty("traits", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Traits { get; set; }

    [JsonProperty("relationships", NullValueHandling = NullValueHandling.Ignore)]
    public List<CharacterRelationshipData> Relationships { get; set; }
}

public record class CharacterRelationshipData
{
    [JsonProperty("targetName")]
    public string TargetName { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}
