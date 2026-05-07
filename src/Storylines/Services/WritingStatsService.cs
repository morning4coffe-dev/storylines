using Storylines.Services.Interfaces;
using System;
using System.Diagnostics;

namespace Storylines.Services
{
    /// <summary>
    /// In-memory implementation of <see cref="IWritingStatsService"/>. Persists daily counters
    /// and streak through <see cref="IAppSettingsService"/>. Date rollover is detected on every
    /// <see cref="RecordSnapshot"/> call so the service does not need its own timer.
    /// </summary>
    internal sealed class WritingStatsService : IWritingStatsService
    {
        private readonly IAppSettingsService _settings;

        private DateTime _today;
        private bool _hasStartOfDayBaseline;
        private int _wordsAtStartOfDay;
        private int _lastKnownWordCount;
        private int _sessionStartWordCount;
        private Stopwatch _sessionStopwatch;

        public WritingStatsService(IAppSettingsService settings)
        {
            _settings = settings;
            _today = DateTime.Today;
            DailyGoal = _settings.DailyWordGoal;
            WordsToday = 0;
            CurrentStreakDays = _settings.WritingStreakDays;
        }

        public int WordsToday { get; private set; }
        public int DailyGoal { get; private set; }
        public int CurrentStreakDays { get; private set; }
        public int SessionWords { get; private set; }
        public TimeSpan SessionDuration => _sessionStopwatch?.Elapsed ?? TimeSpan.Zero;
        public bool IsSessionActive => _sessionStopwatch is not null;

        public event Action StatsChanged;

        public void StartSession(int initialWordCount)
        {
            _sessionStartWordCount = initialWordCount;
            _lastKnownWordCount = initialWordCount;
            SessionWords = 0;
            _sessionStopwatch = Stopwatch.StartNew();
            RaiseStatsChanged();
        }

        public void RecordSnapshot(int currentWordCount)
        {
            RolloverIfNeeded();

            if (!_hasStartOfDayBaseline)
            {
                _wordsAtStartOfDay = currentWordCount;
                _hasStartOfDayBaseline = true;
            }

            var delta = currentWordCount - _wordsAtStartOfDay;
            WordsToday = delta < 0 ? 0 : delta;

            if (IsSessionActive)
            {
                var sessionDelta = currentWordCount - _sessionStartWordCount;
                SessionWords = sessionDelta < 0 ? 0 : sessionDelta;
            }

            _lastKnownWordCount = currentWordCount;

            UpdateStreakIfGoalMet();
            RaiseStatsChanged();
        }

        public void EndSession()
        {
            if (_sessionStopwatch is not null)
            {
                _sessionStopwatch.Stop();
                _sessionStopwatch = null;
            }
            SessionWords = 0;
            RaiseStatsChanged();
        }

        public void SetDailyGoal(int goal)
        {
            if (goal < 0) goal = 0;
            DailyGoal = goal;
            _settings.DailyWordGoal = goal;
            RaiseStatsChanged();
        }

        private void RolloverIfNeeded()
        {
            var current = DateTime.Today;
            if (current == _today) return;

            // Streak: increment if yesterday's goal was hit; reset otherwise.
            if (DailyGoal > 0 && WordsToday >= DailyGoal)
                CurrentStreakDays++;
            else
                CurrentStreakDays = 0;

            _settings.WritingStreakDays = CurrentStreakDays;

            _today = current;
            _hasStartOfDayBaseline = true;
            _wordsAtStartOfDay = _lastKnownWordCount;
            WordsToday = 0;
        }

        private void UpdateStreakIfGoalMet()
        {
            // Streak is committed at rollover, but we must guard against decreasing it within the
            // same day if WordsToday dips below the goal (shouldn't happen, but defensive).
            if (DailyGoal > 0 && WordsToday >= DailyGoal && CurrentStreakDays == 0)
            {
                CurrentStreakDays = 1;
                _settings.WritingStreakDays = 1;
            }
        }

        private void RaiseStatsChanged() => StatsChanged?.Invoke();
    }
}
