using Storylines.Scripts.Variables;

namespace Storylines.Scripts.Services.Interfaces
{
    public interface ISaveSerializer
    {
        string Serialize(ProjectData projectData);
        ProjectData Deserialize(string content);
        bool CanDeserialize(string content);
    }
}
