
namespace Storylines.Services.Interfaces;

public interface IPreferencesService
{
    T Get<T>(string key, T defaultValue = default);
    void Set<T>(string key, T value);
    bool Contains(string key);
    void Remove(string key);
}
