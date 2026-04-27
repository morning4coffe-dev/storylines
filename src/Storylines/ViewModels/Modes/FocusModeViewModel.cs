using CommunityToolkit.Mvvm.ComponentModel;
using Storylines.Constants;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Views.Dialogs;
using System;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;

namespace Storylines.ViewModels.Modes
{
    public enum MeasureMetric { Characters, Words, Paragraphs }

    public partial class FocusModeViewModel : ObservableObject
    {
        private static readonly Regex ParagraphRegex = new Regex(
            @"[^\r\n]+((\r|\n|\r\n)[^\r\n]+)*", RegexOptions.Compiled);

        // ── timer ────────────────────────────────────────────────────────────
        private DispatcherTimer _timer;
        private long _timerStartTicks;    // original duration in ticks
        private TimeSpan _timerRemaining; // counts down

        private bool _timeFinal;
        private bool _measureFinal;

        // ── measure ──────────────────────────────────────────────────────────
        public int MeasureTarget { get; private set; }
        public MeasureMetric Metric { get; private set; }
        private int _measureBaseline;
        private int _currentMeasureDelta;

        // ── down-bar text ─────────────────────────────────────────────────────
        private string _downBarTime = string.Empty;
        private string _downBarMeasure = string.Empty;

        [ObservableProperty]
        private string _downBarText = string.Empty;

        // ── leave gate ────────────────────────────────────────────────────────
        /// <summary>True once both the time target (if any) and measure target (if any) have been reached.</summary>
        public bool Final => _timeFinal && _measureFinal;

        // ── public initializer ────────────────────────────────────────────────
        public void Initialize(TimeSpan time, int measureTarget, MeasureMetric metric)
        {
            MeasureTarget = measureTarget;
            Metric = metric;

            // time gate
            if (time != TimeSpan.Zero)
            {
                _timerStartTicks = time.Ticks;
                _timerRemaining = time;

                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(LayoutConstants.FocusModeTimerIntervalSeconds) };
                _timer.Tick += OnTimerTick;
                _timer.Start();

                // Prime first tick value so down bar isn't blank for the first minute.
                _downBarTime = $"{time.Hours}:{time.Minutes:D2}";
            }
            else
            {
                _timeFinal = true;
            }

            // measure gate
            if (measureTarget > 0)
            {
                var text = ProjectStatsDialogue.GetTextFromAllChapters();
                _measureBaseline = MeasureRaw(text);
            }
            else
            {
                _measureFinal = true;
            }

            UpdateDownBar();
            NotificationManager.DisplayMainProgressBar(false);
        }

        // ── text-changed callback (called by FocusMode.OnTextChanged) ─────────
        public void OnTextChanged()
        {
            if (MeasureTarget <= 0) return;

            var text = ProjectStatsDialogue.GetTextFromAllChapters();
            _currentMeasureDelta = MeasureRaw(text) - _measureBaseline;

            _downBarMeasure = $"{_currentMeasureDelta} / {MeasureTarget}";

            if (Math.Abs(_currentMeasureDelta) >= MeasureTarget)
                SetMeasureFinal(true);
            else
                SetMeasureFinal(false);

            RefreshProgressBar(_timerRemaining.Ticks, _currentMeasureDelta);
            UpdateDownBar();
        }

        // ── cleanup ───────────────────────────────────────────────────────────
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Tick -= OnTimerTick;
                _timer.Stop();
                _timer = null;
            }
            NotificationManager.HideMainProgressBar();
        }

        // ── private helpers ───────────────────────────────────────────────────
        private void OnTimerTick(object sender, object e)
        {
            _timerRemaining = _timerRemaining.Subtract(TimeSpan.FromSeconds(LayoutConstants.FocusModeTimerIntervalSeconds));

            if (_timerRemaining.Ticks > 0)
            {
                _downBarTime = $"{_timerRemaining.Hours}:{_timerRemaining.Minutes:D2}";
                RefreshProgressBar(_timerRemaining.Ticks, _currentMeasureDelta);
            }
            else
            {
                _timer.Stop();
                _timeFinal = true;
                _downBarTime = ResourceLoader.GetForViewIndependentUse().GetString("done");
                RefreshProgressBar(0, _currentMeasureDelta);
            }

            UpdateDownBar();
        }

        private void SetMeasureFinal(bool reached)
        {
            _measureFinal = reached;
        }

        private void RefreshProgressBar(long currentTimeTicks, int currentMeasure)
        {
            int percentage = 0;
            int multiplier = (_timerStartTicks < 1 || MeasureTarget < 1)
                ? LayoutConstants.FocusModeSingleTargetMultiplier
                : LayoutConstants.FocusModeDualTargetMultiplier;

            if (MeasureTarget > 0)
            {
                int clamped = Math.Min(Math.Abs(currentMeasure), MeasureTarget);
                percentage += (int)Math.Round((double)(multiplier * clamped) / MeasureTarget);
            }

            if (_timerStartTicks > 0 && currentTimeTicks >= 0)
                percentage += (int)((_timerStartTicks - currentTimeTicks) * multiplier / _timerStartTicks);

            percentage = Math.Max(0, Math.Min(100, percentage));
            NotificationManager.UpdateMainProgressBar(percentage, NotificationManager.ProgressState.Normal);
        }

        private void UpdateDownBar()
        {
            DownBarText = $"{_downBarMeasure}   {_downBarTime}".Trim();
            App.TryGetService<EventAggregator>()?.Publish(new FocusModeDownBarTextChangedEvent { Text = DownBarText });
        }

        private int MeasureRaw(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            switch (Metric)
            {
                case MeasureMetric.Characters:
                    return text.Length > 0 ? text.Length - 1 : 0;
                case MeasureMetric.Words:
                    return text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                case MeasureMetric.Paragraphs:
                    return ParagraphRegex.Matches(text).Count;
                default:
                    return 0;
            }
        }
    }
}
