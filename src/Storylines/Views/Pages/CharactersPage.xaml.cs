using Microsoft.UI.Xaml.Media.Imaging;

namespace Storylines.Views.Pages;

public sealed partial class CharactersPage : Page
{
    private readonly WindowContext _windowContext;
    private string _pendingSelectedCharacterToken;

    public ObservableCollection<Character> FilteredCharacters => ViewModel.FilteredCharacters;
    public ObservableCollection<Character> Characters => App.GetService<ProjectState>().Characters;
    public CharactersPageViewModel ViewModel { get; }

    private bool selectionChanged = false;
    public bool unappliedChanges
    {
        get => ViewModel.UnappliedChanges;
        set => ViewModel.UnappliedChanges = value;
    }

    public CharactersPage()
    {
        _windowContext = App.GetService<WindowContext>();
        ViewModel = App.GetService<CharactersPageViewModel>();

        InitializeComponent();

        _windowContext.CharactersPage = this;
        _windowContext.AppView.page = AppView.Pages.Characters;

        TimeTravelCharacter.ClearUndoAndRedo();

        ViewModel.FilteredListRefreshed += OnFilteredListRefreshed;
        ViewModel.CharacterSelectedFromUndo += OnCharacterSelectedFromUndo;

        ViewModel.RefreshFilteredList();
    }

    private void OnFilteredListRefreshed(string selectedToken)
    {
        if (!string.IsNullOrWhiteSpace(selectedToken))
            listView.SelectedItem = FilteredCharacters.FirstOrDefault(c => c.Token == selectedToken);
    }

    private void OnCharacterSelectedFromUndo(string selectedToken)
    {
        if (FilteredCharacters.All(c => c.Token != selectedToken) && !string.IsNullOrWhiteSpace(characterSearchBox.Text))
            characterSearchBox.Text = string.Empty;

        ViewModel.RefreshFilteredList(selectedToken);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _pendingSelectedCharacterToken = e.Parameter as string;
        TrySelectPendingCharacter();
    }

    public void EnableEditMode(bool enable)
    {
        if (enable && listView.SelectedItem is not null)
        {
            ViewModel.SelectedCharacter = listView.SelectedItem as Character;
            ViewModel.EnterEditMode();
        }
        else
        {
            ViewModel.ExitEditMode();
        }

        if (!enable && !selectionChanged)
            if (DidSomethingChange())
            {
                ApplyChanges();
                Sort();
            }
    }

    public void ApplyChanges()
    {
        var character = listView.SelectedItem as Character;
        if (character is not null && ViewModel.SelectedCharacter is not null)
        {
            ViewModel.TraitsText = GetTraitsFromTokenBox();
            ViewModel.ApplyChanges();
            ViewModel.RefreshFilteredList(character.Token);
            UpdateDialogueInsights(character);
        }
    }

    public void CancelEdit()
    {
        ViewModel.CancelEdit();
        LoadTraitsIntoTokenBox(ViewModel.TraitsText);
        _picture = null;
        EnableEditMode(false);
    }

    public bool DidSomethingChange()
    {
        if (ViewModel.SelectedCharacter is null) return false;
        ViewModel.TraitsText = GetTraitsFromTokenBox();
        return ViewModel.DidSomethingChange();
    }

    #region Flyout
    private string characterItemFlyoutedToken;
    private void OpenFlyout(string token, bool enabled)
    {
        characterItemFlyoutedToken = token;

        addFlyout.IsEnabled = true;

        editFlyout.IsEnabled = enabled;
        removeFlyout.IsEnabled = enabled;
    }

