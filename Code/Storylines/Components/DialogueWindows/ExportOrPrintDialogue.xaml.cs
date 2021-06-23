using System;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class ExportOrPrintDialogue : ContentDialog
    {
        public static ExportOrPrintDialogue exportOrPrintDialogue;

        //private bool isPrint;

        public ExportOrPrintDialogue()
        {
            this.InitializeComponent();
            exportOrPrintDialogue = this;

            exportOrPrintDialogue.RequestedTheme = MainPage.mainPage.RequestedTheme;

            MainPage.currentlyOpenedDialogue = exportOrPrintDialogue;

            exportOrPrintButton.IsEnabled = MainPage.chapterList.chapters.Count > 0 ? true : false;
            //if (isPrint)
        }

        public static async System.Threading.Tasks.Task Open(bool isPrint)
        {
            //exportOrPrintDialogue.isPrint = isPrint;
            await new ExportOrPrintDialogue().ShowAsync();
        }

        private void ContentDialog_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            chaptersToExportList.Items.Clear();

            for (int i = 0; i < MainPage.chapterList.chapters.Count; i++)
            {
                chaptersToExportList.Items.Add(new ListViewItem() { Content = MainPage.chapterList.chapters[i].name, IsSelected = true });
            }
        }

        private void OnExportOrPrintButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            System.Collections.Generic.List<int> selectedIndexes = new System.Collections.Generic.List<int>();

            for (int i = 0; i < chaptersToExportList.SelectedItems.Count; i++)
            {
                selectedIndexes.Add(chaptersToExportList.Items.IndexOf(chaptersToExportList.SelectedItems[i]));
            }

            MainPage.saveSystem.Export(selectedIndexes, (bool)withChapterNameCheckBox.IsChecked);
            this.Hide();
        }

        private void OnCancelButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Hide();
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            MainPage.currentlyOpenedDialogue = null;
        }

        private void OnChaptersToExportList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            exportOrPrintButton.IsEnabled = true;

            if (chaptersToExportList.SelectedItems.Count == chaptersToExportList.Items.Count)
            {
                chaptersToExport.Content = "All";
            }
            else
            if (chaptersToExportList.SelectedItems.Count == 0)
            {
                chaptersToExport.Content = "None";
                exportOrPrintButton.IsEnabled = false;
            }
            else
            {
                chaptersToExport.Content = "";

                for (int i = 0; i < chaptersToExportList.SelectedItems.Count; i++)
                {
                    chaptersToExport.Content += chaptersToExport.Content.ToString() != "" ? $", {(chaptersToExportList.SelectedItems[i] as ListViewItem).Content}" : $"{(chaptersToExportList.SelectedItems[i] as ListViewItem).Content}";
                }
            }

        }
    }
}
