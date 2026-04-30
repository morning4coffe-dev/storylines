using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Storylines.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Storylines.ViewModels
{
    /// <summary>
    /// Drives the unified Speech Hub toolbar group: a single mic + speaker pair that toggles
    /// dictation and read-aloud through <see cref="ISpeechService"/>. Inserts recognised speech
    /// into the editor via <see cref="ITextEditorService"/> so views never touch the recognizer
    /// directly.
    /// </summary>
    public partial class SpeechHubViewModel : ObservableObject
    {
        private readonly ISpeechService _speech;
        private readonly ITextEditorService _textEditor;
        private readonly IAppSettingsService _settings;
        private readonly INotificationService _notifications;

        [ObservableProperty]
        private SpeechMode _mode;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isPermissionDenied;

        public SpeechHubViewModel(
            ISpeechService speech,
            ITextEditorService textEditor,
            IAppSettingsService settings,
            INotificationService notifications = null)
        {
            _speech = speech;
            _textEditor = textEditor;
            _settings = settings;
            _notifications = notifications;

            _mode = _speech.Mode;
            _speech.ModeChanged += OnModeChanged;
            _speech.Dictation.ResultRecognized += OnDictationResult;
            _speech.Dictation.StateChanged += OnDictationStateChanged;
        }

        public bool IsDictating => Mode == SpeechMode.Dictating;
        public bool IsReading => Mode == SpeechMode.Reading;
        public bool IsIdle => Mode == SpeechMode.Idle;

        [RelayCommand]
        private async Task ToggleDictationAsync()
        {
            if (_speech.Dictation.IsListening)
            {
                await _speech.Dictation.StopAsync().ConfigureAwait(false);
                return;
            }

            // Mutual-exclusion: stop reading first if it is active. The current TTS path lives
            // in the command-bar code-behind and listens to ISpeechService mode changes there.
            if (_speech.Mode == SpeechMode.Reading && _speech is SpeechService service)
                service.NotifyReadingStopped();

            IsPermissionDenied = false;
            var languageTag = string.IsNullOrWhiteSpace(_settings.UserLanguage)
                ? null
                : _settings.UserLanguage;

            await _speech.Dictation.StartAsync(languageTag).ConfigureAwait(false);
        }

        private void OnModeChanged(SpeechMode mode)
        {
            Mode = mode;
            OnPropertyChanged(nameof(IsDictating));
            OnPropertyChanged(nameof(IsReading));
            OnPropertyChanged(nameof(IsIdle));
        }

        private void OnDictationResult(DictationResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.Text))
                return;

            // Append a trailing space so consecutive utterances do not run together.
            _textEditor.InsertTextAtCaret(result.Text + " ");
        }

        private void OnDictationStateChanged(DictationStateChange change)
        {
            switch (change.State)
            {
                case DictationState.PermissionDenied:
                    IsPermissionDenied = true;
                    StatusMessage = "Microphone access denied.";
                    _notifications?.ShowNotification(
                        Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                        "Microphone access denied",
                        "Grant microphone access in Windows Settings to use dictation.");
                    break;
                case DictationState.Unsupported:
                    StatusMessage = "Dictation is not available on this device.";
                    break;
                case DictationState.Error:
                    StatusMessage = string.IsNullOrWhiteSpace(change.Message)
                        ? "Dictation error."
                        : change.Message;
                    break;
                case DictationState.Listening:
                    StatusMessage = "Listening…";
                    break;
                case DictationState.Stopped:
                    StatusMessage = string.Empty;
                    break;
            }
        }
    }
}
