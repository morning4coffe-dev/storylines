using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.Services.Serializers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.UI.Xaml;

namespace Storylines.Services
{
    /// <summary>
    /// Periodically caches project state to local settings so unsaved work
    /// can be recovered after a crash. This does NOT affect the "Edited"
    /// indicator or the undo/redo system.
    /// </summary>
    internal static class RecoveryService
    {
        private const string RecoveryCacheFileName = "RecoveryCache.json";
        private const string RecoveryCacheTimestampKey = "RecoveryCacheTimestamp";
        private const string RecoveryProjectTokenKey = "RecoveryProjectToken";
        private const string RecoveryDocumentTypeKey = "RecoveryDocumentType";
        private const int CacheIntervalSeconds = 30;

        private static DispatcherTimer _cacheTimer;
        private static bool _isRunning;
        private static readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        public static void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            _cacheTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(CacheIntervalSeconds)
            };
            _cacheTimer.Tick += OnCacheTimer_Tick;
            _cacheTimer.Start();
        }

        public static void Stop()
        {
            if (_cacheTimer != null)
            {
                _cacheTimer.Tick -= OnCacheTimer_Tick;
                _cacheTimer.Stop();
                _cacheTimer = null;
            }
            _isRunning = false;
        }

        private static void OnCacheTimer_Tick(object sender, object e)
        {
            _ = CacheCurrentStateAsync();
        }

        public static void CacheCurrentState()
        {
            _ = CacheCurrentStateAsync();
        }

        public static async Task CacheCurrentStateAsync()
        {
            await _cacheLock.WaitAsync();

            try
            {
                var projectState = App.TryGetService<ProjectState>();
                if (projectState == null || projectState.Chapters.Count == 0 || !TimeTravelSystem.unSavedProgress)
                    return;

                var serializer = App.TryGetService<JsonSaveSerializer>();
                if (serializer == null)
                    return;

                var persistence = App.TryGetService<IProjectPersistenceService>();
                if (persistence == null)
                    return;

                var projectData = persistence.CollectProjectData();
                var json = serializer.Serialize(projectData);

                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    RecoveryCacheFileName,
                    CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteTextAsync(file, json);

                var settings = ApplicationData.Current.LocalSettings;
                settings.Values[RecoveryCacheTimestampKey] = DateTimeOffset.UtcNow.ToString("o");

                var currentProject = persistence.CurrentProject;
                if (!string.IsNullOrWhiteSpace(currentProject?.Token))
                    settings.Values[RecoveryProjectTokenKey] = currentProject.Token;
                else
                    settings.Values.Remove(RecoveryProjectTokenKey);

                settings.Values[RecoveryDocumentTypeKey] = currentProject?.file?.FileType ?? ".srl";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Recovery cache failed: {ex.Message}");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public static bool HasRecoveryData()
        {
            return ApplicationData.Current.LocalSettings.Values.ContainsKey(RecoveryCacheTimestampKey);
        }

        public static async Task<string> GetRecoveryJsonAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(RecoveryCacheFileName);
                return await FileIO.ReadTextAsync(file);
            }
            catch
            {
                return null;
            }
        }

        public static DateTimeOffset? GetRecoveryTimestamp()
        {
            if (ApplicationData.Current.LocalSettings.Values[RecoveryCacheTimestampKey] is string ts
                && DateTimeOffset.TryParse(ts, out var result))
                return result;
            return null;
        }

        public static string GetRecoveryProjectToken()
        {
            return ApplicationData.Current.LocalSettings.Values[RecoveryProjectTokenKey] as string;
        }

        public static string GetRecoveryDocumentType()
        {
            return ApplicationData.Current.LocalSettings.Values[RecoveryDocumentTypeKey] as string ?? ".srl";
        }

        public static void ClearRecoveryData()
        {
            _ = ClearRecoveryDataAsync();
        }

        public static async Task ClearRecoveryDataAsync()
        {
            await _cacheLock.WaitAsync();

            try
            {
                ApplicationData.Current.LocalSettings.Values.Remove(RecoveryCacheTimestampKey);
                ApplicationData.Current.LocalSettings.Values.Remove(RecoveryProjectTokenKey);
                ApplicationData.Current.LocalSettings.Values.Remove(RecoveryDocumentTypeKey);

                var existingFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(RecoveryCacheFileName);
                if (existingFile != null)
                    await existingFile.DeleteAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Recovery cache cleanup failed: {ex.Message}");
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    }
}
