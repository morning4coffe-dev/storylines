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