using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using Storylines.Views.Controls;

namespace Storylines.Views.Dialogs
{
    public abstract class AppShellDialog : AppContentDialog
    {
        private readonly Grid _rootGrid;
        private readonly StackPanel _stackPanel;
        private readonly TextBlock _titleTextBlock;
        private readonly TextBlock _subtitleTextBlock;
        private readonly ContentPresenter _headerPresenter;
        private readonly ContentPresenter _bodyPresenter;
        private readonly ContentPresenter _footerPresenter;
        private readonly Grid _defaultFooterGrid;
        private readonly SymbolButton _primaryActionButton;
        private readonly SymbolButton _secondaryActionButton;

        public static readonly DependencyProperty DialogTitleProperty = DependencyProperty.Register(
            nameof(DialogTitle),
            typeof(string),
            typeof(AppShellDialog),
            new PropertyMetadata(string.Empty, OnShellPropertyChanged));

        public static readonly DependencyProperty DialogSubtitleProperty = DependencyProperty.Register(
            nameof(DialogSubtitle),
            typeof(string),
            typeof(AppShellDialog),
            new PropertyMetadata(string.Empty, OnShellPropertyChanged));

        public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register(
            nameof(HeaderContent),
            typeof(object),
            typeof(AppShellDialog),
            new PropertyMetadata(null, OnShellPropertyChanged));

        public static readonly DependencyProperty DialogBodyProperty = DependencyProperty.Register(
            nameof(DialogBody),
            typeof(object),
            typeof(AppShellDialog),
            new PropertyMetadata(null, OnShellPropertyChanged));

        public static readonly DependencyProperty FooterContentProperty = DependencyProperty.Register(
            nameof(FooterContent),
            typeof(object),
            typeof(AppShellDialog),
            new PropertyMetadata(null, OnShellPropertyChanged));

        public static readonly DependencyProperty DialogPaddingProperty = DependencyProperty.Register(
            nameof(DialogPadding),
            typeof(Thickness),
            typeof(AppShellDialog),
            new PropertyMetadata(new Thickness(4), OnShellPropertyChanged));

        public static readonly DependencyProperty DialogSpacingProperty = DependencyProperty.Register(
            nameof(DialogSpacing),
            typeof(double),
            typeof(AppShellDialog),
            new PropertyMetadata(16d, OnShellPropertyChanged));

        public static readonly DependencyProperty DialogMinWidthProperty = DependencyProperty.Register(
            nameof(DialogMinWidth),
            typeof(double),
            typeof(AppShellDialog),
            new PropertyMetadata(340d, OnShellPropertyChanged));

        public static readonly DependencyProperty DialogMaxWidthProperty = DependencyProperty.Register(
            nameof(DialogMaxWidth),
            typeof(double),
            typeof(AppShellDialog),
            new PropertyMetadata(double.PositiveInfinity, OnShellPropertyChanged));

        public static readonly DependencyProperty UseDefaultFooterProperty = DependencyProperty.Register(
            nameof(UseDefaultFooter),
            typeof(bool),
            typeof(AppShellDialog),
            new PropertyMetadata(true, OnShellPropertyChanged));

        public static readonly DependencyProperty PrimaryActionTextProperty = DependencyProperty.Register(
            nameof(PrimaryActionText),
            typeof(string),
            typeof(AppShellDialog),
            new PropertyMetadata(string.Empty, OnShellPropertyChanged));

        public static readonly DependencyProperty PrimaryActionGlyphProperty = DependencyProperty.Register(
            nameof(PrimaryActionGlyph),
            typeof(string),
            typeof(AppShellDialog),
            new PropertyMetadata(string.Empty, OnShellPropertyChanged));

        public static readonly DependencyProperty IsPrimaryActionEnabledProperty = DependencyProperty.Register(
            nameof(IsPrimaryActionEnabled),
            typeof(bool),
            typeof(AppShellDialog),
            new PropertyMetadata(true, OnShellPropertyChanged));

        public static readonly DependencyProperty PrimaryActionVisibilityProperty = DependencyProperty.Register(
            nameof(PrimaryActionVisibility),
            typeof(Visibility),
            typeof(AppShellDialog),
            new PropertyMetadata(Visibility.Visible, OnShellPropertyChanged));

        public static readonly DependencyProperty SecondaryActionTextProperty = DependencyProperty.Register(
            nameof(SecondaryActionText),
            typeof(string),
            typeof(AppShellDialog),
            new PropertyMetadata(string.Empty, OnShellPropertyChanged));

