
namespace Storylines.ViewModels;

/// <summary>
/// ViewModel for the ProjectFileInfoDialogue.
/// Loads and formats file metadata for display.
/// </summary>
public partial class ProjectFileInfoViewModel : ObservableObject
{
    private readonly IProjectPersistenceService _persistence;
    private readonly ILogger _logger;
    private readonly ResourceLoader _resources;

    [ObservableProperty] private string _fileInfoText = string.Empty;

    public ProjectFileInfoViewModel(
        IProjectPersistenceService persistence,
        ILogger logger)
    {
        _persistence = persistence;
        _logger = logger;
        _resources = ResourceLoader.GetForViewIndependentUse();
    }

    [RelayCommand]
    public async Task LoadFileInfoAsync()
    {
        var project = _persistence?.CurrentProject;
        StorageFile file = project?.file;

        string unavailable = _resources.GetString("projectFileUnavailable");
        string notSaved = _resources.GetString("projectFileNotSaved");

        string fileName = file?.Name;
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = !string.IsNullOrWhiteSpace(project?.ProjectName) ? project.ProjectName : !string.IsNullOrWhiteSpace(project?.Name) ? project.Name : unavailable;

        string location = file?.Path;
        if (string.IsNullOrWhiteSpace(location))
            location = !string.IsNullOrWhiteSpace(project?.Path) ? project.Path : project is null ? unavailable : notSaved;

        string created = unavailable;
        string modified = unavailable;
        string size = unavailable;
        string type = unavailable;

        if (file is not null)
        {
            try
            {
                var basicProperties = await file.GetBasicPropertiesAsync();

                created = FormatDate(file.DateCreated, unavailable);
                modified = FormatDate(basicProperties.DateModified, unavailable);
                size = FormatFileSize(basicProperties.Size);
                type = string.IsNullOrWhiteSpace(file.FileType) ? unavailable : file.FileType;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to load project file metadata: {ex.Message}");
            }
        }

        FileInfoText =
            $"{_resources.GetString("projectFileNameLabel")} {fileName}\n" +
            $"{_resources.GetString("projectFileLocationLabel")} {location}\n" +
            $"{_resources.GetString("projectFileCreatedLabel")} {created}\n" +
            $"{_resources.GetString("projectFileModifiedLabel")} {modified}\n" +
            $"{_resources.GetString("projectFileSizeLabel")} {size}\n" +
            $"{_resources.GetString("projectFileTypeLabel")} {type}";
    }

    private static string FormatDate(DateTimeOffset date, string fallback)
    {
        return date == default
            ? fallback
            : date.ToString("g", System.Globalization.CultureInfo.CurrentCulture);
    }

    private static string FormatFileSize(ulong size)
    {
        if (size == 0)
            return "0 B";

        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = size;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        string formattedValue = value >= 10 || unitIndex == 0
            ? value.ToString("0", System.Globalization.CultureInfo.CurrentCulture)
            : value.ToString("0.#", System.Globalization.CultureInfo.CurrentCulture);

        return $"{formattedValue} {units[unitIndex]}";
    }
}
