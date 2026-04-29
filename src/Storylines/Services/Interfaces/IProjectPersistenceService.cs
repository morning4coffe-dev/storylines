using Storylines.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Storylines.Services.Interfaces
{
    public interface IProjectPersistenceService
    {
        Dictionary<string, string> SavedValues { get; }

        ProjectFile CurrentProject { get; set; }

        void Save();

        Task SaveAsync();

        void SaveCopy();

        void SaveAndExitOrClearAll(bool exit);

        Task SaveAndExitOrClearAllAsync(bool exit);

        void CancelPendingAfterSaveAction();

        ProjectData CollectProjectData();

        Task OpenFileExplorerSaveAsync(string fileName);

        Task NewFileAsync(StorageFolder folder, string fullFileName);

        void Load(ProjectFile project);

        Task LoadAsync(ProjectFile project);

        Task<bool> TryRestoreRecoveryAsync();

        void DefaultLaunch(IStorageItem storageItem);

        void EnableAutosave();

        void DisableAutosave();

        void RefreshAutosave();
    }
}