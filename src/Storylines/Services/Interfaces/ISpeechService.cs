using System;

namespace Storylines.Services.Interfaces
{
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
        /// Notify the hub that read-aloud (TTS) has started. Stops any active dictation session.
        /// </summary>
        void NotifyReadingStarted();

        /// <summary>
        /// Notify the hub that read-aloud has stopped (cancelled or completed naturally).
        /// </summary>
        void NotifyReadingStopped();
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
}
