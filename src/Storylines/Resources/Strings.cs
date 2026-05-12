
namespace Storylines.Resources;

public class ExportDialogue
{
    private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();

    public static string Title { get => _resources.GetString("exportTitle"); }
    public static string OptionChapters { get => _resources.GetString("exportOptionChapters"); }
    public static string OptionCharacters { get => _resources.GetString("exportOptionCharacters"); }
    public static string OptionDialogues { get => _resources.GetString("exportOptionDialogues"); }
    public static string FileNameCollisionError { get => _resources.GetString("exportFileNameCollisionError"); }

    public static string FileName { get => _resources.GetString("exportFileName"); }
    public static string FileNamePlaceholder { get => _resources.GetString("exportFileNamePlaceholder"); }

    public static string FileLocation { get => _resources.GetString("exportFileLocation"); }
    public static string FileLocationPlaceholder { get => _resources.GetString("exportFileLocationPlaceholder"); }

    public static string IncludeChapterName { get => _resources.GetString("exportIncludeChapterName"); }
    public static string ChaptersToExport { get => _resources.GetString("exportChaptersToExport"); }
    public static string CharactersToExport { get => _resources.GetString("exportCharactersToExport"); }

    public static string None { get => _resources.GetString("exportNone"); }
    public static string All { get => _resources.GetString("exportAll"); }

    public static string Submit { get => _resources.GetString("exportSubmit"); }
}

public class SaveDialogue
{
    private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();

    public static string Title(Storylines.Views.Dialogs.SaveDialogue.Type type) 
    {
        switch (type)
        {
            case Storylines.Views.Dialogs.SaveDialogue.Type.Save:
                return _resources.GetString("saveTitle");
            case Storylines.Views.Dialogs.SaveDialogue.Type.SaveCopy:
                return _resources.GetString("saveCopyTitle");
            default:
                return null;
        }
    }

    public static string ProjectName { get => _resources.GetString("saveProjectName"); }
    public static string ProjectNamePlaceholder { get => _resources.GetString("saveProjectNamePlaceholder"); }
    public static string Submit { get => _resources.GetString("saveTitle"); }
}
