using Newtonsoft.Json;
using System;
using Storylines.Services.Interfaces;

namespace Storylines.Services
{
    /// <summary>
    /// Tracks per-day word counts and writing streaks, persisted in LocalSettings.
    /// Call <see cref="OnSessionStart"/> once when the user opens a project, then
    /// <see cref="RecordWords"/> after each text-change event.
    /// </summary>
    public class WritingSessionService : IWritingSessionService
    {
        private const string SettingsKey = "WritingSession";

        private int _sessionBaselineWords;
        private bool _sessionStarted;
        private readonly IPreferencesService _prefs;
        private readonly EventAggregator _events;
        private readonly ILogger _logger;

        public WritingSessionData Current { get; private set; }

        public WritingSessionService(IPreferencesService prefs, EventAggregator events, ILogger logger)
        {
            _prefs = prefs;
            _events = events;
            _logger = logger;
            Current = Load();
        }

        // ─── Public API ───────────────────────────────────────────────

        /// <summary>Call once when a project is opened / app becomes active.</summary>
        public void OnSessionStart(int currentProjectWordCount)
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
        public void RecordWords(int currentProjectWordCount)
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
            _events.Publish(new SessionStatsUpdatedEvent
            {
                TodayWords = Current.TodayWords,
                StreakDays = Current.StreakDays
            });
        }

        /// <summary>Called when the user completes a writing day — bumps streak.</summary>
        public void OnDayCompleted()
        {
            Current.StreakDays++;
            Current.LastSessionDate = Today();
            Save();
        }

        public int GetCurrentStreak() => Current.StreakDays;

        public int GetTodayWords() => Current.TodayWords;

        // ─── Persistence ─────────────────────────────────────────────

        private WritingSessionData Load()
        {
            try
            {
                var raw = _prefs.Get<string>(SettingsKey);
                if (!string.IsNullOrWhiteSpace(raw))
                    return JsonConvert.DeserializeObject<WritingSessionData>(raw) ?? new WritingSessionData();
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to load writing session data: {ex.Message}");
            }

            return new WritingSessionData();
        }

        private void Save()
        {
            try
            {
                _prefs.Set(SettingsKey, JsonConvert.SerializeObject(Current));
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to save writing session data: {ex.Message}");
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────

        private string Today() => DateTime.Now.ToString("yyyy-MM-dd");
        private string Yesterday() => DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
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
