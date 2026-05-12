
namespace Storylines.Views.Dialogs;

public sealed partial class ProjectStatsDialogue : AppContentDialog
{
    private readonly ProjectStatsViewModel _viewModel;

    public ProjectStatsViewModel ViewModel => _viewModel;

    public ProjectStatsDialogue()
    {
        _viewModel = new ProjectStatsViewModel(
            App.GetService<ProjectState>());

        InitializeComponent();
        CloseOnOutsideTap = true;
    }

    public static void Open(bool fromDownBar)
    {
        _ = OpenAsync(fromDownBar);
    }

    public static async System.Threading.Tasks.Task<ContentDialogResult> OpenAsync(bool fromDownBar)
    {
        var dialog = new ProjectStatsDialogue();
        App.TryGetService<ITelemetryService>()?.TrackProjectStatsOpened(fromDownBar);
        var showTask = App.GetService<IDialogService>().ShowAsync(dialog);
        dialog.LoadStats();
        return await showTask;
    }

    private void LoadStats()
    {
        var windowContext = App.GetService<WindowContext>();
        var textBox = windowContext.ChapterText?.textBox;
        if (textBox is null) return;

        textBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string txt);
        _viewModel.ComputeStats(txt);

        // Bind computed text to UI
        storyRun.Text = _viewModel.StoryStatsText;
        charactersRun.Text = _viewModel.CharactersStatsText;
        chaptersRun.Text = _viewModel.ChaptersStatsText;
        textRun.Text = _viewModel.CurrentChapterStatsText;

        if (!string.IsNullOrEmpty(_viewModel.WordDistributionText))
            wordDistributionTextBox.Text = _viewModel.WordDistributionText;

        PopulateChapterBars();
    }

    private void PopulateChapterBars()
    {
        chapterChartCanvas.Children.Clear();

        var stats = _viewModel.ChapterWordStats;
        if (stats is null || stats.Count == 0) return;

        const double barHeight = 22;
        const double barSpacing = 4;
        const double labelWidth = 140;
        const double chartWidth = 300;
        double y = 0;

        var accentBrush = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]);

        foreach (var stat in stats)
        {
            double barWidth = stat.NormalizedWidth * chartWidth;

            var label = new TextBlock
            {
                Text = stat.Name,
                FontSize = 11,
                Opacity = 0.8,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = labelWidth - 8,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, y + 2);
            chapterChartCanvas.Children.Add(label);

            var bar = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Width = barWidth,
                Height = barHeight - 6,
                RadiusX = 3,
                RadiusY = 3,
                Fill = accentBrush,
                Opacity = 0.7
            };
            Canvas.SetLeft(bar, labelWidth);
            Canvas.SetTop(bar, y + 3);
            chapterChartCanvas.Children.Add(bar);

            var countLabel = new TextBlock
            {
                Text = $"{stat.WordCount}w",
                FontSize = 10,
                Opacity = 0.55,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(countLabel, labelWidth + barWidth + 6);
            Canvas.SetTop(countLabel, y + 3);
            chapterChartCanvas.Children.Add(countLabel);

            y += barHeight + barSpacing;
        }

        chapterChartCanvas.Width = labelWidth + chartWidth + 60;
        chapterChartCanvas.Height = y;
    }

    private void OnCloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
