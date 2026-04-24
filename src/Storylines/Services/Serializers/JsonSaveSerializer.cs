using Newtonsoft.Json;
using Storylines.Services.Interfaces;
using Storylines.Models;

namespace Storylines.Services.Serializers
{
    public class JsonSaveSerializer : ISaveSerializer
    {
        public string Serialize(ProjectData projectData)
        {
            return JsonConvert.SerializeObject(projectData, Formatting.Indented);
        }

        public ProjectData Deserialize(string content)
        {
            return JsonConvert.DeserializeObject<ProjectData>(content);
        }

        public bool CanDeserialize(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            var trimmed = content.TrimStart();
            return trimmed.StartsWith("{");
        }
    }
}
