using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Views.Controls
{
    /// <summary>
    /// Reusable empty-state UserControl: large glyph, headline, description and an optional CTA.
    /// Drop into any list/grid host so empty / loading / error states share a single visual shape
    /// across chapters, characters, pinboard, branching dialogue, and recent projects lists.
    /// </summary>
    public sealed partial class EmptyStateControl : UserControl
    {
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(""));

        public static readonly DependencyProperty HeadlineProperty =
            DependencyProperty.Register(nameof(Headline), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(string.Empty, OnActionChanged));

        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(EmptyStateControl), new PropertyMetadata(null, OnActionChanged));

        public static readonly DependencyProperty ActionVisibilityProperty =
            DependencyProperty.Register(nameof(ActionVisibility), typeof(Visibility), typeof(EmptyStateControl), new PropertyMetadata(Visibility.Collapsed));

        public EmptyStateControl()
        {
            InitializeComponent();
        }

        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        public string Headline
        {
            get => (string)GetValue(HeadlineProperty);
            set => SetValue(HeadlineProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public ICommand ActionCommand
        {
            get => (ICommand)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        public Visibility ActionVisibility
        {
            get => (Visibility)GetValue(ActionVisibilityProperty);
            private set => SetValue(ActionVisibilityProperty, value);
        }

        private static void OnActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmptyStateControl control)
            {
                var hasAction = control.ActionCommand != null && !string.IsNullOrWhiteSpace(control.ActionText);
                control.ActionVisibility = hasAction ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
