using Storylines.Views.Dialogs;
using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace Storylines.Views.Pages
{
    public sealed partial class CharactersPage : Page
    {
        private readonly EventAggregator _events;
        private readonly INavigationService _navigation;
        private readonly ProjectState _projectState;
        private readonly WindowContext _windowContext;
        private string _pendingSelectedCharacterToken;


        public ObservableCollection<Character> Characters => _projectState.Characters;
        public ObservableCollection<Character> FilteredCharacters { get; } = new ObservableCollection<Character>();
        public CharactersPageViewModel ViewModel { get; }
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForViewIndependentUse();

        private bool selectionChanged = false;
        public bool unappliedChanges
        {
            get => ViewModel.UnappliedChanges;
            set => ViewModel.UnappliedChanges = value;
        }



        public CharactersPage()
        {
            InitializeComponent();

            _windowContext = App.GetService<WindowContext>();
            _events = App.GetService<EventAggregator>();
            _navigation = App.GetService<INavigationService>();
            _projectState = App.GetService<ProjectState>();
            ViewModel = App.GetService<CharactersPageViewModel>();

            _windowContext.CharactersPage = this;

            _windowContext.AppView.page = AppView.Pages.Characters;

            TimeTravelCharacter.ClearUndoAndRedo();

            RefreshCharacterList();

            _events.Subscribe<UndoRedoStateChangedEvent>(OnUndoRedoStateChanged);

            // Listen for character selection events from TimeTravelCharacter
            _events.Subscribe<CharacterSelectedEvent>(e =>
            {
                if (e.HasSelection && e.SelectedIndex >= 0 && e.SelectedIndex < _projectState.Characters.Count)
                {
                    var selectedToken = _projectState.Characters[e.SelectedIndex].Token;
                    if (FilteredCharacters.All(character => character.Token != selectedToken) && !string.IsNullOrWhiteSpace(characterSearchBox.Text))
                        characterSearchBox.Text = string.Empty;

                    RefreshCharacterList(selectedToken);
                }
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _pendingSelectedCharacterToken = e.Parameter as string;
            TrySelectPendingCharacter();
        }

        private void OnUndoRedoStateChanged(UndoRedoStateChangedEvent e)
        {
            if (e.Context == "characters")
            {
                ViewModel.CanUndo = e.CanUndo;
                ViewModel.CanRedo = e.CanRedo;
            }
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

                RefreshCharacterList(character.Token);
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
                listView.SelectedIndex = _projectState.FindCharacterID(characterItemFlyoutedToken);
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
            int value = Random.Shared.Next(0, 2);

            listView.SelectedItem = _projectState.CreateNewCharacter(value == 1 ? ResourceLoader.GetForViewIndependentUse().GetString("johnDoe") : ResourceLoader.GetForViewIndependentUse().GetString("janeDoe"), "");
            RefreshCharacterList((listView.SelectedItem as Character)?.Token);
            EnableEditMode(true);

            CheckForNullCharacter();
        }

        public void Remove()
        {
            if (listView.SelectedItem is not null)
                _projectState.RemoveCharacter((listView.SelectedItem as Character).Token);

            RefreshCharacterList();
            CheckForNullCharacter();
        }

        public void Sort()
        {
            var selectedToken = (listView.SelectedItem as Character)?.Token;
            _projectState.SortCharacters();
            RefreshCharacterList(selectedToken);
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

                LoadTraitsIntoTokenBox(character.TraitsText);
                LoadRelationships(character);
            }
            else
            {
                selectedCharactersNullText.Visibility = Visibility.Visible;
                characterValuesPanel.Visibility = Visibility.Collapsed;
                LoadRelationships(null);
            }

            profilePictureFlyout.IsOpen = false;
            CheckForNullCharacter();

            selectionChanged = false;
        }
        #endregion

        #region Character Command Bar
        private void OnUndoButton_Click(object sender, RoutedEventArgs e)
        {
            TimeTravelCharacter.Undo();

            RefreshCharacterList((listView.SelectedItem as Character)?.Token);
            CheckForNullCharacter();
        }

        private void OnRedoButton_Click(object sender, RoutedEventArgs e)
        {
            TimeTravelCharacter.Redo();

            RefreshCharacterList((listView.SelectedItem as Character)?.Token);
            CheckForNullCharacter();
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
                        traitsBox.Items.Add(trimmed);
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
                // Provide all unique traits from existing characters as suggestions
                var allTraits = new System.Collections.Generic.HashSet<string>(System.StringComparer.CurrentCultureIgnoreCase);
                foreach (var ch in _projectState.Characters)
                    foreach (var trait in ch.Traits ?? new System.Collections.Generic.List<string>())
                        allTraits.Add(trait);

                var query = sender.Text?.Trim() ?? string.Empty;
                sender.ItemsSource = string.IsNullOrWhiteSpace(query)
                    ? new System.Collections.Generic.List<string>(allTraits)
                    : new System.Collections.Generic.List<string>(allTraits.Where(t => t.StartsWith(query, System.StringComparison.CurrentCultureIgnoreCase)));
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
                    App.TryGetService<INotificationService>()?.ShowNotification(
                        Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                        Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse().GetString("picturesNotFound"));
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
            RefreshCharacterList((listView.SelectedItem as Character)?.Token);
        }

        private void OnProfilePictureButton_Click(object sender, RoutedEventArgs e)
        {
            OnProfilePicture_Tapped(sender, new TappedRoutedEventArgs());
        }

        public void CheckForNullCharacter()
        { 
            if (Characters.Count == 0)
            {
                charactersNullText.Visibility = Visibility.Visible;
                charactersSearchNullText.Visibility = Visibility.Collapsed;
            }
            else if (FilteredCharacters.Count == 0)
            {
                charactersNullText.Visibility = Visibility.Collapsed;
                charactersSearchNullText.Visibility = Visibility.Visible;
            }
            else
            {
                charactersNullText.Visibility = Visibility.Collapsed;
                charactersSearchNullText.Visibility = Visibility.Collapsed;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshCharacterList();
            TrySelectPendingCharacter();
            CheckForNullCharacter();
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

        private void RefreshCharacterList(string selectedToken = null)
        {
            selectedToken ??= (listView.SelectedItem as Character)?.Token;

            var query = (characterSearchBox?.Text ?? string.Empty).Trim();
            var matchingCharacters = Characters
                .Where(character => MatchesCharacterSearch(character, query))
                .ToList();

            FilteredCharacters.Clear();

            foreach (var character in matchingCharacters)
                FilteredCharacters.Add(character);

            if (!string.IsNullOrWhiteSpace(selectedToken))
                listView.SelectedItem = FilteredCharacters.FirstOrDefault(character => character.Token == selectedToken);

            CheckForNullCharacter();
        }

        private static bool MatchesCharacterSearch(Character character, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            var searchTarget = string.Join(" ", new[]
            {
                character?.Name,
                character?.Role,
                character?.Age,
                character?.TraitsText,
                character?.Appearance,
                character?.Description,
            });

            return searchTarget?.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        // ─── Relationships ────────────────────────────────────────────

        public void LoadRelationships(Character character)
        {
            var items = new List<RelationshipDisplayItem>();

            if (character?.Relationships is not null)
            {
                foreach (var rel in character.Relationships)
                {
                    var target = _projectState.FindCharacter(rel.TargetCharacterToken);
                    items.Add(new RelationshipDisplayItem
                    {
                        DisplayText = target?.Name ?? "(unknown)",
                        Type = rel.Type ?? "",
                        TargetToken = rel.TargetCharacterToken
                    });
                }
            }

            relationshipsListView.ItemsSource = items;
            noRelationshipsText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            addRelationshipButton.IsEnabled = character is not null;
        }

        private async void OnAddRelationship_Click(object sender, RoutedEventArgs e)
        {
            var character = listView.SelectedItem as Character;
            if (character is null) return;

            var otherCharacters = Characters.Where(c => c.Token != character.Token).ToList();
            if (otherCharacters.Count == 0) return;

            var targetCombo = new ComboBox
            {
                PlaceholderText = resourceLoader.GetString("relationshipSelectCharacter"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = otherCharacters.Select(c => c.Name).ToList()
            };
            var typeBox = new TextBox { PlaceholderText = resourceLoader.GetString("relationshipTypePlaceholder") };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(new TextBlock { Text = resourceLoader.GetString("relationshipCharacterLabel") });
            panel.Children.Add(targetCombo);
            panel.Children.Add(new TextBlock { Text = resourceLoader.GetString("relationshipTypeLabel") });
            panel.Children.Add(typeBox);

            var result = await App.GetService<IDialogService>().ShowMessageAsync(new DialogDefinition
            {
                Title = resourceLoader.GetString("addRelationshipTitle"),
                Content = panel,
                PrimaryButtonText = resourceLoader.GetString("addButtonText"),
                CloseButtonText = resourceLoader.GetString("cancelButtonText"),
                DefaultButton = ContentDialogButton.Primary,
            });
            if (result == ContentDialogResult.Primary && targetCombo.SelectedIndex >= 0)
            {
                var target = otherCharacters[targetCombo.SelectedIndex];
                character.Relationships.Add(new CharacterRelationship
                {
                    TargetCharacterToken = target.Token,
                    Type = typeBox.Text?.Trim() ?? ""
                });
                TimeTravelSystem.SomethingChanged();
                LoadRelationships(character);
            }
        }

        private void OnRemoveRelationship_Click(object sender, RoutedEventArgs e)
        {
            var character = listView.SelectedItem as Character;
            if (character is null) return;

            var targetToken = (sender as Button)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(targetToken))
            {
                var rel = character.Relationships.FirstOrDefault(r => r.TargetCharacterToken == targetToken);
                if (rel is not null)
                {
                    character.Relationships.Remove(rel);
                    TimeTravelSystem.SomethingChanged();
                    LoadRelationships(character);
                }
            }
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

            if (FilteredCharacters.All(character => character.Token != characterToken)
                && !string.IsNullOrWhiteSpace(characterSearchBox?.Text))
            {
                characterSearchBox.Text = string.Empty;
            }

            RefreshCharacterList(characterToken);
        }

        private void NavigateToChapter(string chapterToken)
        {
            if (!string.IsNullOrWhiteSpace(chapterToken))
                _navigation?.NavigateTo(NavigationTarget.MainPage, chapterToken);
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

    public class RelationshipDisplayItem
    {
        public string DisplayText { get; set; }
        public string Type { get; set; }
        public string TargetToken { get; set; }
    }
}
