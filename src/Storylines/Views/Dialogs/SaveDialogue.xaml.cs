using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Models;
using System;
using System.Collections.ObjectModel;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Storylines.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

namespace Storylines.Views.Dialogs
{
    public sealed partial class SaveDialogue : ContentDialog
    {
        private readonly ILogger _logger;
        private readonly IProjectPersistenceService _persistence;
        private readonly ProjectState _projectState;
        private readonly IFilePickerService _filePicker;
        private readonly WindowContext _windowContext;

        public static SaveDialogue saveDialogue;
        public StorageFolder saveFolder;

        public ObservableCollection<string> extensions { get; private set; } = new ObservableCollection<string>();
        private bool _submitted;
        private int _collisionCheckVersion;

        public enum Type { Save, SaveCopy }
        private static Type type;

        public SaveDialogue()
        {
            InitializeComponent();
            DialogHelper.EnsureXamlRoot(this);
            saveDialogue = this;

            _logger = App.GetService<ILogger>();
            _persistence = App.GetService<IProjectPersistenceService>();
            _projectState = App.GetService<ProjectState>();
            _filePicker = App.GetService<IFilePickerService>();
            _windowContext = App.GetService<WindowContext>();

            InitializeClickOutToClose();

            saveDialogue.RequestedTheme = App.GetService<WindowContext>().AppView.ActualTheme;
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
            StorageFolder folder = await _filePicker.PickFolderAsync();

            if (folder != null)
            {
                saveFolder = folder;
                locationText.Text = folder.Path;
                locationText.Visibility = Visibility.Visible;
                locationTextPlaceholder.Visibility = Visibility.Collapsed;

                _ = SomethingChangedAsync(true);
            }
        }

        private async Task SomethingChangedAsync(bool nameOrLocation)
        {
            submitButton.IsEnabled = saveFolder != null && SettingsValues.IsStringSaveable(fileNameText.Text);

            if (!nameOrLocation || saveFolder == null || string.IsNullOrEmpty(fileNameText.Text))
            {
                nameCollisionWarning.Visibility = Visibility.Collapsed;
                return;
            }

            var collisionCheckVersion = ++_collisionCheckVersion;

            try
            {
                var fileName = $"{fileNameText.Text}{extensionComboBox.SelectedItem}";
                var file = await saveFolder.TryGetItemAsync(fileName);

                if (collisionCheckVersion != _collisionCheckVersion)
                    return;

                nameCollisionWarning.Visibility = file != null ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                if (collisionCheckVersion == _collisionCheckVersion)
                    nameCollisionWarning.Visibility = Visibility.Collapsed;

                _logger?.Warning($"Failed to check for file collision: {ex.Message}");
            }
        }

        private async void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_persistence.CurrentProject == null)
                _persistence.CurrentProject = new ProjectFile();

            _persistence.CurrentProject.projectName = nameText.Text;
            await _persistence.NewFileAsync(saveFolder, $"{fileNameText.Text}{extensionComboBox.SelectedItem}");
            _submitted = true;
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
            _ = SomethingChangedAsync(true);
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
            _windowContext.RootElement.PointerPressed -= OnWindowPointerPressed;

            if (!_submitted)
                _persistence.CancelPendingAfterSaveAction();

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
            _windowContext.RootElement.PointerPressed += OnWindowPointerPressed;

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }

        private void OnWindowPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (isHide && !isFlyoutOpen)
                Hide();
        }
    }
}
