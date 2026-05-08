namespace Windows.UI
{
    public struct Color
    {
        public byte A { get; set; }

        public byte R { get; set; }

        public byte G { get; set; }

        public byte B { get; set; }

        public static Color FromArgb(byte a, byte r, byte g, byte b)
            => new Color
            {
                A = a,
                R = r,
                G = g,
                B = b,
            };
    }
}

namespace Microsoft.UI.Xaml.Controls
{
    public enum InfoBarSeverity
    {
        Informational,
        Success,
        Warning,
        Error,
    }
}

namespace Microsoft.UI.Xaml
{
    public enum Visibility
    {
        Visible,
        Collapsed,
    }
}

namespace Windows.ApplicationModel.Resources
{
    public sealed class ResourceLoader
    {
        public static ResourceLoader GetForViewIndependentUse()
            => new ResourceLoader();

        public string GetString(string resource)
            => resource switch
            {
                "dictationPermissionDeniedStatus" => "Microphone access denied.",
                "dictationPermissionDeniedTitle" => "Microphone access denied",
                "dictationPermissionDeniedMessage" => "Grant microphone access in Windows Settings to use dictation.",
                "dictationUnsupportedStatus" => "Dictation is not available on this device.",
                "dictationErrorStatus" => "Dictation error.",
                "dictationListeningStatus" => "Listening…",
                _ => resource,
            };
    }
}