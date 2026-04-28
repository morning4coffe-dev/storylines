using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Views.Pages;
using Storylines.Services;
using Storylines.Services.Modes;
using Storylines.Models;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;

namespace Storylines.Helpers
{
    public enum ShortcutScope
    {
        Global,
        MainPage,
        CharactersPage
    }

    public class ShortcutDefinition
    {
        private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();

        public string DescriptionKey { get; }
        public bool UseCtrl { get; }
        public bool UseShift { get; }
        public string KeyDisplayName { get; }
        public ShortcutScope Scope { get; }

        public ShortcutDefinition(string descriptionKey, ShortcutScope scope, string keyDisplayName, bool useCtrl = true, bool useShift = false)
        {
            DescriptionKey = descriptionKey;
            Scope = scope;
            KeyDisplayName = keyDisplayName;
            UseCtrl = useCtrl;
            UseShift = useShift;
        }

        public string Description => _resources.GetString(DescriptionKey);
        public string ShortcutText => $"{(UseCtrl ? "Ctrl+" : "")}{(UseShift ? "Shift+" : "")}{KeyDisplayName}";
    }

    class ShortcutManager
    {
        private static ProjectState ProjectState => App.GetService<ProjectState>();

        public static IReadOnlyList<ShortcutDefinition> Shortcuts { get; } = new List<ShortcutDefinition>
        {
            // Global
            new("shortcutSave", ShortcutScope.Global, "S"),
            new("shortcutSaveCopy", ShortcutScope.Global, "S", useShift: true),
            new("shortcutExport", ShortcutScope.Global, "E"),
            new("shortcutUndo", ShortcutScope.Global, "Z"),
            new("shortcutRedo", ShortcutScope.Global, "Y"),
            new("shortcutOpenSettings", ShortcutScope.Global, "I"),
            // MainPage
            new("shortcutAddChapter", ShortcutScope.MainPage, "Q"),
            new("shortcutRemoveChapter", ShortcutScope.MainPage, "Del"),
            new("shortcutChapterAbove", ShortcutScope.MainPage, "PageUp"),
            new("shortcutChapterBelow", ShortcutScope.MainPage, "PageDown"),
            new("shortcutReadAloud", ShortcutScope.MainPage, "R"),
            new("shortcutSearch", ShortcutScope.MainPage, "F"),
            new("shortcutSearchAndReplace", ShortcutScope.MainPage, "H"),
            new("shortcutToggleDialogueMode", ShortcutScope.MainPage, "D", useShift: true),
            new("shortcutBold", ShortcutScope.MainPage, "B", useShift: true),
            new("shortcutItalic", ShortcutScope.MainPage, "I", useShift: true),
            new("shortcutUnderline", ShortcutScope.MainPage, "U", useShift: true),
            new("shortcutStrikethrough", ShortcutScope.MainPage, "T", useShift: true),
            // CharactersPage
            new("shortcutAddCharacter", ShortcutScope.CharactersPage, "Q"),
            new("shortcutRemoveCharacter", ShortcutScope.CharactersPage, "Del"),
            new("shortcutToggleEditMode", ShortcutScope.CharactersPage, "N"),
        };

        public static IEnumerable<ShortcutDefinition> GetShortcuts(ShortcutScope scope) =>
            Shortcuts.Where(s => s.Scope == scope);

        private static bool IsCtrlKeyPressed()
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control);
            return (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        private static bool IsShiftKeyPressed()
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Shift);
            return (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        public static void Check(KeyEventArgs e)
        {
            if (IsCtrlKeyPressed())
            {
                if (IsShiftKeyPressed())
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
                                        ProjectState.RemoveChapter((MainPage.ChapterList.listView.SelectedItem as Chapter).Token); break;

                                case Windows.System.VirtualKey.E:
                                    if (MainPage.CommandBar.exportButton.IsEnabled && AppView.currentlyOpenedDialogue == null)
                                        ExportDialogue.Open(default); break;
                                case Windows.System.VirtualKey.R: MainPage.CommandBar.ReadAloud(); break;
                                case Windows.System.VirtualKey.F: MainPage.ChapterText.EnableSeach(); break;
                                case Windows.System.VirtualKey.H: MainPage.ChapterText.OpenSearchAndReplace(); break;
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
                                            ProjectState.AddChapterFromCreator(ProjectState.Chapters.Count + 1, Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("chapterWithoutName"));
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
                                        ExportDialogue.Open(ExportService.WhatToExport.Characters); break;
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
                            {
                                var modeSvc = App.TryGetService<EditorModeService>();
                                    bool allowsSettings = modeSvc?.Current.Chrome.AllowsSettingsShortcut ?? true;
                                if (AppView.current.page != AppView.Pages.Settings && allowsSettings)
                                    AppView.current.ChangePage(AppView.Pages.Settings);
                                break;
                            }
                    }
                    //case Windows.System.VirtualKey.F: MainPage.ChangePage(MainPage.Pages.Settings); break;   /// search
                }
            }
        }
    }
}
