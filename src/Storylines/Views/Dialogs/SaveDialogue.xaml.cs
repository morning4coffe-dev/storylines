using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Models;
using System;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System.Threading.Tasks;

namespace Storylines.Views.Dialogs
{
    public sealed partial class SaveDialogue : ContentDialog
    {
        private readonly ILogger _logger;
        private readonly ProjectState _projectState;

        public static SaveDialogue saveDialogue;
        public StorageFolder saveFolder;

        public ObservableCollection<string> extensions { get; private set; } = new ObservableCollection<string>();

        public enum Type { Save, SaveCopy }
        private static Type type;

        public SaveDialogue()
        {
            InitializeComponent();
            saveDialogue = this;

            _logger = App.GetService<ILogger>();
            _projectState = App.GetService<ProjectState>();

            InitializeClickOutToClose();

            saveDialogue.RequestedTheme = AppView.current.RequestedTheme;
            AppView.currentlyOpenedDialogue = saveDialogue;

            extensions.Add(".srl");

            if (_projectState.Chapters.Count <= 1 && _projectState.Characters.Count == 0)
                extensions.Add(".txt");
            else 
                extensionComboBox.IsEnabled = false;

            extensionComboBox.SelectedIndex = 0;

            title.Text = Storylines.Resources.SaveDialogue.Title(type);
        }

        public static void Open(Type type)
        {
            var currentDialog = AppView.currentlyOpenedDialogue;
            if (currentDialog == LoadProjectDialogue.loadFile)
                LoadProjectDialogue.loadFile.isEscape = false;

            AppView.currentlyOpenedDialogue = null;
            currentDialog?.Hide();

            SaveDialogue.type = type;
            _ = new SaveDialogue().ShowAsync();
        }

        public async Task ChooseFileToSaveAsync()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                saveFolder = folder;
                locationText.Text = folder.Path;
                locationText.Visibility = Visibility.Visible;
                locationTextPlaceholder.Visibility = Visibility.Collapsed;

                SomethingChanged(true);
            }
        }

        public async void SomethingChanged(bool nameOrLocation)
        {
            if (saveFolder != null && SettingsValues.IsStringSaveable(fileNameText.Text))
                submitButton.IsEnabled = true;
            else
                submitButton.IsEnabled = false;

            if (nameOrLocation && saveFolder != null && !string.IsNullOrEmpty(fileNameText.Text))
                try
                {
                    var file = await saveFolder.TryGetItemAsync($"{fileNameText.Text + extensionComboBox.SelectedItem}");

                    nameCollisionWarning.Visibility = file != null ? Visibility.Visible : Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Failed to check for file collision: {ex.Message}");
                }
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SaveSystem.NewFileAsync(saveFolder, $"{fileNameText.Text}{extensionComboBox.SelectedItem}");
            SaveSystem.currentProject.projectName = nameText.Text;
            saveDialogue.Hide();
        }

        private void OnSaveToLocationButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ChooseFileToSaveAsync();
        }

        private void OnSaveLocationFrame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _ = ChooseFileToSaveAsync();
        }

        private void OnTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SomethingChanged(true);
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            saveDialogue.Hide();
        }

        private void ContentDialog_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && submitButton.IsEnabled)
                    OnSubmitButton_Click(sender, new RoutedEventArgs());
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            AppView.currentlyOpenedDialogue = null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _ = ChooseFileToSaveAsync();
        }

        bool isFlyoutOpen = false;
        private void OnExtensionComboBox_DropDownOpened(object sender, object e)
        {
            isFlyoutOpen = true;
        }

        private void OnExtensionComboBox_DropDownClosed(object sender, object e)
        {
            isFlyoutOpen = false;
        }

        bool isHide = true;
        private void InitializeClickOutToClose()
        {
            Window.Current.CoreWindow.PointerPressed += (s, e) =>
            {
                if (isHide && !isFlyoutOpen)
                    Hide();
            };

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }
    }
}
