
namespace Storylines.Services.Interfaces;

/// <summary>
/// Unified façade over read-aloud (text-to-speech) and dictation (speech-to-text). Maintains
/// mutual-exclusion: starting one capability stops the other so the microphone and speakers
/// never contend.
/// </summary>
public interface ISpeechService
{
    /// <summary>
    /// Current high-level mode. Only one capability is active at a time.
    /// </summary>
    SpeechMode Mode { get; }

    /// <summary>
    /// Raised whenever <see cref="Mode"/> changes so toolbar / status footer indicators stay in sync.
    /// </summary>
    event Action<SpeechMode> ModeChanged;

    /// <summary>
    /// Underlying dictation capability.
    /// </summary>
    IDictationService Dictation { get; }

    /// <summary>
    /// Underlying read-aloud (text-to-speech) capability.
    /// </summary>
    IReadAloudService ReadAloud { get; }
}

/// <summary>
/// Top-level speech-hub state surfaced to the UI.
/// </summary>
public enum SpeechMode
{
    Idle,
    Reading,
    Dictating,
}
