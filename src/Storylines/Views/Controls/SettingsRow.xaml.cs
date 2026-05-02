using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Storylines.Views.Controls
{
    /// <summary>
    /// Single-row layout primitive used to build settings pages from a consistent template:
    /// optional leading glyph, header, description, and a trailing action region (toggle, combo,
    /// button) supplied by the consumer. Cuts boilerplate from the per-page settings XAML.
    /// </summary>
    public sealed partial class SettingsRow : UserControl
    {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsRow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsRow), new PropertyMetadata(string.Empty, OnDescriptionChanged));

        public static readonly DependencyProperty DescriptionVisibilityProperty =
            DependencyProperty.Register(nameof(DescriptionVisibility), typeof(Visibility), typeof(SettingsRow), new PropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(SettingsRow), new PropertyMetadata(string.Empty, OnGlyphChanged));

        public static readonly DependencyProperty GlyphVisibilityProperty =
            DependencyProperty.Register(nameof(GlyphVisibility), typeof(Visibility), typeof(SettingsRow), new PropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty ActionContentProperty =
            DependencyProperty.Register(nameof(ActionContent), typeof(object), typeof(SettingsRow), new PropertyMetadata(null));

        public SettingsRow()
        {
            InitializeComponent();
        }

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public Visibility DescriptionVisibility
        {
            get => (Visibility)GetValue(DescriptionVisibilityProperty);
            private set => SetValue(DescriptionVisibilityProperty, value);
        }

        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        public Visibility GlyphVisibility
        {
            get => (Visibility)GetValue(GlyphVisibilityProperty);
            private set => SetValue(GlyphVisibilityProperty, value);
        }

        public object ActionContent
        {
            get => GetValue(ActionContentProperty);
            set => SetValue(ActionContentProperty, value);
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsRow row)
            {
                row.DescriptionVisibility = string.IsNullOrWhiteSpace(row.Description)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsRow row)
            {
                row.GlyphVisibility = string.IsNullOrWhiteSpace(row.Glyph)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }
    }
}
