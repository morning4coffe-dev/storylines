using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Storylines.Services
{
    public class FilePickerService : IFilePickerService
    {
        private readonly WindowContext _windowContext;

        public FilePickerService(WindowContext windowContext)
        {
            _windowContext = windowContext;
        }

        public async Task<StorageFolder> PickFolderAsync()
        {
            var picker = new FolderPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeFilter.Add("*");

            WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowContext.Hwnd);

            return await picker.PickSingleFolderAsync();
        }

        public async Task<StorageFile> PickSaveFileAsync(SaveFilePickerRequest request)
        {
            if (request?.FileExtensions == null || request.FileExtensions.Count == 0)
                return null;

            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = request.SuggestedFileName,
            };

            var displayTypeName = !string.IsNullOrWhiteSpace(request.DisplayTypeName)
                ? request.DisplayTypeName
                : request.FileExtensions[0].TrimStart('.').ToUpperInvariant();

            picker.FileTypeChoices.Add(displayTypeName, request.FileExtensions.Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowContext.Hwnd);

            return await picker.PickSaveFileAsync();
        }

        public async Task<StorageFile> PickOpenFileAsync(IReadOnlyList<string> fileExtensions)
        {
            if (fileExtensions == null || fileExtensions.Count == 0)
                return null;

            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };

            foreach (var extension in fileExtensions.Where(extension => !string.IsNullOrWhiteSpace(extension)).Distinct(StringComparer.OrdinalIgnoreCase))
                picker.FileTypeFilter.Add(extension);

            WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowContext.Hwnd);

            return await picker.PickSingleFileAsync();
        }
    }
}
