namespace Storylines.Views.Dialogs;

public sealed partial class GlobalSearchDialogue : AppContentDialog
{
    private readonly GlobalSearchViewModel _viewModel;
    private readonly string _initialQuery;

    public GlobalSearchViewModel ViewModel => _viewModel;

    public GlobalSearchDialogue(string initialQuery = null)
    {
        _viewModel = App.GetService<GlobalSearchViewModel>();
        _viewModel.CloseRequested += () => Hide();
        _initialQuery = initialQuery?.Trim();

        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_initialQuery))
            {
                searchBox.Text = _initialQuery;
                searchBox.SelectionStart = searchBox.Text.Length;
                _viewModel.SearchQuery = _initialQuery;
            }

            searchBox.Focus(FocusState.Programmatic);
            _viewModel.RefreshResults();
        };
    }

    public static async Task OpenAsync(string initialQuery = null)
    {
        try
        {
            var dialog = new GlobalSearchDialogue(initialQuery);
            await App.GetService<IDialogService>().ShowAsync(dialog);
        }
        catch (Exception ex)
        {
            App.TryGetService<ILogger>()?.Warning($"Failed to open global search dialog: {ex.Message}");
        }
    }

    private void OnSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchQuery = searchBox.Text;
    }

    private void OnResultItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GlobalSearchResultItem result)
            _viewModel.ExecuteResultCommand.Execute(result);
    }
}
