using Storylines.Scripts.Services.Interfaces;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Storylines.Scripts.Services
{
    public class FileService : IFileService
    {
        public async Task WriteAsync(StorageFile file, string content)
        {
            IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(content, BinaryStringEncoding.Utf8);
            await FileIO.WriteBufferAsync(file, buffer);
        }

        public async Task<string> ReadAsync(StorageFile file)
        {
            return await FileIO.ReadTextAsync(file);
        }

        public async Task<StorageFile> PickFileForOpenAsync()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeFilter.Add(".srl");
            picker.FileTypeFilter.Add(".txt");

            return await picker.PickSingleFileAsync();
        }

        public async Task<StorageFolder> PickFolderForSaveAsync()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            return await picker.PickSingleFolderAsync();
        }

        public async Task<StorageFile> CreateFileAsync(StorageFolder folder, string fileName)
        {
            return await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
        }
    }
}
