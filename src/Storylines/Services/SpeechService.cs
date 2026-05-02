using Storylines.Services.Interfaces;
using System;

namespace Storylines.Services
{
    /// <summary>
    /// Coordinates the read-aloud (TTS) and dictation (STT) capabilities so they never run
    /// concurrently. The actual TTS work currently lives in the command-bar code-behind; this
    /// service tracks lifecycle for the UI and exposes the unified <see cref="ISpeechService.Mode"/>
    /// state. As Phase 6 lands, the TTS path will move behind an <c>IReadAloudService</c> and be
    /// driven through this same coordinator.
    /// </summary>
    internal sealed class SpeechService : ISpeechService
    {
        private SpeechMode _mode = SpeechMode.Idle;

        public SpeechService(IDictationService dictation)
        {
            Dictation = dictation;
            dictation.StateChanged += OnDictationStateChanged;
        }

        public IDictationService Dictation { get; }

        public SpeechMode Mode
        {
            get => _mode;
            private set
            {
                if (_mode == value) return;
                _mode = value;
                ModeChanged?.Invoke(value);
            }
        }

        public event Action<SpeechMode> ModeChanged;

        public void NotifyReadingStarted() => Mode = SpeechMode.Reading;

        public void NotifyReadingStopped()
        {
            if (Mode == SpeechMode.Reading)
                Mode = SpeechMode.Idle;
        }

        private void OnDictationStateChanged(DictationStateChange change)
        {
            switch (change.State)
            {
                case DictationState.Listening:
                    Mode = SpeechMode.Dictating;
                    break;
                case DictationState.Stopped:
                case DictationState.PermissionDenied:
                case DictationState.Unsupported:
                case DictationState.Error:
                    if (Mode == SpeechMode.Dictating)
                        Mode = SpeechMode.Idle;
                    break;
            }
        }
    }
}
