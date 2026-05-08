using Windows.ApplicationModel.Resources;

namespace Storylines.Resources
{
    public static class SpeechHubStrings
    {
        private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();

        public static string DictationPermissionDeniedStatus => _resources.GetString("dictationPermissionDeniedStatus");
        public static string DictationPermissionDeniedTitle => _resources.GetString("dictationPermissionDeniedTitle");
        public static string DictationPermissionDeniedMessage => _resources.GetString("dictationPermissionDeniedMessage");
        public static string DictationUnsupportedStatus => _resources.GetString("dictationUnsupportedStatus");
        public static string DictationErrorStatus => _resources.GetString("dictationErrorStatus");
        public static string DictationListeningStatus => _resources.GetString("dictationListeningStatus");
    }
}