        public static readonly DependencyProperty SecondaryActionGlyphProperty = DependencyProperty.Register(
            nameof(SecondaryActionGlyph),
            typeof(string),
            typeof(AppShellDialog),
            new PropertyMetadata(string.Empty, OnShellPropertyChanged));

        public static readonly DependencyProperty IsSecondaryActionEnabledProperty = DependencyProperty.Register(
            nameof(IsSecondaryActionEnabled),
            typeof(bool),
            typeof(AppShellDialog),
            new PropertyMetadata(true, OnShellPropertyChanged));

        public static readonly DependencyProperty SecondaryActionVisibilityProperty = DependencyProperty.Register(
            nameof(SecondaryActionVisibility),
            typeof(Visibility),
            typeof(AppShellDialog),
            new PropertyMetadata(Visibility.Visible, OnShellPropertyChanged));

        public static readonly DependencyProperty UseCancelSecondaryActionProperty = DependencyProperty.Register(
            nameof(UseCancelSecondaryAction),
            typeof(bool),
            typeof(AppShellDialog),
            new PropertyMetadata(true, OnShellPropertyChanged));

        public AppShellDialog()
        {
            _rootGrid = new Grid();

            _stackPanel = new StackPanel();
            _rootGrid.Children.Add(_stackPanel);

            var titleSection = new StackPanel
            {
                Spacing = 6,
            };
            _stackPanel.Children.Add(titleSection);

            _titleTextBlock = new TextBlock();
            if (Application.Current.Resources.TryGetValue("DialogueWindowHeaderStyle", out var headerStyle)
                && headerStyle is Style titleStyle)
            {
                _titleTextBlock.Style = titleStyle;
            }

            _subtitleTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                Opacity = 0.8,
            };

            titleSection.Children.Add(_titleTextBlock);
            titleSection.Children.Add(_subtitleTextBlock);

            _headerPresenter = new ContentPresenter();
            _bodyPresenter = new ContentPresenter();
            _footerPresenter = new ContentPresenter();

            _stackPanel.Children.Add(_headerPresenter);
            _stackPanel.Children.Add(_bodyPresenter);
            _stackPanel.Children.Add(_footerPresenter);

            _defaultFooterGrid = new Grid
            {
                Margin = new Thickness(0, 4, 0, 0),
            };
            _defaultFooterGrid.ColumnDefinitions.Add(new ColumnDefinition());
            _defaultFooterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            _defaultFooterGrid.ColumnDefinitions.Add(new ColumnDefinition());

            _primaryActionButton = new SymbolButton
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsPrimary = true,
            };
            _primaryActionButton.Click += PrimaryActionButton_Click;

            _secondaryActionButton = new SymbolButton
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            _secondaryActionButton.Click += SecondaryActionButton_Click;
            Grid.SetColumn(_secondaryActionButton, 2);

            _defaultFooterGrid.Children.Add(_primaryActionButton);
            _defaultFooterGrid.Children.Add(_secondaryActionButton);
            _stackPanel.Children.Add(_defaultFooterGrid);

