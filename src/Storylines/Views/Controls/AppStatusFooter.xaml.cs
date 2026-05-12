
namespace Storylines.Views.Controls;

/// <summary>
/// Persistent app footer surfacing word count, daily goal progress, current editor mode,
/// speech state and save status. Designed to be hosted by the shell and bound to
/// <c>AppStatusFooterViewModel</c> (introduced as Phase 6 features wire in). Properties are
/// dependency properties so consumers can drive the footer from XAML without a code-behind
/// dependency chain.
/// </summary>
public sealed partial class AppStatusFooter : UserControl
{
    public static readonly DependencyProperty WordCountLabelProperty =
        DependencyProperty.Register(nameof(WordCountLabel), typeof(string), typeof(AppStatusFooter), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GoalLabelProperty =
        DependencyProperty.Register(nameof(GoalLabel), typeof(string), typeof(AppStatusFooter), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GoalProgressProperty =
        DependencyProperty.Register(nameof(GoalProgress), typeof(double), typeof(AppStatusFooter), new PropertyMetadata(0d));

    public static readonly DependencyProperty GoalVisibilityProperty =
        DependencyProperty.Register(nameof(GoalVisibility), typeof(Visibility), typeof(AppStatusFooter), new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty ModeTextProperty =
        DependencyProperty.Register(nameof(ModeText), typeof(string), typeof(AppStatusFooter), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ModeBadgeVisibilityProperty =
        DependencyProperty.Register(nameof(ModeBadgeVisibility), typeof(Visibility), typeof(AppStatusFooter), new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty SpeechGlyphProperty =
        DependencyProperty.Register(nameof(SpeechGlyph), typeof(string), typeof(AppStatusFooter), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SpeechTooltipProperty =
        DependencyProperty.Register(nameof(SpeechTooltip), typeof(string), typeof(AppStatusFooter), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SpeechIconVisibilityProperty =
        DependencyProperty.Register(nameof(SpeechIconVisibility), typeof(Visibility), typeof(AppStatusFooter), new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty SaveIndicatorProperty =
        DependencyProperty.Register(nameof(SaveIndicator), typeof(string), typeof(AppStatusFooter), new PropertyMetadata(string.Empty));

    public AppStatusFooter()
    {
        InitializeComponent();
    }

    public string WordCountLabel { get => (string)GetValue(WordCountLabelProperty); set => SetValue(WordCountLabelProperty, value); }
    public string GoalLabel { get => (string)GetValue(GoalLabelProperty); set => SetValue(GoalLabelProperty, value); }
    public double GoalProgress { get => (double)GetValue(GoalProgressProperty); set => SetValue(GoalProgressProperty, value); }
    public Visibility GoalVisibility { get => (Visibility)GetValue(GoalVisibilityProperty); set => SetValue(GoalVisibilityProperty, value); }
    public string ModeText { get => (string)GetValue(ModeTextProperty); set => SetValue(ModeTextProperty, value); }
    public Visibility ModeBadgeVisibility { get => (Visibility)GetValue(ModeBadgeVisibilityProperty); set => SetValue(ModeBadgeVisibilityProperty, value); }
    public string SpeechGlyph { get => (string)GetValue(SpeechGlyphProperty); set => SetValue(SpeechGlyphProperty, value); }
    public string SpeechTooltip { get => (string)GetValue(SpeechTooltipProperty); set => SetValue(SpeechTooltipProperty, value); }
    public Visibility SpeechIconVisibility { get => (Visibility)GetValue(SpeechIconVisibilityProperty); set => SetValue(SpeechIconVisibilityProperty, value); }
    public string SaveIndicator { get => (string)GetValue(SaveIndicatorProperty); set => SetValue(SaveIndicatorProperty, value); }
}
