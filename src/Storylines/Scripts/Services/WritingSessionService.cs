using Newtonsoft.Json;
using System;
using Windows.Storage;

namespace Storylines.Scripts.Services
{
    /// <summary>
    /// Tracks per-day word counts and writing streaks, persisted in LocalSettings.
    /// Call <see cref="OnSessionStart"/> once when the user opens a project, then
    /// <see cref="RecordWords"/> after each text-change event.
    /// </summary>
    public static class WritingSessionService
    {
        private const string SettingsKey = "WritingSession";

        private static int _sessionBaselineWords;
        private static bool _sessionStarted;

        public static WritingSessionData Current { get; private set; } = Load();

        // ─── Public API ───────────────────────────────────────────────

        /// <summary>Call once when a project is opened / app becomes active.</summary>
        public static void OnSessionStart(int currentProjectWordCount)
        {
            Current = Load();

            string today = Today();

            // Roll streak: if last session was not yesterday or today, reset
            if (!string.IsNullOrEmpty(Current.LastSessionDate))
            {
                if (Current.LastSessionDate == today)
                {
                    // Same day — continue session
                }
                else if (Current.LastSessionDate == Yesterday())
                {
                    // New day but consecutive
                    Current.TodayWords = 0;
                    Current.Date = today;
                }
                else
                {
                    // Gap in writing — break streak
                    Current.StreakDays = 0;
                    Current.TodayWords = 0;
                    Current.Date = today;
                }
            }
            else
            {
                Current.Date = today;
            }

            _sessionBaselineWords = currentProjectWordCount;
            _sessionStarted = true;
            Save();
        }

        /// <summary>
        /// Call with the new total project word count after each text change.
        /// Calculates delta from the session baseline and accumulates.
        /// </summary>
        public static void RecordWords(int currentProjectWordCount)
        {
            if (!_sessionStarted)
                return;

            int delta = Math.Max(0, currentProjectWordCount - _sessionBaselineWords);
            Current.TodayWords = delta;

            string today = Today();
            if (Current.Date != today)
            {
                Current.StreakDays++;
                Current.Date = today;
                Current.TodayWords = 0;
                _sessionBaselineWords = currentProjectWordCount;
            }

            Current.LastSessionDate = today;

            Save();
            ServiceLocator.Events.Publish(new SessionStatsUpdatedEvent
            {
                TodayWords = Current.TodayWords,
                StreakDays = Current.StreakDays
            });
        }

        /// <summary>Called when the user completes a writing day — bumps streak.</summary>
        public static void OnDayCompleted()
        {
            Current.StreakDays++;
            Current.LastSessionDate = Today();
            Save();
        }

        public static int GetCurrentStreak() => Current.StreakDays;

        public static int GetTodayWords() => Current.TodayWords;

        // ─── Persistence ─────────────────────────────────────────────

        private static WritingSessionData Load()
        {
            try
            {
                var raw = ApplicationData.Current.LocalSettings.Values[SettingsKey]?.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                    return JsonConvert.DeserializeObject<WritingSessionData>(raw) ?? new WritingSessionData();
            }
            catch { /* corrupt data — start fresh */ }
            return new WritingSessionData();
        }

        private static void Save()
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values[SettingsKey] = JsonConvert.SerializeObject(Current);
            }
            catch { /* best-effort */ }
        }

        // ─── Helpers ─────────────────────────────────────────────────

        private static string Today() => DateTime.Now.ToString("yyyy-MM-dd");
        private static string Yesterday() => DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
    }

    public class WritingSessionData
    {
        [JsonProperty("date")]
        public string Date { get; set; } = string.Empty;

        [JsonProperty("todayWords")]
        public int TodayWords { get; set; }

        [JsonProperty("streakDays")]
        public int StreakDays { get; set; }

        [JsonProperty("lastSessionDate")]
        public string LastSessionDate { get; set; } = string.Empty;

        [JsonProperty("dailyGoal")]
        public int DailyGoal { get; set; } = 500;
    }
}
