using Storylines.Views.Controls;
using Storylines.Views.Dialogs;
using Storylines.Views.Pages;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Services.Modes;
using Storylines.Models;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml.Input;

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
        private static WindowContext WindowContext => App.GetService<WindowContext>();
        private static AppView AppView => WindowContext.AppView;
        private static CharactersPage CharactersPage => WindowContext.CharactersPage;

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
            new("shortcutToggleDictation", ShortcutScope.MainPage, "M", useShift: true),
            new("shortcutTypewriterMode", ShortcutScope.MainPage, "W", useShift: true),
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
            var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
            return (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        }

        private static bool IsShiftKeyPressed()
        {
            var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
            return (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        }

        public static void Check(KeyRoutedEventArgs e)
        {
            if (IsCtrlKeyPressed())
            {
                if (IsShiftKeyPressed())
                {
                    switch (AppView.page)
                    {
                        case AppView.Pages.MainPage:
                            switch (e.Key)
                            {
                                case Windows.System.VirtualKey.D: WindowContext.ChapterText.DialoguesOnOff(!(bool)WindowContext.CommandBar.dialoguesEnableButton.IsChecked); break;
                                case Windows.System.VirtualKey.M:
                                    App.GetService<Storylines.ViewModels.SpeechHubViewModel>().ToggleDictationCommand.Execute(null);
                                    break;
                                case Windows.System.VirtualKey.W:
                                    bool tw = !(WindowContext.CommandBar.typewriterModeButton.IsChecked == true);
                                    WindowContext.CommandBar.typewriterModeButton.IsChecked = tw;
                                    WindowContext.ChapterText.IsTypewriterModeActive = tw;
                                    break;
                                case Windows.System.VirtualKey.B:
                                    if (WindowContext.ChapterText.chapterTextCommandBar.IsEnabled)
                                        WindowContext.ChapterText.BoldChapterTextBox();
                                    break;
                                case Windows.System.VirtualKey.I:
                                    if (WindowContext.ChapterText.chapterTextCommandBar.IsEnabled)
                                        WindowContext.ChapterText.ItalicChapterTextBox();
                                    break;
                                case Windows.System.VirtualKey.U:
                                    if (WindowContext.ChapterText.chapterTextCommandBar.IsEnabled)
                                        WindowContext.ChapterText.UnderlineChapterTextBox();
                                    break;
                                case Windows.System.VirtualKey.T:
                                    if (WindowContext.ChapterText.chapterTextCommandBar.IsEnabled)
                                        WindowContext.ChapterText.StrikethroughChapterTextBox();
                                    break;
                            }
                            break;
                        case AppView.Pages.Characters:
                            break;
                        case AppView.Pages.Settings:
                            break;
                    }

                    switch (e.Key)
                    {
                        case Windows.System.VirtualKey.S: App.GetService<IProjectPersistenceService>().SaveCopy(); break;
                    }
                }
                else
                {
                    switch (AppView.page)
                    {
                        case AppView.Pages.MainPage:
                            switch (e.Key)
                            {
                                case Windows.System.VirtualKey.Q:
                                    if (WindowContext.ChapterList.canAdd && App.GetService<IDialogService>().CurrentDialog is null)
                                        App.TryGetService<IChapterWorkflowService>()?.OpenCreateChapterDialog(); break;
                                case Windows.System.VirtualKey.Delete:
                                    if (WindowContext.ChapterList.listView.SelectedItem is Chapter selectedChapter && App.GetService<IDialogService>().CurrentDialog is null)
                                        App.TryGetService<IChapterWorkflowService>()?.DeleteChapter(selectedChapter.Token); break;

                                case Windows.System.VirtualKey.E:
                                    if (WindowContext.CommandBar.exportButton.IsEnabled && App.GetService<IDialogService>().CurrentDialog is null)
                                        App.GetService<IDialogService>().OpenExportDialogue(); break;
                                case Windows.System.VirtualKey.R:
                                    App.GetService<Storylines.ViewModels.SpeechHubViewModel>().StartReadAloudCommand.Execute(null);
                                    break;
                                case Windows.System.VirtualKey.F: WindowContext.ChapterText.OpenSearchAndReplace(); break;
                                case Windows.System.VirtualKey.H: WindowContext.ChapterText.OpenSearchAndReplace(); break;
                                case Windows.System.VirtualKey.PageUp:
                                    if (WindowContext.ChapterList.listView.SelectedItem is not null && WindowContext.ChapterList.listView.IsEnabled && WindowContext.ChapterList.listView.SelectedIndex > 0)
                                        WindowContext.ChapterList.listView.SelectedIndex -= 1;
                                    break;
                                case Windows.System.VirtualKey.PageDown:
                                    if (WindowContext.ChapterList.listView.SelectedItem is not null && WindowContext.ChapterList.listView.IsEnabled)
                                        if (WindowContext.ChapterList.listView.SelectedIndex >= 0 && WindowContext.ChapterList.listView.SelectedIndex < (WindowContext.ChapterList.listView.Items.Count - 1))
                                            WindowContext.ChapterList.listView.SelectedIndex += 1;
                                        else
                                        if (WindowContext.ChapterList.listView.Items.Count == WindowContext.ChapterList.listView.SelectedIndex + 1 &&
                                            App.GetService<Storylines.Services.Interfaces.IPreferencesService>().Get(SettingsValueStrings.OnPageDownNewChapterEnabled, true))
                                        {
                                            App.TryGetService<IChapterWorkflowService>()?.CreateChapterFromInput(ProjectState.GetRandomChapterName());
                                        }
                                    break;
                                case Windows.System.VirtualKey.Z:
                                {
                                    var undoSvc = App.GetService<Services.Interfaces.IUndoRedoService>();
                                    if (undoSvc.CanUndo("chapters"))
                                        undoSvc.Undo("chapters");
                                    break;
                                }
                                case Windows.System.VirtualKey.Y:
                                {
                                    var undoSvc = App.GetService<Services.Interfaces.IUndoRedoService>();
                                    if (undoSvc.CanRedo("chapters"))
                                        undoSvc.Redo("chapters");
                                    break;
                                }
                            }
                            break;
                        case AppView.Pages.Characters:
                            switch (e.Key)
                            {
                                case Windows.System.VirtualKey.Q:
                                    if (CharactersPage.isAddEnabled)
                                        CharactersPage.Add(); break;
                                case Windows.System.VirtualKey.Delete:
                                    if (CharactersPage.isRemoveEnabled)
                                        CharactersPage.Remove(); break;
                                case Windows.System.VirtualKey.E:
                                    if (CharactersPage.exportButton.IsEnabled)
                                        App.GetService<IDialogService>().OpenExportDialogue(ExportTarget.Characters); break;
                                case Windows.System.VirtualKey.N:
                                    if (CharactersPage.editButton.IsEnabled)
                                        CharactersPage.EnableEditMode(!(bool)CharactersPage.editButton.IsChecked); break;

                                case Windows.System.VirtualKey.Z:
                                {
                                    var undoSvc = App.GetService<Services.Interfaces.IUndoRedoService>();
                                    if (undoSvc.CanUndo("characters"))
                                        undoSvc.Undo("characters");
                                    break;
                                }
                                case Windows.System.VirtualKey.Y:
                                {
                                    var undoSvc = App.GetService<Services.Interfaces.IUndoRedoService>();
                                    if (undoSvc.CanRedo("characters"))
                                        undoSvc.Redo("characters");
                                    break;
                                }
                            }
                            break;
                        case AppView.Pages.Settings:
                            break;
                    }

                    switch (e.Key)
                    {
                        case Windows.System.VirtualKey.S: App.GetService<IProjectPersistenceService>().Save(); break;
                        case Windows.System.VirtualKey.I:
                            {
                                var modeSvc = App.TryGetService<EditorModeService>();
                                    bool allowsSettings = modeSvc?.Current.Chrome.AllowsSettingsShortcut ?? true;
                                if (AppView.page != AppView.Pages.Settings && allowsSettings)
                                    AppView.ChangePage(AppView.Pages.Settings);
                                break;
                            }
                    }
                }
            }
        }
    }
}
