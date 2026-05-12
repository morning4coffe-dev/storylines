namespace Storylines.Services;

public class FileService : IFileService
{
    private readonly IFilePickerService _filePicker;

    public FileService(IFilePickerService filePicker)
    {
        _filePicker = filePicker;
    }

    public async Task WriteAsync(StorageFile file, string content)
    {
        if (file is null)
            throw new ArgumentNullException(nameof(file));

        await FileIO.WriteTextAsync(file, content ?? string.Empty);
    }

    public async Task<string> ReadAsync(StorageFile file)
    {
        return await FileIO.ReadTextAsync(file);
    }

    public async Task<StorageFile> PickFileForOpenAsync()
    {
        return await _filePicker.PickOpenFileAsync(new[] { ".srl", ".txt" });
    }

    public async Task<StorageFolder> PickFolderForSaveAsync()
    {
        return await _filePicker.PickFolderAsync();
    }

    public async Task<StorageFile> CreateFileAsync(StorageFolder folder, string fileName)
    {
        return await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
    }
}
