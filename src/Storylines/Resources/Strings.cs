using Windows.ApplicationModel.Resources;
using static Storylines.Components.DialogueWindows.SaveDialogue;

namespace Storylines.Resources
{
    public class ShortcutsDialogue
    {
        private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse("ShortcutsDialogue");

        public static string Title { get => _resources.GetString("Title"); }
        //Global
        public static string Global { get => _resources.GetString("Global"); }
        public static string Save { get => _resources.GetString("Save"); }
        public static string SaveCopy { get => _resources.GetString("SaveCopy"); }
        public static string Export { get => _resources.GetString("Export"); }
        public static string Undo { get => _resources.GetString("Undo"); }
        public static string Redo { get => _resources.GetString("Redo"); }

        //MainPage
        public static string MainPage { get => _resources.GetString("MainPage"); }
        public static string AddChapter { get => _resources.GetString("AddChapter"); }
        public static string RemoveChapter { get => _resources.GetString("RemoveChapter"); }
        public static string ChapterAbove { get => _resources.GetString("ChapterAbove"); }
        public static string ChapterBelow { get => _resources.GetString("ChapterBelow"); }
        public static string ReadAloud { get => _resources.GetString("ReadAloud"); }
        public static string Search { get => _resources.GetString("Search"); }
        public static string Bold { get => _resources.GetString("Bold"); }
        public static string Italic { get => _resources.GetString("Italic"); }
        public static string Underline { get => _resources.GetString("Underline"); }
        public static string Strikethrough { get => _resources.GetString("Strikethrough"); }

        //CharactersPage
        public static string CharactersPage { get => _resources.GetString("CharactersPage"); }
        public static string AddCharacter { get => _resources.GetString("AddCharacter"); }
        public static string RemoveCharacter { get => _resources.GetString("RemoveCharacter"); }
    }

    public class ExportDialogue
    {
        private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse("ExportDialogue");

        public static string Title { get => _resources.GetString("Title"); }
        public static string OptionChapters { get => _resources.GetString("OptionChapters"); }
        public static string OptionCharacters { get => _resources.GetString("OptionCharacters"); }
        public static string OptionDialogues { get => _resources.GetString("OptionDialogues"); }
        public static string FileNameCollisionError { get => _resources.GetString("FileNameCollisionError"); }

        public static string FileName { get => _resources.GetString("FileName"); }
        public static string FileNamePlaceholder { get => _resources.GetString("FileNamePlaceholder"); }

        public static string FileLocation { get => _resources.GetString("FileLocation"); }
        public static string FileLocationPlaceholder { get => _resources.GetString("FileLocationPlaceholder"); }

        public static string IncludeChapterName { get => _resources.GetString("IncludeChapterName"); }
        public static string ChaptersToExport { get => _resources.GetString("ChaptersToExport"); }

        public static string None { get => _resources.GetString("None"); }
        public static string All { get => _resources.GetString("All"); }

        public static string Submit { get => _resources.GetString("Submit"); }
    }

    public class SaveDialogue //switch to separate .resw too
    {
        private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse("SaveDialogue");//SaveDialogue


        public static string Title(Type type) 
        {
            switch (type)
            {
                case Type.Save:
                    return _resources.GetString("SaveTitle");
                case Type.SaveCopy:
                    return _resources.GetString("SaveCopyTitle");
                default:
                    return null;
            }
        }

        public static string ProjectName { get => _resources.GetString("ProjectName"); }
        public static string ProjectNamePlaceholder { get => _resources.GetString("ProjectNamePlaceholder"); }
        public static string Submit { get => _resources.GetString("SaveTitle"); }
    }
}
