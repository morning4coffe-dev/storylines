namespace Storylines.Services;

public class PreferencesService : IPreferencesService
{
    private readonly ApplicationDataContainer _localSettings;

    public PreferencesService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    public T Get<T>(string key, T defaultValue = default)
    {
        if (_localSettings.Values.TryGetValue(key, out object value))
        {
            if (value is T tValue)
                return tValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _localSettings.Values[key] = value;
    }

    public bool Contains(string key)
    {
        return _localSettings.Values.ContainsKey(key);
    }

    public void Remove(string key)
    {
        _localSettings.Values.Remove(key);
    }
}
