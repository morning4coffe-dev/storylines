using Storylines.Models;

namespace Storylines.Services.Interfaces
{
    public interface ISaveSerializer
    {
        string Serialize(ProjectData projectData);
        ProjectData Deserialize(string content);
        bool CanDeserialize(string content);
    }
}
