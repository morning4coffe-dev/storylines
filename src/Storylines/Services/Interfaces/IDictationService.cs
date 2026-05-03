using System;
using System.Threading;
using System.Threading.Tasks;

namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Speech-to-text dictation. Inserts recognised speech into the active text editor so users
    /// can write hands-free. Companion to the read-aloud / text-to-speech path; both are unified
    /// behind <see cref="ISpeechService"/>.
    /// </summary>
    public interface IDictationService
    {
        /// <summary>
        /// True while the underlying speech recogniser is actively listening for input.
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// Raised when the recogniser produces a finalised hypothesis ready to insert.
        /// </summary>
        event Action<DictationResult> ResultRecognized;

        /// <summary>
        /// Raised when the recogniser changes lifecycle state (start, stop, error).
        /// </summary>
        event Action<DictationStateChange> StateChanged;

        /// <summary>
        /// Begin continuous dictation. Returns when the recogniser has started; recognised text
        /// is delivered asynchronously via <see cref="ResultRecognized"/>. The provided
        /// <paramref name="cancellationToken"/> cancels the start operation only — call
        /// <see cref="StopAsync"/> to halt an already-running session.
        /// </summary>
        /// <param name="languageTag">BCP-47 language tag for recognition. <c>null</c> uses the system default.</param>
        Task StartAsync(string languageTag = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Halt an in-progress dictation session. Safe to call when not listening.
        /// </summary>
        Task StopAsync();
    }

    /// <summary>
    /// A finalised dictation hypothesis ready to be inserted into the editor.
    /// </summary>
    public sealed class DictationResult
    {
        public DictationResult(string text, double confidence)
        {
            Text = text;
            Confidence = confidence;
        }

        public string Text { get; }

        public double Confidence { get; }
    }

    /// <summary>
    /// Lifecycle event for <see cref="IDictationService"/>.
    /// </summary>
    public sealed class DictationStateChange
    {
        public DictationStateChange(DictationState state, string message = null)
        {
            State = state;
            Message = message;
        }

        public DictationState State { get; }

        public string Message { get; }
    }

    /// <summary>
    /// High-level dictation lifecycle states surfaced to the UI.
    /// </summary>
    public enum DictationState
    {
        Idle,
        Listening,
        Stopped,
        PermissionDenied,
        Unsupported,
        Error,
    }
}
