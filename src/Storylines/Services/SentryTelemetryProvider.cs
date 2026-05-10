using Sentry;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Storylines.Services
{
    internal sealed class SentryTelemetryProvider : ITelemetryProvider
    {
        private static string Dsn => Environment.GetEnvironmentVariable("sentryId");

        private const bool IsEnabled
#if DEBUG
        = false
#else
        = true
#endif
        ;

        private readonly ILogger _logger;
        private bool _initialized;

        public SentryTelemetryProvider(ILogger logger)
        {
            _logger = logger;
        }

        public string ProviderName => "sentry";

        private static bool IsConfigured => !string.IsNullOrWhiteSpace(Dsn);
        private static bool IsActive => IsConfigured && IsEnabled;

        public Task InitializeAsync()
        {
            if (_initialized || !IsActive)
                return Task.CompletedTask;

            _initialized = true;

            try
            {
                SentrySdk.Init(options =>
                {
                    options.Dsn = Dsn;
                    options.IsGlobalModeEnabled = true;
                    options.AutoSessionTracking = false;
                    options.TracesSampleRate = 0;
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize Sentry telemetry provider.", ex);
            }

            return Task.CompletedTask;
        }

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties)
        {
            if (!_initialized || !IsActive || string.IsNullOrWhiteSpace(eventName))
                return;

            try
            {
                SentrySdk.CaptureMessage(eventName, scope =>
                {
                    foreach (var (key, value) in (properties ?? Enumerable.Empty<KeyValuePair<string, string>>()))
                    {
                        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                            scope.SetTag(key, value);
                    }
                });
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
                SentrySdk.CaptureException(effectiveException, scope =>
                {
                    foreach (var (key, value) in (properties ?? Enumerable.Empty<KeyValuePair<string, string>>()))
                    {
                        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                            scope.SetTag(key, value);
                    }

                    if (!string.IsNullOrWhiteSpace(attachmentText))
                    {
                        var fileName = string.IsNullOrWhiteSpace(attachmentFileName) ? "UnhandledException.txt" : attachmentFileName;
                        scope.AddAttachment(Encoding.UTF8.GetBytes(attachmentText), fileName);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to track telemetry error.", ex);
            }
        }
    }
}
