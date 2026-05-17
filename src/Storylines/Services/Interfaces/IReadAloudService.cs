
namespace Storylines.Services.Interfaces;

/// <summary>
/// Text-to-speech playback. Synthesizes text and plays it through the system audio output,
/// driven by the voice / volume / rate / pitch values stored in <see cref="IAppSettingsService"/>.
/// Companion to <see cref="IDictationService"/>; both are unified behind <see cref="ISpeechService"/>.
/// </summary>
public interface IReadAloudService
{
    ReadAloudState State { get; }

    /// <summary>0..1 progress within the current utterance.</summary>
    double Progress { get; }

    int CurrentParagraphIndex { get; }

    int TotalParagraphs { get; }

    event Action<ReadAloudState> StateChanged;

    event Action<double> ProgressChanged;

    event Action Completed;

    /// <summary>Speak a single utterance. Replaces any in-progress playback.</summary>
    Task SpeakAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Speak a sequence of paragraphs starting at <paramref name="startIndex"/>.</summary>
    Task SpeakParagraphsAsync(IReadOnlyList<string> paragraphs, int startIndex = 0, CancellationToken cancellationToken = default);

    /// <summary>Speak a short sample using the current voice / rate / pitch — used by the settings "test voice" button.</summary>
    Task SpeakSampleAsync(CancellationToken cancellationToken = default);

    void Pause();

    void Resume();

    void Stop();

    Task NextParagraphAsync();

    Task PreviousParagraphAsync();
}

/// <summary>
/// Lifecycle state of <see cref="IReadAloudService"/>.
/// </summary>
public enum ReadAloudState
{
    Idle,
    Loading,
    Playing,
    Paused,
}
