
namespace Storylines.Services;

/// <summary>
/// Coordinates the read-aloud (TTS) and dictation (STT) capabilities so they never run
/// concurrently. Listens to lifecycle events from both sub-services and exposes a unified
/// <see cref="ISpeechService.Mode"/> for the UI to bind against.
/// </summary>
internal sealed class SpeechService : ISpeechService
{
    private SpeechMode _mode = SpeechMode.Idle;

    public SpeechService(IDictationService dictation, IReadAloudService readAloud)
    {
        Dictation = dictation;
        ReadAloud = readAloud;

        dictation.StateChanged += OnDictationStateChanged;
        readAloud.StateChanged += OnReadAloudStateChanged;
    }

    public IDictationService Dictation { get; }

    public IReadAloudService ReadAloud { get; }

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

    private void OnReadAloudStateChanged(ReadAloudState state)
    {
        switch (state)
        {
            case ReadAloudState.Loading:
            case ReadAloudState.Playing:
            case ReadAloudState.Paused:
                Mode = SpeechMode.Reading;
                break;
            case ReadAloudState.Idle:
                if (Mode == SpeechMode.Reading)
                    Mode = SpeechMode.Idle;
                break;
        }
    }
}
