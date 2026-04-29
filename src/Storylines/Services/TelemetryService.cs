using Microsoft.Toolkit.Uwp.Helpers;
using Storylines.Helpers;
using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.Services.Modes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Storylines.Services
{
    internal sealed class TelemetryService : ITelemetryService
    {
        private readonly ITelemetryProvider _provider;
        private readonly ProjectState _projectState;
        private readonly EditorModeService _editorModeService;
        private readonly ILogger _logger;
        private readonly string _sessionId = Guid.NewGuid().ToString("N");

        public TelemetryService(ITelemetryProvider provider, ProjectState projectState, EditorModeService editorModeService, ILogger logger)
        {
            _provider = provider;
            _projectState = projectState;
            _editorModeService = editorModeService;
            _logger = logger;
        }

        public Task InitializeAsync() => _provider.InitializeAsync();

        public void TrackAppStarted(string activationKind)
        {
            TrackProviderEvent(
                "app_start",
                TelemetryEventPropertyBuilder.Create(
                    ("activation_kind", activationKind),
                    ("first_run", SystemInformation.Instance.IsFirstRun.ToString()),
                    ("launch_count", FormatNumber(SystemInformation.Instance.TotalLaunchCount)),
                    ("autosave_enabled", SettingsValues.autosaveEnabled.ToString()),
                    ("autosave_interval_minutes", FormatNumber(SettingsValues.autosaveInterval)),
                    ("exit_dialog_enabled", SettingsValues.exitDiagEnabled.ToString()),
                    ("white_text_background", SettingsValues.whiteTextBackground.ToString()),
                    ("accent", SettingsValues.selectedAccent.ToString()),
                    ("experimental_features_enabled", SettingsValues.experimentalFeaturesEnabled.ToString()),
                    ("dialogue_mode_enabled", SettingsValues.dialogueModeEnabled.ToString()),
                    ("review_prompt_state", GetReviewPromptState())));
        }

        public void TrackAppClosingRequested(bool blockedByUnsavedChanges)
        {
            var properties = TelemetryEventPropertyBuilder.Build(
                CreateFullProjectSummaryProperties(),
                TelemetryEventPropertyBuilder.Create(
                    ("blocked_by_unsaved_changes", blockedByUnsavedChanges.ToString()),
                    ("uptime_minutes", FormatNumber(SystemInformation.Instance.AppUptime.TotalMinutes)),
                    ("unsaved_progress", TimeTravelSystem.unSavedProgress.ToString())));

            TrackProviderEvent("app_close_requested", properties);
        }

        public void TrackReviewPromptDisplayed(string source)
        {
            TrackProviderEvent(
                "review_prompt_displayed",
                TelemetryEventPropertyBuilder.Create(
                    ("source", source),
                    ("review_prompt_state", GetReviewPromptState()),
                    ("launch_count", FormatNumber(SystemInformation.Instance.TotalLaunchCount))));
        }

        public void TrackReviewInteraction(string source, string action, string status = null)
        {
            TrackProviderEvent(
                "review_prompt_interaction",
                TelemetryEventPropertyBuilder.Create(
                    ("source", source),
                    ("action", action),
                    ("status", status),
                    ("review_prompt_state", GetReviewPromptState())));
        }

        public void TrackStoreUpdateAvailable(int packageCount)
        {
            TrackProviderEvent(
                "store_update_available",
                TelemetryEventPropertyBuilder.Create(
                    ("package_count", FormatNumber(packageCount)),
                    ("notification_surface", "infobar")));
        }

        public void TrackFocusModeStarted(bool fullScreen, bool autosave, string measureMetric, int measureTarget, TimeSpan timeTarget)
        {
            var properties = TelemetryEventPropertyBuilder.Build(
                CreateCompactProjectSummaryProperties(),
                TelemetryEventPropertyBuilder.Create(
                    ("full_screen", fullScreen.ToString()),
                    ("autosave", autosave.ToString()),
                    ("measure_metric", measureMetric),
                    ("measure_target", FormatNumber(measureTarget)),
                    ("time_target_minutes", FormatNumber(timeTarget.TotalMinutes)),
                    ("uses_measure_target", (measureTarget > 0).ToString()),
                    ("uses_time_target", (timeTarget > TimeSpan.Zero).ToString())));

            TrackProviderEvent("focus_mode_started", properties);
        }

        public void TrackFocusModeLeft(bool finished)
        {
            TrackProviderEvent(
                "focus_mode_left",
                TelemetryEventPropertyBuilder.Create(
                    ("finished", finished.ToString()),
                    ("uptime_minutes", FormatNumber(SystemInformation.Instance.AppUptime.TotalMinutes)),
                    ("can_leave", _editorModeService.Current.CanLeave.ToString())));
        }

        public void TrackProjectStatsOpened(bool fromDownBar)
        {
            var properties = TelemetryEventPropertyBuilder.Build(
                CreateFullProjectSummaryProperties(),
                TelemetryEventPropertyBuilder.Create(
                    ("from_down_bar", fromDownBar.ToString())));

            TrackProviderEvent("project_stats_opened", properties);
        }

        public void TrackBannerClicked(string bannerName, string destination)
        {
            TrackProviderEvent(
                "banner_clicked",
                TelemetryEventPropertyBuilder.Create(
                    ("banner_name", bannerName),
                    ("destination", destination)));
        }

        public void TrackUnhandledException(Exception exception, string message)
        {
            var eventProperties = TelemetryEventPropertyBuilder.Build(
                CreateCompactProjectSummaryProperties(),
                TelemetryEventPropertyBuilder.Create(
                    ("exception_type", exception?.GetType().Name ?? "Unknown"),
                    ("message", message ?? exception?.Message ?? "Unknown"),
                    ("has_inner_exception", (exception?.InnerException != null).ToString()),
                    ("inner_exception_type", exception?.InnerException?.GetType().Name),
                    ("available_memory", FormatNumber(SystemInformation.Instance.AvailableMemory)),
                    ("uptime_minutes", FormatNumber(SystemInformation.Instance.AppUptime.TotalMinutes)),
                    ("unsaved_progress", TimeTravelSystem.unSavedProgress.ToString())));

            TrackProviderEvent("app_unhandled_exception", eventProperties);

            try
            {
                var attachment =
                    $"Exception: {exception}{Environment.NewLine}" +
                    $"Message: {message}{Environment.NewLine}" +
                    $"InnerException: {exception?.InnerException}{Environment.NewLine}" +
                    $"InnerExceptionMessage: {exception?.InnerException?.Message}";

                _provider.TrackError(
                    exception ?? new InvalidOperationException(message ?? "Unknown unhandled exception."),
                    BuildProperties(eventProperties),
                    attachment,
                    "UnhandledException.txt");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to record telemetry for an unhandled exception.", ex);
            }
        }

        private IEnumerable<KeyValuePair<string, string>> CreateBaseProperties()
        {
            return TelemetryEventPropertyBuilder.Create(
                ("session_id", _sessionId),
                ("telemetry_provider", _provider.ProviderName),
                ("app_version", SystemInformation.Instance.ApplicationVersion.ToFormattedString()),
                ("os_version", GetOperatingSystemVersion()),
                ("device_family", SystemInformation.Instance.DeviceFamily),
                //TODO ("device_architecture", SystemInformation.Instance.OperatingSystemArchitecture),
                ("app_language", GetApplicationLanguage()),
                ("ui_theme", SettingsValues.selectedTheme.ToString()),
                ("editor_mode", _editorModeService.Current.Id));
        }

        private IEnumerable<KeyValuePair<string, string>> CreateCompactProjectSummaryProperties()
        {
            var graphs = _projectState.BranchingDialogues ?? new List<BranchingDialogueGraphData>();

            return TelemetryEventPropertyBuilder.Create(
                ("has_open_project", HasOpenProject().ToString()),
                ("chapter_count", FormatNumber(_projectState.Chapters.Count)),
                ("character_count", FormatNumber(_projectState.Characters.Count)),
                ("branching_dialogue_graph_count", FormatNumber(graphs.Count)));
        }

        private IEnumerable<KeyValuePair<string, string>> CreateFullProjectSummaryProperties()
        {
            var graphs = _projectState.BranchingDialogues ?? new List<BranchingDialogueGraphData>();
            var nodeCount = graphs.Sum(graph => graph?.Nodes?.Count ?? 0);
            var choiceCount = graphs.Sum(graph => graph?.Nodes?.Sum(node => node?.Choices?.Count ?? 0) ?? 0);

            return TelemetryEventPropertyBuilder.Create(
                ("has_open_project", HasOpenProject().ToString()),
                ("chapter_count", FormatNumber(_projectState.Chapters.Count)),
                ("character_count", FormatNumber(_projectState.Characters.Count)),
                ("plot_thread_count", FormatNumber(_projectState.PlotThreads.Count)),
                ("branching_dialogue_graph_count", FormatNumber(graphs.Count)),
                ("branching_dialogue_node_count", FormatNumber(nodeCount)),
                ("branching_dialogue_choice_count", FormatNumber(choiceCount)));
        }

        private IReadOnlyDictionary<string, string> BuildProperties(IEnumerable<KeyValuePair<string, string>> properties)
        {
            return TelemetryEventPropertyBuilder.Build(CreateBaseProperties(), properties);
        }

        private void TrackProviderEvent(string eventName, IEnumerable<KeyValuePair<string, string>> properties)
        {
            try
            {
                _provider.TrackEvent(eventName, BuildProperties(properties));
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to record telemetry event '{eventName}'.", ex);
            }
        }

        private bool HasOpenProject()
        {
            return _projectState.Chapters.Count > 0
                || _projectState.Characters.Count > 0
                || _projectState.PlotThreads.Count > 0
                || (_projectState.BranchingDialogues?.Count ?? 0) > 0;
        }

        private static string GetApplicationLanguage()
        {
            return string.IsNullOrWhiteSpace(SettingsValues.language)
                ? SystemInformation.Instance.Culture.Name
                : SettingsValues.language;
        }

        private static string GetOperatingSystemVersion()
        {
            var version = SystemInformation.Instance.OperatingSystemVersion;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private static string GetReviewPromptState()
        {
            var storedValue = Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ReviewPrompt] ?? (int)SettingsValues.ReviewPrompt.NotYet;
            return ((SettingsValues.ReviewPrompt)storedValue).ToString();
        }

        private static string FormatNumber(int value)
            => value.ToString(CultureInfo.InvariantCulture);

        private static string FormatNumber(double value)
            => Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
    }
}