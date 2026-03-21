using System.Threading.Tasks;
using Windows.Storage;

namespace Storylines.Scripts.Services.Interfaces
{
    public interface IFileService
    {
        Task WriteAsync(StorageFile file, string content);
        Task<string> ReadAsync(StorageFile file);
        Task<StorageFile> PickFileForOpenAsync();
        Task<StorageFolder> PickFolderForSaveAsync();
        Task<StorageFile> CreateFileAsync(StorageFolder folder, string fileName);
    }
}
