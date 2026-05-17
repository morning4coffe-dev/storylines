using Microsoft.UI.Xaml.Markup;

namespace Storylines.Views.Controls;

/// <summary>
/// Shared host layout for settings pages: scroll viewer, responsive outer margins,
/// and a centered content column with consistent spacing.
/// </summary>
[ContentProperty(Name = nameof(PageContent))]
public sealed partial class SettingsPageLayout : UserControl
{
    public static readonly DependencyProperty PageContentProperty =
        DependencyProperty.Register(
            nameof(PageContent),
            typeof(object),
            typeof(SettingsPageLayout),
            new PropertyMetadata(null));

    public SettingsPageLayout()
    {
        InitializeComponent();
    }

    public object PageContent
    {
        get => GetValue(PageContentProperty);
        set => SetValue(PageContentProperty, value);
    }
}
