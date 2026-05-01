using Storylines.Helpers;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.ViewModels.Modes;
using Storylines.Views.Controls.Modes;
using System;
using Windows.UI.ViewManagement;

namespace Storylines.Services.Modes.Impl
{
    /// <summary>
    /// New FocusMode implementation that delegates all state to
    /// <see cref="FocusModeViewModel"/> and renders UI via
    /// <see cref="FocusModeOverlay"/> — no view mutation, no reparenting.
    /// </summary>
    public sealed class FocusMode : IEditorMode
    {
        private readonly EventAggregator _events;
        private readonly INotificationService _notifications;

        public FocusMode(EventAggregator events, INotificationService notifications)
        {
            _events = events;
            _notifications = notifications;
        }

        // ── options set by the mode picker before Activate ────────────────────
        public bool FullScreen { get; set; }
        public TimeSpan Time { get; set; }
        public int MeasureTarget { get; set; }
        public MeasureMetric Metric { get; set; }

        // ── per-session state ─────────────────────────────────────────────────
        private FocusModeViewModel _vm;
        private FocusModeOverlay _overlay;

        // IEditorMode ─────────────────────────────────────────────────────────
        public string Id => "focus";
        public string DisplayNameKey => "modeFocus";
        public string DescriptionKey => "modeFocusDescription";
        public string IconGlyph => "\uE1D5";

        public ModeChromeConfig Chrome => new ModeChromeConfig(
            showDefaultCommandBar: false,
            showChapterList: false,
            showChapterTextFormattingBar: false,
            showDownBarStats: false,
            showDownBarFocusText: true,
            isTextReadOnly: false,
            allowsEditingShortcuts: true,
            allowsSettingsShortcut: false,
            overlayContent: _overlay);

        public bool CanLeave => _vm == null || _vm.Final;

        public void Enter()
        {
            _vm = new FocusModeViewModel(_events, _notifications);
            _overlay = new FocusModeOverlay(_vm);

            if (FullScreen)
            {
                var view = ApplicationView.GetForCurrentView();
                view.TryEnterFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            }

            _vm.Initialize(Time, MeasureTarget, Metric);
        }

        public void Leave()
        {
            _vm?.Stop();
            _vm = null;

            if (FullScreen)
            {
                var view = ApplicationView.GetForCurrentView();
                view.ExitFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            }

            _overlay = null;
        }

        public void OnTextChanged()
        {
            _vm?.OnTextChanged();
        }
    }
}
