using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storylines.Services
{
    internal sealed class AppCenterTelemetryProvider : ITelemetryProvider
    {
        private const string AppSecret = "";
        private const bool IsEnabled = false;
        private const int MaxPropertiesPerEvent = 20;
        private const int MaxPropertyLength = 125;

        private readonly ILogger _logger;
        private bool _initialized;

        public AppCenterTelemetryProvider(ILogger logger)
        {
            _logger = logger;
        }

        public string ProviderName => "appcenter";

        private static bool IsConfigured => !string.IsNullOrWhiteSpace(AppSecret);
        private static bool IsActive => IsConfigured && IsEnabled;

        public async Task InitializeAsync()
        {
            if (_initialized || !IsConfigured)
                return;

            _initialized = true;

            try
            {
                AppCenter.Start(AppSecret, typeof(Analytics), typeof(Crashes));
                await AppCenter.SetEnabledAsync(IsEnabled);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize App Center telemetry provider.", ex);
            }
        }

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties)
        {
            if (!_initialized || !IsActive || string.IsNullOrWhiteSpace(eventName))
                return;

            try
            {
                Analytics.TrackEvent(TrimToProviderLimit(eventName), NormalizeProperties(properties));
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to track telemetry event '{eventName}'.", ex);
            }
        }

        public void TrackError(Exception exception, IReadOnlyDictionary<string, string> properties, string attachmentText = null, string attachmentFileName = null)
        {
            if (!_initialized || !IsActive)
                return;

            try
            {
                var effectiveException = exception ?? new InvalidOperationException("Unknown telemetry error.");
                var normalizedProperties = NormalizeProperties(properties);

                if (string.IsNullOrWhiteSpace(attachmentText))
                {
                    Crashes.TrackError(effectiveException, normalizedProperties);
                    return;
                }

                var attachment = ErrorAttachmentLog.AttachmentWithText(
                    attachmentText,
                    string.IsNullOrWhiteSpace(attachmentFileName) ? "UnhandledException.txt" : TrimToProviderLimit(attachmentFileName));

                Crashes.TrackError(effectiveException, normalizedProperties, attachment);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to track telemetry error.", ex);
            }
        }

        private static IDictionary<string, string> NormalizeProperties(IReadOnlyDictionary<string, string> properties)
        {
            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in properties ?? Enumerable.Empty<KeyValuePair<string, string>>())
            {
                if (normalized.Count >= MaxPropertiesPerEvent)
                    break;

                var key = TrimToProviderLimit(property.Key);
                var value = TrimToProviderLimit(property.Value);
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    continue;

                normalized[key] = value;
            }

            return normalized;
        }

        private static string TrimToProviderLimit(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var trimmed = value.Trim();
            return trimmed.Length <= MaxPropertyLength ? trimmed : trimmed.Substring(0, MaxPropertyLength);
        }
    }
}