using Microsoft.UI.Xaml.Markup;

namespace Storylines.Views.Controls;

/// <summary>
/// Reusable settings card control implementing the Fluent Design settings row pattern.
/// Displays an optional icon, header/description text, and a trailing action area.
/// Replaces the hand-rolled Grid+Icon+Label+Action boilerplate on settings pages.
/// </summary>
[ContentProperty(Name = nameof(ActionContent))]
public sealed partial class SettingsCard : UserControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(SettingsCard), new PropertyMetadata(null));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(object), typeof(SettingsCard), new PropertyMetadata(null, OnDescriptionChanged));

    public static readonly DependencyProperty DescriptionVisibilityProperty =
        DependencyProperty.Register(nameof(DescriptionVisibility), typeof(Visibility), typeof(SettingsCard), new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(SettingsCard), new PropertyMetadata(string.Empty, OnGlyphChanged));

    public static readonly DependencyProperty GlyphVisibilityProperty =
        DependencyProperty.Register(nameof(GlyphVisibility), typeof(Visibility), typeof(SettingsCard), new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty ActionContentProperty =
        DependencyProperty.Register(nameof(ActionContent), typeof(object), typeof(SettingsCard), new PropertyMetadata(null));

    public SettingsCard()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Primary label for the setting. Accepts a string or a UIElement (e.g. a TextBlock with x:Uid).
    /// </summary>
    public object Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Secondary description text. Auto-hidden when null or empty. Accepts a string or UIElement.
    /// </summary>
    public object Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public Visibility DescriptionVisibility
    {
        get => (Visibility)GetValue(DescriptionVisibilityProperty);
        private set => SetValue(DescriptionVisibilityProperty, value);
    }

    /// <summary>
    /// Segoe Fluent Icons glyph for the leading icon. Auto-hidden when empty.
    /// </summary>
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

    /// <summary>
    /// The interactive control displayed on the trailing edge (toggle, combo box, button, etc.).
    /// This is the XAML content property — child elements are placed here by default.
    /// </summary>
    public object ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SettingsCard card)
        {
            card.DescriptionVisibility = IsContentEmpty(e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SettingsCard card)
        {
            card.GlyphVisibility = string.IsNullOrWhiteSpace(e.NewValue as string) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static bool IsContentEmpty(object value) =>
        value is null || (value is string s && string.IsNullOrWhiteSpace(s));
}
