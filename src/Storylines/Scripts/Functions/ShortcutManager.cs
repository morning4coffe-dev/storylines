using Storylines.Components;
using Storylines.Components.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;

namespace Storylines.Scripts.Functions
{
    class ShortcutManager
    {
        private static bool IsCtrlKeyPressed()
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control);
            return (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        private static bool IsShiftlKeyPressed()
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Shift);
            return (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        public static void Check(KeyEventArgs e)
        {
            if (IsCtrlKeyPressed())
            {
                if (IsShiftlKeyPressed())
                {
                    switch (AppView.current.page)
                    {
                        case AppView.Pages.MainPage:
                            switch (e.VirtualKey)
                            {
                                case Windows.System.VirtualKey.D: MainPage.ChapterText.DialoguesOnOff(!(bool)MainPage.CommandBar.dialoguesEnableButton.IsChecked); break;
                                case Windows.System.VirtualKey.B:
                                    if (MainPage.ChapterText.chapterTextCommandBar.IsEnabled)
                                        MainPage.ChapterText.BoldChapterTextBox();
                                    break;
                                case Windows.System.VirtualKey.I:
                                    if (MainPage.ChapterText.chapterTextCommandBar.IsEnabled)
                                        MainPage.ChapterText.ItalicChapterTextBox();
                                    break;
                                case Windows.System.VirtualKey.U:
                                    if (MainPage.ChapterText.chapterTextCommandBar.IsEnabled)
                                        MainPage.ChapterText.UnderlineChapterTextBox();
                                    break;
                                case Windows.System.VirtualKey.T:
                                    if (MainPage.ChapterText.chapterTextCommandBar.IsEnabled)
                                        MainPage.ChapterText.StrikethroughChapterTextBox();
                                    break;
                            }
                            break;
                        case AppView.Pages.Characters:
                            break;
                        case AppView.Pages.Settings:
                            break;
                    }

                    switch (e.VirtualKey)
                    {
                        case Windows.System.VirtualKey.S: SaveSystem.SaveCopy(); break;
                    }
                }
                else
                {
                    switch (AppView.current.page)
                    {
                        case AppView.Pages.MainPage:
                            switch (e.VirtualKey)
                            {
                                case Windows.System.VirtualKey.Q:
                                    if (MainPage.ChapterList.canAdd && AppView.currentlyOpenedDialogue == null)
                                        ChapterCreatorOrRenamer.Open(null, false); break;
                                case Windows.System.VirtualKey.Delete:
                                    if (MainPage.ChapterList.listView.SelectedItem != null && AppView.currentlyOpenedDialogue == null)
                                        Chapter.Remove((MainPage.ChapterList.listView.SelectedItem as Chapter).token); break;

                                case Windows.System.VirtualKey.E:
                                    if (MainPage.CommandBar.exportButton.IsEnabled && AppView.currentlyOpenedDialogue == null)
                                        ExportDialogue.Open(default); break;
                                case Windows.System.VirtualKey.R: MainPage.CommandBar.ReadAloud(); break;
                                case Windows.System.VirtualKey.F: MainPage.ChapterText.EnableSeach(); break;
                                case Windows.System.VirtualKey.PageUp:
                                    if (MainPage.ChapterList.listView.SelectedItem != null && MainPage.ChapterList.listView.IsEnabled && MainPage.ChapterList.listView.SelectedIndex > 0)
                                        MainPage.ChapterList.listView.SelectedIndex -= 1;
                                    break;
                                case Windows.System.VirtualKey.PageDown:
                                    if (MainPage.ChapterList.listView.SelectedItem != null && MainPage.ChapterList.listView.IsEnabled)
                                        if (MainPage.ChapterList.listView.SelectedIndex >= 0 && MainPage.ChapterList.listView.SelectedIndex < (MainPage.ChapterList.listView.Items.Count - 1))
                                            MainPage.ChapterList.listView.SelectedIndex += 1;
                                        else
                                        if (MainPage.ChapterList.listView.Items.Count == MainPage.ChapterList.listView.SelectedIndex + 1 &&
                                            System.Convert.ToBoolean(Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.OnPageDownNewChapterEnabled]))
                                        {
                                            Chapter.AddFromCreator(Chapter.chapters.Count + 1, Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("chapterWithoutName"));
                                            MainPage.ChapterList.listView.SelectedIndex += 1;
                                        }
                                    break;
                                case Windows.System.VirtualKey.Z:
                                    if (MainPage.CommandBar.undoButton.IsEnabled)
                                        TimeTravelChapter.Undo(); break;
                                case Windows.System.VirtualKey.Y:
                                    if (MainPage.CommandBar.redoButton.IsEnabled)
                                        TimeTravelChapter.Redo(); break;
                            }
                            break;
                        case AppView.Pages.Characters:
                            switch (e.VirtualKey)
                            {
                                case Windows.System.VirtualKey.Q:
                                    if (CharactersPage.current.isAddEnabled)
                                        CharactersPage.current.Add(); break;
                                case Windows.System.VirtualKey.Delete:
                                    if (CharactersPage.current.isRemoveEnabled)
                                        CharactersPage.current.Remove(); break;
                                case Windows.System.VirtualKey.E:
                                    if (CharactersPage.current.exportButton.IsEnabled)
                                        ExportDialogue.Open(ExportSystem.WhatToExport.Characters); break;
                                case Windows.System.VirtualKey.N:
                                    if (CharactersPage.current.editButton.IsEnabled)
                                        CharactersPage.current.EnableEditMode(!(bool)CharactersPage.current.editButton.IsChecked); break;

                                case Windows.System.VirtualKey.Z:
                                    if (CharactersPage.current.undoButton.IsEnabled)
                                        TimeTravelCharacter.Undo(); break;
                                case Windows.System.VirtualKey.Y:
                                    if (CharactersPage.current.redoButton.IsEnabled)
                                        TimeTravelCharacter.Redo(); break;
                            }
                            break;
                        case AppView.Pages.Settings:
                            break;
                    }

                    switch (e.VirtualKey)
                    {
                        case Windows.System.VirtualKey.S: SaveSystem.Save(); break;
                        case Windows.System.VirtualKey.I:
                            if (AppView.current.page != AppView.Pages.Settings && MainPage.FocusMode == null)
                                AppView.current.ChangePage(AppView.Pages.Settings); break;
                    }
                    //case Windows.System.VirtualKey.F: MainPage.ChangePage(MainPage.Pages.Settings); break;   /// search
                }
            }
        }
    }
}
