
namespace Storylines.Services.Interfaces;

public interface ITelemetryProvider
{
    string ProviderName { get; }

    Task InitializeAsync();

    void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties);

    void TrackError(Exception exception, IReadOnlyDictionary<string, string> properties, string attachmentText = null, string attachmentFileName = null);
}