    private void OnFlyoutDisplayButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFlyout(listView.SelectedItem is null ? "" : (listView.SelectedItem as Character).Token, true);
        chaptersListViewFlyout.ShowAt((Button)sender);
    }

    private void OnCharactersListViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        OpenFlyout((sender as Grid).Tag.ToString(), true);

        var s = (FrameworkElement)sender;
        chaptersListViewFlyout.ShowAt(s, e.GetPosition(s));
    }

    private void OnCharactersListViewItem_Holding(object sender, HoldingRoutedEventArgs e)
    {
        OpenFlyout((sender as Grid).Tag.ToString(), true);

        chaptersListViewFlyout.ShowAt((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
    }

    private void OnChaptersListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (characterItemFlyoutedToken is null && listView.IsEnabled)
        {
            chaptersListViewFlyout.ShowAt((Grid)sender, e.GetPosition((Grid)sender));
            OpenFlyout("", false);
        }
    }

    private void ChaptersListViewItemFlyout_Closed(object sender, object e)
    {
        characterItemFlyoutedToken = null;
    }

    #region Characters Command Bar
    private void OnAddButton_Click(object sender, RoutedEventArgs e)
    {
        Add();
    }

    private void OnEditFlyout_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(characterItemFlyoutedToken))
        {
            listView.SelectedIndex = App.GetService<ProjectState>().FindCharacterID(characterItemFlyoutedToken);
            EnableEditMode(true);
        }
    }

    private void OnRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        Remove();
    }
    #endregion
    #endregion

    public void Add()
    {
        var newCharacter = ViewModel.AddNewCharacter();
        listView.SelectedItem = newCharacter;
        ViewModel.RefreshFilteredList(newCharacter?.Token);
        EnableEditMode(true);
        ViewModel.UpdateEmptyState();
    }

    public void Remove()
    {
        if (listView.SelectedItem is Character c)
            App.GetService<ProjectState>().RemoveCharacter(c.Token);

        ViewModel.RefreshFilteredList();
        ViewModel.UpdateEmptyState();
    }

    public void Sort()
    {
        var selectedToken = (listView.SelectedItem as Character)?.Token;
        App.GetService<ProjectState>().SortCharacters();
        ViewModel.RefreshFilteredList(selectedToken);
    }

    #region Characters ListView
    private void OnListDetailsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        selectionChanged = true;
        EnableEditMode(false);

        var character = listView.SelectedItem as Character;
        ViewModel.SelectedCharacter = character;

        if (character is not null)
        {
            selectedCharactersNullText.Visibility = Visibility.Collapsed;
            characterValuesPanel.Visibility = Visibility.Visible;

            // Defer items mutation until after SelectionChanged completes — WCT
            // TokenizingTextBox throws E_UNEXPECTED if Items are modified mid-event.
            DispatcherQueue.TryEnqueue(() => LoadTraitsIntoTokenBox(character.TraitsText));
            ViewModel.LoadRelationships(character);
        }
        else
        {
            selectedCharactersNullText.Visibility = Visibility.Visible;
            characterValuesPanel.Visibility = Visibility.Collapsed;
            ViewModel.LoadRelationships(null);
        }

        profilePictureFlyout.IsOpen = false;
        ViewModel.UpdateEmptyState();

        selectionChanged = false;
    }
    #endregion

    #region Character Command Bar
    private void OnUndoButton_Click(object sender, RoutedEventArgs e)
    {
        TimeTravelCharacter.Undo();
        ViewModel.RefreshFilteredList((listView.SelectedItem as Character)?.Token);
        ViewModel.UpdateEmptyState();
    }

    private void OnRedoButton_Click(object sender, RoutedEventArgs e)
    {
        TimeTravelCharacter.Redo();
        ViewModel.RefreshFilteredList((listView.SelectedItem as Character)?.Token);
        ViewModel.UpdateEmptyState();
    }

    private void OnEditButton_Click(object sender, RoutedEventArgs e)
    {
        EnableEditMode((bool)editButton.IsChecked);
    }

    private void OnCancelEditButton_Click(object sender, RoutedEventArgs e)
    {
        CancelEdit();
    }

    private void OnExportButton_Click(object sender, RoutedEventArgs e)
    {
        App.GetService<IDialogService>().OpenExportDialogue(ExportTarget.Characters);
    }
    #endregion

    // ─── TokenizingTextBox helpers for traits ─────────────────────

    private void LoadTraitsIntoTokenBox(string traitsText)
    {
        try
        {
            traitsBox.Items.Clear();
        }
        catch (Exception ex)
        {
            App.TryGetService<ILogger>()?.Warning($"Failed to clear traits token box: {ex.Message}");
        }

        if (!string.IsNullOrWhiteSpace(traitsText))
            foreach (var t in traitsText.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = t.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    try { traitsBox.Items.Add(trimmed); }
                    catch (Exception ex)
                    {
                        App.TryGetService<ILogger>()?.Warning($"Failed to add trait token '{trimmed}': {ex.Message}");
                    }
                }
            }
    }

    private string GetTraitsFromTokenBox()
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var item in traitsBox.Items)
            if (!string.IsNullOrWhiteSpace(item?.ToString()))
                parts.Add(item.ToString().Trim());
        return string.Join(", ", parts);
    }

    private void OnTraitsTokenItem_Added(CommunityToolkit.WinUI.Controls.TokenizingTextBox sender, object args)
    {
        if (!selectionChanged && DidSomethingChange())
            ViewModel.MarkUnappliedChanges();
    }

    private void OnTraitsTokenItem_Removing(CommunityToolkit.WinUI.Controls.TokenizingTextBox sender, CommunityToolkit.WinUI.Controls.TokenItemRemovingEventArgs args)
    {
        if (!selectionChanged && DidSomethingChange())
            ViewModel.MarkUnappliedChanges();
    }

    private void OnTraitsBox_TextChanged(Microsoft.UI.Xaml.Controls.AutoSuggestBox sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == Microsoft.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason.UserInput)
        {
            sender.ItemsSource = ViewModel.GetTraitSuggestions(sender.Text?.Trim() ?? string.Empty);
        }
    }

    private void Box_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!selectionChanged)
            if (ViewModel.DidSomethingChange())
                ViewModel.MarkUnappliedChanges();
            else if (ViewModel.UnappliedChanges)
                ViewModel.MarkCleanEditMode();
    }

    private void OnProfilePicture_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (ViewModel.IsEditMode)
        {
            _ = InitializeProfilePictures();
            profilePictureFlyout.IsOpen = true;
        }
    }

    public async Task InitializeProfilePictures()
    {
        var images = await GetImagesAsync();

        profilePictureHolder.Children.Clear();

        var newPictureButton = new Button()
        {
            Height = 80,
            Width = 90,
        };
        newPictureButton.Click += OnAddNewImageButton_Click;

        var icon = new SymbolIcon()
        {
            Symbol = Symbol.OpenFile,
        };
        newPictureButton.Content = icon;

        profilePictureHolder.Children.Add(newPictureButton);

        foreach (var image in images)
        {
            var imageButton = new Button()
            {
                Height = 80,
                Tag = image,
            };

            imageButton.Click += ImageButton_Click;
            imageButton.RightTapped += ImageButton_RightTapped;
            imageButton.Holding += ImageButton_Holding; ;

            var imageEl = new Image()
            {
                Height = 80,
                Margin = new Thickness(-12),
                Source = image.Image,
            };

            imageButton.Width = imageEl.Width;
            imageButton.Content = imageEl;
            profilePictureHolder.Children.Add(imageButton);
        }
    }

    private void ImageButton_Holding(object sender, HoldingRoutedEventArgs e)
    {
        ImageButton_RightTapped(sender, new RightTappedRoutedEventArgs());
    }

    private object imageSender;
    private void ImageButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        imageSender = sender;
        picturesHolderFlyout.ShowAt(sender as FrameworkElement);
    }

    private void OnCharactersListViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        EnableEditMode(true);
    }

    private async Task RemovePicture(CharacterPicture cp)
    {
        if (cp is null || string.IsNullOrWhiteSpace(cp.FileName))
            return;

        try
        {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);
            StorageFile file = await folder.GetFileAsync(cp.FileName);
            await file.DeleteAsync();
        }
        catch (Exception ex)
        {
            App.TryGetService<ILogger>()?.Warning($"Failed to remove profile picture: {ex.Message}");
        }
    }

    private CharacterPicture _picture;
    private async void ImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (!selectionChanged)
        {
            var p = (sender as Button).Tag as CharacterPicture;
            var bmp = await Character.LoadProfilePictureAsync(p);
            if (bmp is null)
            {
                App.TryGetService<INotificationService>()?.ShowNotification(new NotificationRequest
                {
                    Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                    Title = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse().GetString("picturesNotFound"),
                    Duration = TimeSpan.FromSeconds(Constants.LayoutConstants.NotificationDismissSeconds + 2)
                });
                return;
            }
            profilePicture.ProfilePicture = bmp;
            profilePictureFlyout.IsOpen = false;

            PictureChanged(p);
        }
    }

    private void PictureChanged(CharacterPicture picture)
    {
        _picture = picture;
        ViewModel.SetPicture(picture, picture?.Image);

        if (DidSomethingChange())
            ViewModel.MarkUnappliedChanges();
        else if (ViewModel.UnappliedChanges)
            ViewModel.MarkCleanEditMode();
    }

    public async Task<List<CharacterPicture>> GetImagesAsync()
    {
        StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);

        try
        {
            var files = await folder.GetFilesAsync();
            List<CharacterPicture> images = new List<CharacterPicture>();

            foreach (var file in files)
            {
                images.Add(new CharacterPicture() { FileName = file.Name, Image = new BitmapImage(new Uri(file.Path)) });
            }
            return images;
        }
        catch (Exception ex)
        {
            App.TryGetService<ILogger>()?.Warning($"Failed to load profile pictures: {ex.Message}");
            return new List<CharacterPicture>();
        }
    }

    private async Task OpenFilePickerAsync()
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".gif");

        WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowContext.Hwnd);

        StorageFile file = await picker.PickSingleFileAsync();

        if (file is not null)
        {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);
            StorageFile newFile = await file.CopyAsync(folder, file.Name, NameCollisionOption.ReplaceExisting);
            BitmapImage image = new BitmapImage(new Uri(newFile.Path));

            profilePicture.ProfilePicture = image;
            var p = new CharacterPicture() { LocalFilePath = file.Path, FileName = file.Name, Image = image };
            PictureChanged(p); 

            profilePictureFlyout.IsOpen = false;

        }
    }

    private async void OnPictureRemove_Click(object sender, RoutedEventArgs e)
    {
        await RemovePicture((imageSender as Button).Tag as CharacterPicture);
        profilePictureHolder.Children.Remove(imageSender as Button);
    }

    private void OnAddNewImageButton_Click(object sender, RoutedEventArgs e)
    {
        _ = OpenFilePickerAsync();
    }

    private void OnHyperlinkButton_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        OnAddButton_Click(sender, new RoutedEventArgs());
    }

    private void OnCharacterSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.SearchQuery = characterSearchBox.Text;
        ViewModel.RefreshFilteredList((listView.SelectedItem as Character)?.Token);
    }

    private void OnProfilePictureButton_Click(object sender, RoutedEventArgs e)
    {
        OnProfilePicture_Tapped(sender, new TappedRoutedEventArgs());
    }

    public void CheckForNullCharacter()
    {
        ViewModel.UpdateEmptyState();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshFilteredList();
        TrySelectPendingCharacter();
        ViewModel.UpdateEmptyState();
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ActualWidth < 700)
        {
            listViewColumn.Width = new GridLength(63, GridUnitType.Pixel);
            charactersCountText.Visibility = Visibility.Collapsed;
        }
        else
        {
            listViewColumn.Width = new GridLength(1, GridUnitType.Star);
            charactersCountText.Visibility = Visibility.Visible;
        }
    }

    // ─── Relationships ────────────────────────────────────────────

    private async void OnAddRelationship_Click(object sender, RoutedEventArgs e)
    {
        var character = listView.SelectedItem as Character;
        await ViewModel.AddRelationshipAsync(character);
    }

    private void OnRemoveRelationship_Click(object sender, RoutedEventArgs e)
    {
        var character = listView.SelectedItem as Character;
        var targetToken = (sender as Button)?.Tag?.ToString();
        ViewModel.RemoveRelationship(character, targetToken);
    }

    private void OnRelationshipTarget_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string targetToken)
            SelectCharacterByToken(targetToken);
    }

    private void TrySelectPendingCharacter()
    {
        if (string.IsNullOrWhiteSpace(_pendingSelectedCharacterToken))
            return;

        SelectCharacterByToken(_pendingSelectedCharacterToken);

        if ((listView.SelectedItem as Character)?.Token == _pendingSelectedCharacterToken)
            _pendingSelectedCharacterToken = null;
    }

    private void SelectCharacterByToken(string characterToken)
    {
        if (string.IsNullOrWhiteSpace(characterToken))
            return;

        if (FilteredCharacters.All(c => c.Token != characterToken)
            && !string.IsNullOrWhiteSpace(characterSearchBox?.Text))
        {
            characterSearchBox.Text = string.Empty;
            ViewModel.SearchQuery = string.Empty;
        }

        ViewModel.RefreshFilteredList(characterToken);
    }

    private void NavigateToChapter(string chapterToken)
    {
        ViewModel.NavigateToChapter(chapterToken);
    }

    /// <summary>
    /// Refreshes branching dialogue insights for the given character.
    /// Only available when the dialogue plugin is compiled in.
    /// </summary>
    private void UpdateDialogueInsights(Character character)
    {
        // No-op when the dialogue plugin is not available.
    }
}
