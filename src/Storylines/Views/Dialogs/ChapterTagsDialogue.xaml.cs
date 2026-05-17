using CommunityToolkit.WinUI.Controls;

namespace Storylines.Views.Dialogs;

public sealed partial class ChapterTagsDialogue : AppContentDialog
{
    private readonly ChapterTagsViewModel _viewModel;

    public ChapterTagsViewModel ViewModel => _viewModel;

    public ChapterTagsDialogue(Chapter chapter)
    {
        _viewModel = new ChapterTagsViewModel(App.GetService<ProjectState>(), chapter);
        InitializeComponent();
        CloseOnOutsideTap = true;
    }

    public static void Open(Chapter chapter)
    {
        _ = OpenAsync(chapter);
    }

    public static Task<ContentDialogResult> OpenAsync(Chapter chapter)
    {
        return App.GetService<IDialogService>().ShowAsync(new ChapterTagsDialogue(chapter));
    }

    private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        _viewModel.Initialize();

        chapterNameText.Text = _viewModel.ChapterName;

        tagsTokenBox.Items.Clear();
        foreach (var tag in _viewModel.CurrentTags)
            tagsTokenBox.Items.Add(tag);
    }

    private void OnSuggestionPill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string tag)
        {
            _viewModel.AddSuggestionCommand.Execute(tag);
            if (!GetCurrentTags().Contains(tag, StringComparer.CurrentCultureIgnoreCase))
                tagsTokenBox.Items.Add(tag);
        }
    }

    private void OnTagsTokenBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            sender.ItemsSource = _viewModel.GetAutoSuggestions(sender.Text.Trim()).ToList();
    }

    private void OnTokenItem_Added(TokenizingTextBox sender, object args)
    {
        SyncTagsToViewModel();
        _viewModel.RefreshSuggestions();
        _viewModel.RefreshSavedPresets();
    }

    private void OnTokenItem_Removing(TokenizingTextBox sender, TokenItemRemovingEventArgs args)
    {
        SyncTagsToViewModel();
        _viewModel.RefreshSuggestions();
    }

    private void OnRemovePreset_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string preset)
            _viewModel.RemovePresetCommand.Execute(preset);
    }

    private void OnSaveButton_Click(object sender, RoutedEventArgs e)
    {
        SyncTagsToViewModel();
        _viewModel.SaveCommand.Execute(null);
        Hide();
    }

    private void OnCancelButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void SyncTagsToViewModel()
    {
        _viewModel.CurrentTags = GetCurrentTags();
    }

    private List<string> GetCurrentTags()
    {
        var list = new List<string>();
        foreach (var item in tagsTokenBox.Items)
            list.Add(item?.ToString() ?? string.Empty);
        return list;
    }
}
