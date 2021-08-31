using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class ExportDialogue : ContentDialog
    {
        public static ExportDialogue exportDialogue;

        private static ExportSystem.WhatToExport openExport;

        public StorageFolder saveFolder;

        public ObservableCollection<string> extensions { get; private set; } = new ObservableCollection<string>();

        public ExportDialogue()
        {
            InitializeComponent();
            exportDialogue = this;

            InitializeClickOutToClose();

            exportDialogue.RequestedTheme = AppView.current.RequestedTheme;

            AppView.currentlyOpenedDialogue = exportDialogue;

            fileNameText.Text = $"{(SaveSystem.currentProject.file != null ? SaveSystem.currentProject.file.DisplayName : "my-story")}-export";

            SomethingChanged(false);

            if (openExport != default)
            {
                chooseWhatToExportAnimation.FromVerticalOffset = 0;
                switch (openExport)
                {
                    case ExportSystem.WhatToExport.Chapters:
                        chooseExportChaptersButton.IsChecked = true;
                        exportDialogue.OnChooseExportChaptersButton_Click(new object(), new RoutedEventArgs());
                        break;
                    case ExportSystem.WhatToExport.Dialogues:
                        chooseExportDialoguesButton.IsChecked = true;
                        exportDialogue.OnChooseExportDialoguesButton_Click(new object(), new RoutedEventArgs());
                        break;
                    case ExportSystem.WhatToExport.Characters:
                        chooseExportCharactersButton.IsChecked = true;
                        exportDialogue.OnChooseExportCharactersButton_Click(new object(), new RoutedEventArgs());
                        break;
                }

                openExport = default;
            }
        }

        public static void Open(ExportSystem.WhatToExport openExport)
        {
            ExportDialogue.openExport = openExport;
            _ = new ExportDialogue().ShowAsync();
        }

        public void AddToExport(bool characters)
        {
            int num = characters ? Character.characters.Count : Chapter.chapters.Count;

            chaptersToExportList.Items.Clear();

            for (int i = 0; i < num; i++)
            {
                string itemName = characters ? Character.characters[i].name : Chapter.chapters[i].name;

                chaptersToExportList.Items.Add(new ListViewItem() { Content = itemName, IsSelected = true });
            }

            characterDialoguesToExportList.Items.Clear();

            for (int i = 0; i < Character.characters.Count; i++)
            {
                characterDialoguesToExportList.Items.Add(new ListViewItem() { Content = Character.characters[i].name, IsSelected = true });
            }
        }

        public void SomethingChanged(bool nameOrLocation)
        {
            //if(Character.characters > 0)
            if (saveFolder != null && fileNameText.Text.Length > 0 && SettingsValues.IsStringSaveable(fileNameText.Text))
            {
                if (ExportSystem.export != ExportSystem.WhatToExport.Characters && chaptersToExportList.SelectedItems.Count > 0)
                {
                    submitButton.IsEnabled = true;
                } 
                else
                {
                    if (ExportSystem.export == ExportSystem.WhatToExport.Characters && characterDialoguesToExportList.SelectedItems.Count > 0)
                        submitButton.IsEnabled = true;
                    else
                        submitButton.IsEnabled = false;
                }
            }
            else
                submitButton.IsEnabled = false;

            if (nameOrLocation && saveFolder != null && !string.IsNullOrEmpty(fileNameText.Text))
                try
                {
                    var n = fileNameText.Text + extensionComboBox.SelectedItem;
                    IStorageItem file = saveFolder.TryGetItemAsync($"{n}").AsTask().GetAwaiter().GetResult();

                    nameCollisionWarning.Visibility = file != null ? Visibility.Visible : Visibility.Collapsed;
                }
                catch { }
        }


        public string DropdownContent(ListView listView)
        {
            SomethingChanged(true);

            if (listView.SelectedItems.Count == listView.Items.Count && listView.SelectedItems.Count > 0)
                return ResourceLoader.GetForCurrentView().GetString("all");
            else
            if (listView.SelectedItems.Count == 0)
                return ResourceLoader.GetForCurrentView().GetString("none");
            else
            {
                string txt = "";

                for (int i = 0; i < listView.SelectedItems.Count; i++)
                {
                    txt += txt != "" ? $", {(listView.SelectedItems[i] as ListViewItem).Content}" : $"{(listView.SelectedItems[i] as ListViewItem).Content}";
                }
                return txt;
            }
        }

        public async Task ChooseFileToExportAsync()
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

        private void OnExportButton_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedIndexes = new List<int>();

            for (int i = 0; i < chaptersToExportList.SelectedItems.Count; i++)
            {
                selectedIndexes.Add(chaptersToExportList.Items.IndexOf(chaptersToExportList.SelectedItems[i]));
            }

            List<Character> selectedIndexes2 = new List<Character>();

            for (int i = 0; i < characterDialoguesToExportList.SelectedItems.Count; i++)
            {
                selectedIndexes2.Add(Character.characters[i]);
            }

            ExportSystem.Export(saveFolder, fileNameText.Text, extensionComboBox.SelectedItem as string, selectedIndexes, selectedIndexes2, (bool)withChapterNameCheckBox.IsChecked);
            Hide();
            //}
            //else
            //{
            //    fileNameText.PlaceholderForeground = new SolidColorBrush(new Color() { A = 255, R = 252, B = 3, G = 40 });
            //    fileNameText.Foreground = new SolidColorBrush(new Color() { A = 255, R = 252, B = 3, G = 40 });
            //}
        }

        private void OnChooseExportChaptersButton_Click(object sender, RoutedEventArgs e)
        {
            chooseExportChaptersPanel.Visibility = (bool)chooseExportChaptersButton.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            if ((bool)chooseExportChaptersButton.IsChecked)
            {
                withChapterNameCheckBox.Visibility = Visibility.Visible;
                characterDialoguesToExportHolder.Visibility = Visibility.Collapsed;

                extensions.Clear();

                AddToExport(false);

                extensions.Add(".txt");
                extensions.Add(".rtf");
                extensionComboBox.SelectedIndex = 0;

                ExportSystem.export = ExportSystem.WhatToExport.Chapters;

                chooseExportDialoguesButton.IsChecked = false;
                chooseExportCharactersButton.IsChecked = false;
            }
        }

        private void OnChooseExportDialoguesButton_Click(object sender, RoutedEventArgs e)
        {
            chooseExportChaptersPanel.Visibility = (bool)chooseExportDialoguesButton.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            withChapterNameCheckBox.Visibility = Visibility.Collapsed;
            characterDialoguesToExportHolder.Visibility = Visibility.Visible;

            extensions.Clear();

            AddToExport(false);

            extensions.Add(".txt");
            extensions.Add(".json");
            extensionComboBox.SelectedIndex = 0;

            ExportSystem.export = ExportSystem.WhatToExport.Dialogues;

            chooseExportChaptersButton.IsChecked = false;
            chooseExportCharactersButton.IsChecked = false;
        }

        private void OnChooseExportCharactersButton_Click(object sender, RoutedEventArgs e)
        {
            chooseExportChaptersPanel.Visibility = (bool)chooseExportCharactersButton.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            withChapterNameCheckBox.Visibility = Visibility.Collapsed;
            characterDialoguesToExportHolder.Visibility = Visibility.Collapsed;

            extensions.Clear();

            AddToExport(true);

            extensions.Add(".json");
            extensionComboBox.SelectedIndex = 0;

            ExportSystem.export = ExportSystem.WhatToExport.Characters;

            chooseExportChaptersButton.IsChecked = false;
            chooseExportDialoguesButton.IsChecked = false;
        }

        private void OnChaptersToExportList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chaptersToExport.Content = DropdownContent(chaptersToExportList);
        }

        private void OnCharacterDialoguesToList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            characterDialoguesToExport.Content = DropdownContent(characterDialoguesToExportList);
        }

        private void OnExportToLocationButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ChooseFileToExportAsync();
        }

        private void OnExportLocationFrame_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            _ = ChooseFileToExportAsync();
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            AppView.currentlyOpenedDialogue = null;
        }

        private void OnNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SomethingChanged(true);
        }

        private void ContentDialog_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && submitButton.IsEnabled)
                OnExportButton_Click(sender, new RoutedEventArgs());
        }

        bool isFlyoutOpen = false;
        private void Flyout_Opened(object sender, object e)
        {
            isFlyoutOpen = true;
        }

        private void Flyout_Closed(object sender, object e)
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

        private void OnExtensionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SomethingChanged(true);
        }
    }
}
