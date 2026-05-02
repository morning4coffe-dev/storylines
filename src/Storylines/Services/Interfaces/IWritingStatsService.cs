using System;

namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Tracks per-day writing progress and session metrics independent of the editor mode. The
    /// service owns three orthogonal concerns: today's word delta, the active writing session,
    /// and the rolling daily streak. Surfaced through the status footer and the goals inspector.
    /// </summary>
    public interface IWritingStatsService
    {
        /// <summary>
        /// Words written today across all chapters. Resets at local midnight.
        /// </summary>
        int WordsToday { get; }

        /// <summary>
        /// User-configured daily target. <c>0</c> means the goal feature is disabled.
        /// </summary>
        int DailyGoal { get; }

        /// <summary>
        /// Number of consecutive days the user has hit their <see cref="DailyGoal"/>.
        /// </summary>
        int CurrentStreakDays { get; }

        /// <summary>
        /// Words written in the current writing session (since <see cref="StartSession"/>).
        /// </summary>
        int SessionWords { get; }

        /// <summary>
        /// Wall-clock duration of the current writing session.
        /// </summary>
        TimeSpan SessionDuration { get; }

        /// <summary>
        /// True while a session is active.
        /// </summary>
        bool IsSessionActive { get; }

        /// <summary>
        /// Raised whenever any of the observable counters change so the UI can refresh without
        /// polling. Coalesced — a single tick may reflect multiple counter updates.
        /// </summary>
        event Action StatsChanged;

        /// <summary>
        /// Begin a writing session. Idempotent: calling while a session is active resets the
        /// session start word count to <paramref name="initialWordCount"/>.
        /// </summary>
        void StartSession(int initialWordCount);

        /// <summary>
        /// Update the session and daily counters with a fresh full-project word count snapshot.
        /// Safe to call frequently (e.g. on every text-change debounce tick).
        /// </summary>
        void RecordSnapshot(int currentWordCount);

        /// <summary>
        /// End the active session. Subsequent <see cref="RecordSnapshot"/> calls feed only the
        /// daily counter until <see cref="StartSession"/> runs again.
        /// </summary>
        void EndSession();

        /// <summary>
        /// Update the daily target. <c>0</c> disables the goal feature; persisted via the
        /// app-settings service.
        /// </summary>
        void SetDailyGoal(int goal);
    }
}
