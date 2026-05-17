
namespace Storylines.Services;

public static class TelemetryEventPropertyBuilder
{
    public static Dictionary<string, string> Build(IEnumerable<KeyValuePair<string, string>> baseline, IEnumerable<KeyValuePair<string, string>>? specific = null)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddRange(properties, baseline);
        AddRange(properties, specific);

        return properties;
    }

    public static IEnumerable<KeyValuePair<string, string>> Create(params (string? Key, string? Value)[]? properties)
    {
        if (properties is null)
            return Enumerable.Empty<KeyValuePair<string, string>>();

        return CreateIterator(properties);
    }

    private static void AddRange(IDictionary<string, string> destination, IEnumerable<KeyValuePair<string, string>>? properties)
    {
        foreach (var property in properties ?? Enumerable.Empty<KeyValuePair<string, string>>())
        {
            if (string.IsNullOrWhiteSpace(property.Key) || string.IsNullOrWhiteSpace(property.Value))
                continue;

            destination[property.Key.Trim()] = property.Value.Trim();
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> CreateIterator(IEnumerable<(string? Key, string? Value)> properties)
    {
        foreach (var property in properties)
        {
            if (string.IsNullOrWhiteSpace(property.Key) || string.IsNullOrWhiteSpace(property.Value))
                continue;

            yield return new KeyValuePair<string, string>(property.Key.Trim(), property.Value.Trim());
        }
    }
}