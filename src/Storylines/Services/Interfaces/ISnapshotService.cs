using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Per-project rolling snapshot history that the user can browse and restore. Promotes the
    /// existing recovery mechanism from a save-on-suspend safety net to a first-class feature.
    /// Snapshots are taken on save and on milestones (mode change, large delta).
    /// </summary>
    public interface ISnapshotService
    {
        /// <summary>
        /// Maximum snapshots retained per project. Older entries are evicted FIFO.
        /// </summary>
        int Capacity { get; set; }

        /// <summary>
        /// Returns the snapshot history for the current project, newest first. Empty when no
        /// project is loaded or when no snapshots have been taken yet.
        /// </summary>
        Task<IReadOnlyList<SnapshotEntry>> GetHistoryAsync();

        /// <summary>
        /// Capture a new snapshot of the currently loaded project. <paramref name="reason"/> is
        /// shown to the user in the snapshot browser (e.g. "Manual save", "Focus mode entered").
        /// </summary>
        Task CaptureAsync(string reason);

        /// <summary>
        /// Restore the project state from the snapshot identified by <paramref name="snapshotId"/>.
        /// </summary>
        Task<bool> RestoreAsync(string snapshotId);

        /// <summary>
        /// Permanently delete a snapshot.
        /// </summary>
        Task<bool> DeleteAsync(string snapshotId);
    }

    /// <summary>
    /// Metadata for a single snapshot. Body is loaded lazily on restore.
    /// </summary>
    public sealed class SnapshotEntry
    {
        public SnapshotEntry(
            string id,
            DateTimeOffset captured,
            string reason,
            int chapterCount,
            int characterCount,
            long approximateBytes)
        {
            Id = id;
            Captured = captured;
            Reason = reason;
            ChapterCount = chapterCount;
            CharacterCount = characterCount;
            ApproximateBytes = approximateBytes;
        }

        public string Id { get; }

        public DateTimeOffset Captured { get; }

        public string Reason { get; }

        public int ChapterCount { get; }

        public int CharacterCount { get; }

        public long ApproximateBytes { get; }
    }
}
