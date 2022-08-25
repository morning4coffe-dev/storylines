using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class SaveDialogue : ContentDialog
    {
        public static SaveDialogue saveDialogue;
        public StorageFolder saveFolder;

        public ObservableCollection<string> extensions { get; private set; } = new ObservableCollection<string>();

        public enum Type { Save, SaveCopy }
        private static Type type;

        public SaveDialogue()
        {
            InitializeComponent();
            saveDialogue = this;

            InitializeClickOutToClose();

            saveDialogue.RequestedTheme = AppView.current.RequestedTheme;
            AppView.currentlyOpenedDialogue = saveDialogue;

            extensions.Add(".srl");

            if (Chapter.chapters.Count <= 1 && Character.characters.Count == 0)
                extensions.Add(".txt");
            else 
                extensionComboBox.IsEnabled = false;

            extensionComboBox.SelectedIndex = 0;

            title.Text = Storylines.Resources.SaveDialogue.Title(type);
        }

        public static void Open(Type type)
        {
            if(AppView.currentlyOpenedDialogue != null)
                AppView.currentlyOpenedDialogue.Hide();

            SaveDialogue.type = type;
            if (AppView.currentlyOpenedDialogue == null)
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

        public void SomethingChanged(bool nameOrLocation)
        {
            if (saveFolder != null && SettingsValues.IsStringSaveable(fileNameText.Text))
                submitButton.IsEnabled = true;
            else
                submitButton.IsEnabled = false;

            if (nameOrLocation && saveFolder != null && !string.IsNullOrEmpty(fileNameText.Text))
                try
                {
                    var file = saveFolder.TryGetItemAsync($"{fileNameText.Text + extensionComboBox.SelectedItem}").AsTask().GetAwaiter().GetResult();

                    nameCollisionWarning.Visibility = file != null ? Visibility.Visible : Visibility.Collapsed;
                }
                catch { }
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSystem.NewFile(saveFolder, $"{fileNameText.Text}{extensionComboBox.SelectedItem}");
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
