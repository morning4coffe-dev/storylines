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
    }

    public class CharacterData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("pictureFileName")]
        public string PictureFileName { get; set; }
    }
}