            Content = _rootGrid;
            ApplyShellVisuals();
            KeyDown += OnShellDialogKeyDown;
        }

        public string DialogTitle
        {
            get => (string)GetValue(DialogTitleProperty);
            set => SetValue(DialogTitleProperty, value);
        }

        public string DialogSubtitle
        {
            get => (string)GetValue(DialogSubtitleProperty);
            set => SetValue(DialogSubtitleProperty, value);
        }

        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        public object DialogBody
        {
            get => GetValue(DialogBodyProperty);
            set => SetValue(DialogBodyProperty, value);
        }

        public object FooterContent
        {
            get => GetValue(FooterContentProperty);
            set => SetValue(FooterContentProperty, value);
        }

        public Thickness DialogPadding
        {
            get => (Thickness)GetValue(DialogPaddingProperty);
            set => SetValue(DialogPaddingProperty, value);
        }

        public double DialogSpacing
        {
            get => (double)GetValue(DialogSpacingProperty);
            set => SetValue(DialogSpacingProperty, value);
        }

        public double DialogMinWidth
        {
            get => (double)GetValue(DialogMinWidthProperty);
            set => SetValue(DialogMinWidthProperty, value);
        }

        public double DialogMaxWidth
        {
            get => (double)GetValue(DialogMaxWidthProperty);
            set => SetValue(DialogMaxWidthProperty, value);
        }

        public bool UseDefaultFooter
        {
            get => (bool)GetValue(UseDefaultFooterProperty);
            set => SetValue(UseDefaultFooterProperty, value);
        }

        public string PrimaryActionText
        {
            get => (string)GetValue(PrimaryActionTextProperty);
            set => SetValue(PrimaryActionTextProperty, value);
        }

        public string PrimaryActionGlyph
        {
            get => (string)GetValue(PrimaryActionGlyphProperty);
            set => SetValue(PrimaryActionGlyphProperty, value);
        }

        public bool IsPrimaryActionEnabled
        {
            get => (bool)GetValue(IsPrimaryActionEnabledProperty);
            set => SetValue(IsPrimaryActionEnabledProperty, value);
        }

        public Visibility PrimaryActionVisibility
        {
            get => (Visibility)GetValue(PrimaryActionVisibilityProperty);
            set => SetValue(PrimaryActionVisibilityProperty, value);
        }

        public string SecondaryActionText
        {
            get => (string)GetValue(SecondaryActionTextProperty);
            set => SetValue(SecondaryActionTextProperty, value);
        }

        public string SecondaryActionGlyph
        {
            get => (string)GetValue(SecondaryActionGlyphProperty);
            set => SetValue(SecondaryActionGlyphProperty, value);
        }

        public bool IsSecondaryActionEnabled
        {
            get => (bool)GetValue(IsSecondaryActionEnabledProperty);
            set => SetValue(IsSecondaryActionEnabledProperty, value);
        }

        public Visibility SecondaryActionVisibility
        {
            get => (Visibility)GetValue(SecondaryActionVisibilityProperty);
            set => SetValue(SecondaryActionVisibilityProperty, value);
        }

        public bool UseCancelSecondaryAction
        {
            get => (bool)GetValue(UseCancelSecondaryActionProperty);
            set => SetValue(UseCancelSecondaryActionProperty, value);
        }

        protected virtual bool ShouldExecutePrimaryActionOnEnter(KeyRoutedEventArgs args)
            => args.Key == Windows.System.VirtualKey.Enter
                && IsPrimaryActionEnabled
                && PrimaryActionVisibility == Visibility.Visible
                && !HasOpenTransientElements;

        protected abstract Task<bool> ExecutePrimaryActionAsync();

        protected virtual Task<bool> ExecuteSecondaryActionAsync() => Task.FromResult(true);

        private static void OnShellPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is AppShellDialog dialog)
                dialog.ApplyShellVisuals();
        }

        private async void OnShellDialogKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!ShouldExecutePrimaryActionOnEnter(e))
                return;

            e.Handled = true;
            await InvokePrimaryActionAsync();
        }

        private async void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
        {
            await InvokePrimaryActionAsync();
        }

        private async void SecondaryActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (await ExecuteSecondaryActionAsync())
                Hide();
        }

        private async Task InvokePrimaryActionAsync()
        {
            if (await ExecutePrimaryActionAsync())
                Hide();
        }

        private void ApplyShellVisuals()
        {
            _rootGrid.MinWidth = DialogMinWidth;
            _rootGrid.MaxWidth = DialogMaxWidth;

            _stackPanel.Spacing = DialogSpacing;
            _stackPanel.Margin = DialogPadding;

            _titleTextBlock.Text = DialogTitle ?? string.Empty;
            _titleTextBlock.Visibility = string.IsNullOrWhiteSpace(DialogTitle)
                ? Visibility.Collapsed
                : Visibility.Visible;

            _subtitleTextBlock.Text = DialogSubtitle ?? string.Empty;
            _subtitleTextBlock.Visibility = string.IsNullOrWhiteSpace(DialogSubtitle)
                ? Visibility.Collapsed
                : Visibility.Visible;

            _headerPresenter.Content = HeaderContent;
            _headerPresenter.Visibility = HeaderContent is null
                ? Visibility.Collapsed
                : Visibility.Visible;

            _bodyPresenter.Content = DialogBody;

            _footerPresenter.Content = FooterContent;
            _footerPresenter.Visibility = FooterContent is null
                ? Visibility.Collapsed
                : Visibility.Visible;

            _defaultFooterGrid.Visibility = UseDefaultFooter
                ? Visibility.Visible
                : Visibility.Collapsed;

            _primaryActionButton.Text = PrimaryActionText;
            _primaryActionButton.Glyph = PrimaryActionGlyph;
            _primaryActionButton.IsEnabled = IsPrimaryActionEnabled;
            _primaryActionButton.Visibility = PrimaryActionVisibility;

            _secondaryActionButton.IsCancel = UseCancelSecondaryAction;
            _secondaryActionButton.Text = SecondaryActionText;
            _secondaryActionButton.Glyph = SecondaryActionGlyph;
            _secondaryActionButton.IsEnabled = IsSecondaryActionEnabled;
            _secondaryActionButton.Visibility = SecondaryActionVisibility;
        }
    }
}
