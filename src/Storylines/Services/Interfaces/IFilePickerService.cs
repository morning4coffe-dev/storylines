
namespace Storylines.Services.Interfaces;

public sealed class SaveFilePickerRequest
{
    public string SuggestedFileName { get; set; }
    public string DisplayTypeName { get; set; }
    public IReadOnlyList<string> FileExtensions { get; set; } = Array.Empty<string>();
}

public interface IFilePickerService
{
    Task<StorageFolder> PickFolderAsync();
    Task<StorageFile> PickSaveFileAsync(SaveFilePickerRequest request);
    Task<StorageFile> PickOpenFileAsync(IReadOnlyList<string> fileExtensions);
}