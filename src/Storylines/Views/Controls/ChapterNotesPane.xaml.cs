using Storylines.Views.Pages;

namespace Storylines.Views.Controls;

public sealed partial class ChapterNotesPane : UserControl
{
    private readonly WindowContext _windowContext;
    private readonly ChapterNotesPaneViewModel _viewModel;

    private MainPage CurrentMainPage => _windowContext?.MainPage;

    public ChapterNotesPaneViewModel ViewModel => _viewModel;

    public ChapterNotesPane()
    {
        InitializeComponent();
        _windowContext = App.GetService<WindowContext>();
        _viewModel = App.GetService<ChapterNotesPaneViewModel>();
        DataContext = _viewModel;
    }

    public void LoadNotes()
    {
        _viewModel.LoadNotes();
    }

    private void OnCollapseButton_Click(object sender, RoutedEventArgs e)
    {
        CurrentMainPage?.ToggleNotesPane(false);
    }
}
