using Storylines.Components.DialogueWindows;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Variables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Storylines.Pages
{
    public sealed partial class CharactersPage : Page
    {
        public bool isEditModeEnabled { set; get; } = false;
        public bool isAddEnabled { set; get; } = true;
        public bool isRemoveEnabled { set; get; } = true;

        private bool selectionChanged = false;
        public bool unappliedChanges = false;

        public static CharactersPage current { get; private set; }

        //private enum Sort

        public CharactersPage()
        {
            InitializeComponent();

            current = this;

            AppView.current.page = AppView.Pages.Characters;

            TimeTravelCharacter.ClearUndoAndRedo();
        }

        Character characterBeforeChange;

        public void EnableEditMode(bool enable)
        {
            isEditModeEnabled = enable;

            nameBox.IsEnabled = enable;
            descriptionBox.IsEnabled = enable;
            profilePicture.IsTapEnabled = enable;

            editButton.IsChecked = enable;

            if (enable)
            {
                var ch = listView.SelectedItem as Character;
                characterBeforeChange = new Character() { name = ch.name, description = ch.description, picture = ch.picture };
                characterBeforeChange.SetToken(ch.token);

                //charactersCommandBar.IsEnabled = false;
                listView.IsEnabled = false;

                IsEditEnabled(EditButton.Cancel);
            }
            else
            {
                //charactersCommandBar.IsEnabled = true;
                listView.IsEnabled = true;

                IsEditEnabled(EditButton.Edit);
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
            if (listView.SelectedItem != null && characterBeforeChange != null)
            {
                TimeTravelCharacter.SomethingChanged(TimeTravelCharacter.Changed.Changed, listView.SelectedItem as Character);

                unappliedChanges = false;

                (listView.SelectedItem as Character).name = nameBox.Text;
                (listView.SelectedItem as Character).description = descriptionBox.Text;
                if(_picture != null)
                    (listView.SelectedItem as Character).picture = _picture;
            }
        }

        public void CancelEdit()
        {
            IsEditEnabled(EditButton.Edit);

            nameBox.Text = characterBeforeChange.name;
            descriptionBox.Text = characterBeforeChange.description;
            profilePicture.ProfilePicture = characterBeforeChange.picture.image;
            _picture = characterBeforeChange.picture;

            EnableEditMode(false);
        }

        private enum EditButton { Edit, ApplyChanges, Cancel }
        private void IsEditEnabled(EditButton edit)
        {
            switch (edit)
            {
                case EditButton.Edit:
                    cancelButton.Visibility = Visibility.Collapsed;
                    editButton.Label = ResourceLoader.GetForCurrentView().GetString("editText");
                    editButtonIcon.Glyph = "";

                    unappliedChanges = false;
                    break;
                case EditButton.ApplyChanges:
                    cancelButton.Visibility = Visibility.Visible;
                    editButton.Label = ResourceLoader.GetForCurrentView().GetString("applyChanges");
                    editButtonIcon.Glyph = "";

                    unappliedChanges = true;
                    break;
                case EditButton.Cancel:
                    cancelButton.Visibility = Visibility.Collapsed;
                    editButton.Label = ResourceLoader.GetForCurrentView().GetString("cancelText");
                    editButtonIcon.Glyph = "";

                    unappliedChanges = false;
                    break;
            }
        }

        public bool DidSomethingChange()
        {
            if ((listView.SelectedItem as Character).name == nameBox.Text && (listView.SelectedItem as Character).description == descriptionBox.Text && (listView.SelectedItem as Character).picture.image == (BitmapImage)profilePicture.ProfilePicture)
                return false;
            else
                return true;
        }

        #region Flyout
        private string characterItemFlyoutedToken;
        private void OpenFlyout(string token, bool enabled)
        {
            characterItemFlyoutedToken = token;

            addFlyout.IsEnabled = isAddEnabled;

            editFlyout.IsEnabled = enabled;
            removeFlyout.IsEnabled = enabled;
        }

        private void OnFlyoutDisplayButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFlyout(listView.SelectedItem == null ? "" : (listView.SelectedItem as Character).token, true);
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
            if (characterItemFlyoutedToken == null && listView.IsEnabled)
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
                listView.SelectedIndex = Character.FindID(characterItemFlyoutedToken);
                EnableEditMode(true);
            }
        }

        private void OnRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Remove();
        }
        #endregion
        #endregion

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.current.OpenOrCloseChapterList(false, true);
        }

        public void Add()
        {
            Random rn = new Random();
            int value = rn.Next(0, 2);

            //charactersHolder.SelectedItem = Character.CreateNew(value == 1 ? ResourceLoader.GetForCurrentView().GetString("johnDoe") : ResourceLoader.GetForCurrentView().GetString("janeDoe"), ResourceLoader.GetForCurrentView().GetString("ownDescription"));
            listView.SelectedItem = Character.CreateNew(value == 1 ? ResourceLoader.GetForCurrentView().GetString("johnDoe") : ResourceLoader.GetForCurrentView().GetString("janeDoe"), "");
            EnableEditMode(true);

            CheckForNullCharacter();
        }

        public void Remove()
        {
            if (listView.SelectedItem != null)
                Character.Remove((listView.SelectedItem as Character).token);

            CheckForNullCharacter();
        }

        public void Sort()
        {
            var s = listView.SelectedItem;
            Character.characters = new ObservableCollection<Character>(Character.characters.OrderBy(o => o.name).ToList());
            listView.ItemsSource = Character.characters;
            listView.SelectedItem = s;
        }

        #region Characters ListView
        private void OnListDetailsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectionChanged = true;
            EnableEditMode(false);

            if (listView.SelectedItem != null)
            {
                selectedCharactersNullText.Visibility = Visibility.Collapsed;
                characterValuesPanel.Visibility = Visibility.Visible;

                nameBox.Text = (listView.SelectedItem as Character).name;
                descriptionBox.Text = (listView.SelectedItem as Character).description;
                profilePicture.ProfilePicture = (listView.SelectedItem as Character).picture.image != null ? (listView.SelectedItem as Character).picture.image : null;

                editButton.IsEnabled = true;
            }
            else
            {
                selectedCharactersNullText.Visibility = Visibility.Visible;
                characterValuesPanel.Visibility = Visibility.Collapsed;

                editButton.IsEnabled = false;
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

            CheckForNullCharacter();
        }

        private void OnRedoButton_Click(object sender, RoutedEventArgs e)
        {
            TimeTravelCharacter.Redo();

            CheckForNullCharacter();
        }

        //private void OnSortButtons_Click(object sender, RoutedEventArgs e)
        //{
        //    switch ((sender as Control).Tag)
        //    {
        //        case "AtoZ":
        //            Character.characters = new ObservableCollection<Character>(Character.characters.OrderBy(o => o.name).ToList());
        //            listView.ItemsSource = Character.characters;
        //            break;
        //        case "ZtoA":
        //            Character.characters = new ObservableCollection<Character>(Character.characters.OrderByDescending(o => o.name).ToList());
        //            listView.ItemsSource = Character.characters;
        //            break;
        //    }
        //}

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
            ExportDialogue.Open(Components.ExportSystem.WhatToExport.Characters);
        }
        #endregion

        private void Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!selectionChanged)
                if (DidSomethingChange())
                    IsEditEnabled(EditButton.ApplyChanges);
                else
                    if (unappliedChanges)
                        IsEditEnabled(EditButton.Cancel);
        }

        private void OnProfilePicture_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (isEditModeEnabled)
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
                    Source = image.image,
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
            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);
            StorageFile file = await folder.GetFileAsync(cp.fileName);
            _ = file.DeleteAsync();
        }

        private CharacterPicture _picture;
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!selectionChanged)
            {
                var p = (sender as Button).Tag as CharacterPicture;
                profilePicture.ProfilePicture = Character.ProfilePictureFromCharacterPictureAsync(p);
                profilePictureFlyout.IsOpen = false;

                PictureChanged(p);
            }
        }

        private void PictureChanged(CharacterPicture picture)
        {
            _picture = picture;

            if (DidSomethingChange())
                IsEditEnabled(EditButton.ApplyChanges);
            else
            if (unappliedChanges)
                IsEditEnabled(EditButton.Cancel);
        }

        public async Task<List<CharacterPicture>> GetImagesAsync()
        {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);

            try
            {
                var files = folder.GetFilesAsync().AsTask().GetAwaiter().GetResult();
                List<CharacterPicture> images = new List<CharacterPicture>();

                foreach (var file in files)
                {
                    images.Add(new CharacterPicture() { fileName = file.Name, image = new BitmapImage(new Uri(file.Path)) });
                }
                return images;
            }
            catch { return null; }
        }

        //StorageFile file;
        private async Task OpenFilePickerAsync()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".gif");

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);
                StorageFile newFile = await file.CopyAsync(folder, file.Name, NameCollisionOption.ReplaceExisting);
                BitmapImage image = new BitmapImage(new Uri(newFile.Path));

                profilePicture.ProfilePicture = image;
                var p = new CharacterPicture() { localFilePath = file.Path, fileName = file.Name, image = image };
                PictureChanged(p); 
                
                profilePictureFlyout.IsOpen = false;

                //imageCropperHolder.Visibility = Visibility.Visible;
                //profilePictureHolderScroll.Visibility = Visibility.Collapsed;
                //await imageCropper.LoadImageFromFile(file);
            }
        }

        //private async Task ProfilePicConfirm()
        //{ 
        //    StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePictures", CreationCollisionOption.OpenIfExists);
        //    StorageFile newFile = await file.CopyAsync(folder, file.Name.Remove(file.Name.Length - 4) + ".png", NameCollisionOption.ReplaceExisting);
        //    var stream = await newFile.OpenAsync(FileAccessMode.Read);
        //    await imageCropper.SaveAsync(stream, Microsoft.Toolkit.Uwp.UI.Controls.BitmapFileFormat.Png, false);
        //    //stream.WriteAsync();

        //    using (var outputStream = stream.GetOutputStreamAt(0))
        //    {
        //        using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
        //        {
        //            dataWriter.WriteBuffer
        //        }
        //    }
        //    stream.Dispose();
        //    await FileIO.WriteTextAsync(newFile, stream.ToString());

        //    BitmapImage image = new BitmapImage(new Uri(newFile.Path));

        //    profilePicture.ProfilePicture = image;
        //    (listView.SelectedItem as Character).picture = new CharacterPicture() { localFilePath = file.Path, fileName = file.Name.Remove(file.Name.Length - 4) + ".png", image = image };

        //    profilePictureFlyout.IsOpen = false;
        //    MainPage.mainPage.SomethingChanged();
        //}

        //private void OnConfirmProfilePictureButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if(file != null)
        //    _ = ProfilePicConfirm();
        //}

        private void OnPictureRemove_Click(object sender, RoutedEventArgs e)
        {
            _ = RemovePicture((imageSender as Button).Tag as CharacterPicture);
            profilePictureHolder.Children.Remove(imageSender as Button);
        }

        private void OnAddNewImageButton_Click(object sender, RoutedEventArgs e)
        {
            _ = OpenFilePickerAsync();
        }

        private void OnHyperlinkButton_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            OnAddButton_Click(sender, new RoutedEventArgs());
        }

        private void OnProfilePicture_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //if(isEditModeEnabled)
            //    profilePictureHoverIcon.Visibility = Visibility.Visible;
        }

        private void OnProfilePicture_PointerExited(object sender, PointerRoutedEventArgs e)
        {
                //profilePictureHoverIcon.Visibility = Visibility.Collapsed;
        }

        private void OnProfilePictureButton_Click(object sender, RoutedEventArgs e)
        {
            OnProfilePicture_Tapped(sender, new TappedRoutedEventArgs());
        }

        public void CheckForNullCharacter()
        { 
            if(listView.Items.Count > 0)
                charactersNullText.Visibility = Visibility.Collapsed;
            else
                charactersNullText.Visibility = Visibility.Visible;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CheckForNullCharacter();

            IsEditEnabled(EditButton.Edit);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth < 700)
            {
                listViewColumn.Width = new GridLength(63, GridUnitType.Pixel);
                charactersCountText.Visibility = Visibility.Collapsed;

                //charactersNullText
                //mainGrid.Children.Remove(charactersCommandBar);
                //characterCommandBarHolder.Children.Insert(0, charactersCommandBar);
            }
            else
            {
                listViewColumn.Width = new GridLength(1, GridUnitType.Star);
                charactersCountText.Visibility = Visibility.Visible;
                //characterCommandBarHolder.Children.Remove(charactersCommandBar);
                //mainGrid.Children.Add(charactersCommandBar);
             }

            //if (ActualWidth < 800)
            //    charactersCommandBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;
            //else
            //    charactersCommandBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
        }
    }
}
