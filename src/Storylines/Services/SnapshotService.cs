using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.Services.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storylines.Services
{
    /// <summary>
    /// In-memory rolling snapshot history. Captures a serialized copy of the current project on
    /// demand and lets the user browse / restore prior states. The on-disk variant — needed to
    /// survive process restarts — will replace this implementation in a follow-up; the interface
    /// is the stable seam.
    /// </summary>
    internal sealed class SnapshotService : ISnapshotService
    {
        private readonly ProjectState _projectState;
        private readonly JsonSaveSerializer _serializer;
        private readonly IProjectPersistenceService _persistence;
        private readonly ILogger _logger;
        private readonly LinkedList<StoredSnapshot> _snapshots = new LinkedList<StoredSnapshot>();
        private readonly object _gate = new object();

        public SnapshotService(
            ProjectState projectState,
            JsonSaveSerializer serializer,
            IProjectPersistenceService persistence,
            ILogger logger)
        {
            _projectState = projectState;
            _serializer = serializer;
            _persistence = persistence;
            _logger = logger;
        }

        public int Capacity { get; set; } = 20;

        public Task<IReadOnlyList<SnapshotEntry>> GetHistoryAsync()
        {
            lock (_gate)
            {
                IReadOnlyList<SnapshotEntry> result = _snapshots
                    .Select(s => s.Entry)
                    .OrderByDescending(e => e.Captured)
                    .ToList();
                return Task.FromResult(result);
            }
        }

        public Task CaptureAsync(string reason)
        {
            try
            {
                var data = _persistence.CollectProjectData();
                if (data == null)
                    return Task.CompletedTask;

                var json = _serializer.Serialize(data);
                var entry = new SnapshotEntry(
                    Guid.NewGuid().ToString("N"),
                    DateTimeOffset.UtcNow,
                    reason ?? string.Empty,
                    data.Chapters?.Count ?? 0,
                    data.Characters?.Count ?? 0,
                    json.Length);

                lock (_gate)
                {
                    _snapshots.AddLast(new StoredSnapshot(entry, json));
                    EvictOverflowLocked();
                }
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Snapshot capture failed: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task<bool> RestoreAsync(string snapshotId)
        {
            if (string.IsNullOrEmpty(snapshotId))
                return Task.FromResult(false);

            StoredSnapshot match;
            lock (_gate)
            {
                match = _snapshots.FirstOrDefault(s => s.Entry.Id == snapshotId);
            }
            if (match == null)
                return Task.FromResult(false);

            try
            {
                var data = _serializer.Deserialize(match.SerializedJson);
                if (data == null)
                    return Task.FromResult(false);

                ApplyToProjectState(data);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Snapshot restore failed for {snapshotId}: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeleteAsync(string snapshotId)
        {
            if (string.IsNullOrEmpty(snapshotId))
                return Task.FromResult(false);

            lock (_gate)
            {
                var node = _snapshots.First;
                while (node != null)
                {
                    if (node.Value.Entry.Id == snapshotId)
                    {
                        _snapshots.Remove(node);
                        return Task.FromResult(true);
                    }
                    node = node.Next;
                }
            }
            return Task.FromResult(false);
        }

        private void EvictOverflowLocked()
        {
            while (_snapshots.Count > Math.Max(1, Capacity))
                _snapshots.RemoveFirst();
        }

        private void ApplyToProjectState(ProjectData data)
        {
            _projectState.Clear();

            if (data.Chapters != null)
            {
                foreach (var chapterData in data.Chapters)
                {
                    if (chapterData == null) continue;
                    _projectState.AddExistingChapter(
                        chapterData.Name ?? string.Empty,
                        chapterData.Id ?? Guid.NewGuid().ToString(),
                        chapterData.Text ?? string.Empty,
                        chapterData.Notes ?? string.Empty,
                        chapterData.Synopsis,
                        chapterData.WordCountGoal,
                        chapterData.Tags,
                        chapterData.PinboardX ?? 0,
                        chapterData.PinboardY ?? 0,
                        ParseStatus(chapterData.Status),
                        chapterData.Location,
                        chapterData.PlotThreads,
                        chapterData.LastCaretPosition ?? 0,
                        chapterData.LastVerticalOffset ?? 0);
                }
            }

            if (data.PinboardConnections != null)
                _projectState.PinboardConnections = data.PinboardConnections.ToList();

            if (data.PlotThreads != null)
                _projectState.PlotThreads = data.PlotThreads.ToList();
        }

        private static ChapterStatus ParseStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return ChapterStatus.Draft;

            return Enum.TryParse<ChapterStatus>(status, true, out var parsed)
                ? parsed
                : ChapterStatus.Draft;
        }

        private sealed class StoredSnapshot
        {
            public StoredSnapshot(SnapshotEntry entry, string json)
            {
                Entry = entry;
                SerializedJson = json;
            }

            public SnapshotEntry Entry { get; }

            public string SerializedJson { get; }
        }
    }
}
