
namespace Storylines.Services.Interfaces;

public interface ITelemetryService
{
    Task InitializeAsync();

    void TrackAppStarted(string activationKind);

    void TrackAppClosingRequested(bool blockedByUnsavedChanges);

    void TrackReviewPromptDisplayed(string source);

    void TrackReviewInteraction(string source, string action, string status = null);

    void TrackStoreUpdateAvailable(int packageCount);

    void TrackFocusModeStarted(bool fullScreen, bool autosave, string measureMetric, int measureTarget, TimeSpan timeTarget);

    void TrackFocusModeLeft(bool finished);

    void TrackProjectStatsOpened(bool fromDownBar);

    void TrackBannerClicked(string bannerName, string destination);

    void TrackUnhandledException(Exception exception, string message);
}