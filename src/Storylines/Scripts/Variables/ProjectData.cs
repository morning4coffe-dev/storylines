using Newtonsoft.Json;
using System.Collections.Generic;

namespace Storylines.Scripts.Variables
{
    public class ProjectData
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
    }

    public class ChapterData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        [JsonProperty("synopsis", NullValueHandling = NullValueHandling.Ignore)]
        public string Synopsis { get; set; }

        [JsonProperty("wordCountGoal", NullValueHandling = NullValueHandling.Ignore)]
        public int? WordCountGoal { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }
    }

    public class CharacterData
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
    }
}
