using Storylines.Scripts.Services.Interfaces;
using Storylines.Scripts.Services.Serializers;

namespace Storylines.Scripts.Services
{
    public static class ServiceLocator
    {
        public static ILogger Logger { get; private set; }
        public static IFileService FileService { get; private set; }
        public static INotificationService NotificationService { get; private set; }
        public static ISaveSerializer JsonSerializer { get; private set; }
        public static ISaveSerializer LegacySerializer { get; private set; }

        public static void Initialize()
        {
            Logger = new DebugLogger();
            FileService = new Services.FileService();
            JsonSerializer = new JsonSaveSerializer();
            LegacySerializer = new LegacySrlSerializer();
        }
    }
}
