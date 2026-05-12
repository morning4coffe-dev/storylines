using Sentry;
using Sentry.Profiling;
using System.Text;
using Windows.ApplicationModel;

namespace Storylines.Services;

internal sealed class SentryTelemetryProvider : ITelemetryProvider
{
    private const double TracesSampleRate = 0.2;
    private const double ProfilesSampleRate = 1.0;

    private static readonly HashSet<string> MetricTagAllowList = new(StringComparer.Ordinal)
    {
        "action",
        "activation_kind",
        "app_version",
        "autosave",
        "banner_name",
        "blocked_by_unsaved_changes",
        "destination",
        "device_family",
        "dialogue_mode_enabled",
        "editor_mode",
        "experimental_features_enabled",
        "first_run",
        "finished",
        "full_screen",
        "notification_surface",
        "source",
        "status"
    };

    private static string Dsn => Environment.GetEnvironmentVariable("sentryId");

    private static string Release => $"Storylines@{GetApplicationVersion()}";

    private static string EnvironmentName
#if DEBUG
        => "development";
#else
        => "production";
#endif

    private const bool IsEnabled
#if DEBUG
    = true
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
                options.Release = Release;
                options.Environment = EnvironmentName;
                options.AutoSessionTracking = true;
                options.TracesSampleRate = TracesSampleRate;
                options.ProfilesSampleRate = ProfilesSampleRate;
                // Route SentrySdk.Logger.Log* calls to the Sentry Logs UI
                // (separate from the Issues Feed, which is only for exceptions/CaptureMessage).
                options.EnableLogs = true;
                options.AddIntegration(new ProfilingIntegration(TimeSpan.FromMilliseconds(500)));
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
            var attrs = new Dictionary<string, object>();
            var breadcrumbData = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in (properties ?? Enumerable.Empty<KeyValuePair<string, string>>()))
            {
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    attrs[key] = value;
                    breadcrumbData[key] = value;
                }
            }

            SentrySdk.AddBreadcrumb(eventName, category: "telemetry", data: breadcrumbData);
            SentrySdk.Logger.LogInfo(eventName, attrs);
            EmitMetric(eventName, properties);
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

    private static void EmitMetric(string eventName, IReadOnlyDictionary<string, string> properties)
    {
        var tags = new List<KeyValuePair<string, object>>
        {
            new("event_name", eventName)
        };

        foreach (var (key, value) in properties ?? Enumerable.Empty<KeyValuePair<string, string>>())
        {
            if (!MetricTagAllowList.Contains(key) || string.IsNullOrWhiteSpace(value))
                continue;

            tags.Add(new KeyValuePair<string, object>(key, value));
        }

        SentrySdk.Metrics.EmitCounter("storylines.telemetry_event", 1, tags);
    }

    private static string GetApplicationVersion()
    {
        var version = Package.Current.Id.Version;
        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
