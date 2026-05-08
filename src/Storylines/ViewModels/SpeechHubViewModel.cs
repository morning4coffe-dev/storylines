using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Storylines.Constants;
using Storylines.Helpers;
using Storylines.Resources;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Storylines.ViewModels
{
    /// <summary>
    /// Drives the unified Speech Hub toolbar group: a single mic + speaker pair that toggles
    /// dictation and read-aloud through <see cref="ISpeechService"/>. Inserts recognised speech
    /// into the editor via <see cref="ITextEditorService"/> so views never touch the recognizer
    /// or media player directly.
    /// </summary>
    public partial class SpeechHubViewModel : ObservableObject
    {
        private const double LowConfidenceThreshold = 0.3;
        private static readonly TimeSpan PermissionDeniedNotificationDuration = TimeSpan.FromSeconds(LayoutConstants.NotificationDismissSeconds + 4);

        private readonly ISpeechService _speech;
        private readonly ITextEditorService _textEditor;
        private readonly IAppSettingsService _settings;
        private readonly INotificationService _notifications;
        private readonly ResourceLoader _resources;

        [ObservableProperty]
        private SpeechMode _mode;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isPermissionDenied;

        [ObservableProperty]
        private ReadAloudState _readAloudState;

        [ObservableProperty]
        private double _readAloudProgress;

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
            _resources = ResourceLoader.GetForViewIndependentUse();

            _mode = _speech.Mode;
            _readAloudState = _speech.ReadAloud.State;

            _speech.ModeChanged += HandleModeChanged;
            _speech.Dictation.ResultRecognized += OnDictationResult;
            _speech.Dictation.StateChanged += OnDictationStateChanged;
            _speech.ReadAloud.StateChanged += HandleReadAloudStateChanged;
            _speech.ReadAloud.ProgressChanged += HandleReadAloudProgressChanged;
        }

        public bool IsDictating => Mode == SpeechMode.Dictating;
        public bool IsReading => Mode == SpeechMode.Reading;
        public bool IsIdle => Mode == SpeechMode.Idle;

        public bool CanShowReadAloudControls => ReadAloudState is ReadAloudState.Loading or ReadAloudState.Playing or ReadAloudState.Paused;
        public bool IsReadAloudPaused => ReadAloudState == ReadAloudState.Paused;
        public bool IsReadAloudPlaying => ReadAloudState == ReadAloudState.Playing;

        public Visibility ReadAloudControlsVisibility => CanShowReadAloudControls ? Visibility.Visible : Visibility.Collapsed;

        [RelayCommand]
        private async Task ToggleDictationAsync()
        {
            if (_speech.Dictation.IsListening)
            {
                await _speech.Dictation.StopAsync().ConfigureAwait(false);
                return;
            }

            // Mutual-exclusion: stop reading first if it is active.
            if (_speech.Mode == SpeechMode.Reading)
                _speech.ReadAloud.Stop();

            IsPermissionDenied = false;
            var languageTag = ResolveDictationLanguageTag();
            await _speech.Dictation.StartAsync(languageTag).ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task StartReadAloudAsync()
        {
            // If a session is already active, treat the toolbar button as a stop toggle.
            if (CanShowReadAloudControls)
            {
                _speech.ReadAloud.Stop();
                return;
            }

            var paragraphs = ResolveTextToRead();
            if (paragraphs.Count == 0)
            {
                string title = _resources.GetString("speechReadAloudNoTextTitle");
                string message = _resources.GetString("speechReadAloudNoTextMessage");

                StatusMessage = message;
                _notifications?.ShowNotification(
                    Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                    title,
                    message);
                return;
            }

            await _speech.ReadAloud.SpeakParagraphsAsync(paragraphs).ConfigureAwait(false);
        }

        [RelayCommand]
        private void PauseReadAloud() => _speech.ReadAloud.Pause();

        [RelayCommand]
        private void ResumeReadAloud() => _speech.ReadAloud.Resume();

        [RelayCommand]
        private void StopReadAloud() => _speech.ReadAloud.Stop();

        [RelayCommand]
        private Task NextParagraphAsync() => _speech.ReadAloud.NextParagraphAsync();

        [RelayCommand]
        private Task PreviousParagraphAsync() => _speech.ReadAloud.PreviousParagraphAsync();

        private IReadOnlyList<string> ResolveTextToRead()
        {
            var selection = _textEditor.GetSelectedText();
            var source = string.IsNullOrWhiteSpace(selection)
                ? _textEditor.GetText(TextFormat.PlainText)
                : selection;

            if (string.IsNullOrWhiteSpace(source))
                return Array.Empty<string>();

            return new[] { source };
        }

        private string ResolveDictationLanguageTag()
        {
            if (!string.IsNullOrWhiteSpace(_settings.DictationLanguage))
                return _settings.DictationLanguage;

            return string.IsNullOrWhiteSpace(_settings.UserLanguage)
                ? null
                : _settings.UserLanguage;
        }

        private void HandleModeChanged(SpeechMode mode)
        {
            Mode = mode;
            OnPropertyChanged(nameof(IsDictating));
            OnPropertyChanged(nameof(IsReading));
            OnPropertyChanged(nameof(IsIdle));
        }

        private void OnDictationResult(DictationResult result)
        {
            if (result is null || string.IsNullOrEmpty(result.Text))
                return;

            // Drop low-confidence hypotheses so background noise does not pollute the editor.
            if (result.Confidence < LowConfidenceThreshold)
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
                    StatusMessage = SpeechHubStrings.DictationPermissionDeniedStatus;
                    NotificationManager.ClearBadgeNotification();
                    _notifications?.ShowNotification(new NotificationRequest
                    {
                        Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                        Title = SpeechHubStrings.DictationPermissionDeniedTitle,
                        Message = SpeechHubStrings.DictationPermissionDeniedMessage,
                        Duration = PermissionDeniedNotificationDuration
                    });
                    break;
                case DictationState.Unsupported:
                    StatusMessage = SpeechHubStrings.DictationUnsupportedStatus;
                    NotificationManager.ClearBadgeNotification();
                    break;
                case DictationState.Error:
                    StatusMessage = string.IsNullOrWhiteSpace(change.Message)
                        ? SpeechHubStrings.DictationErrorStatus
                        : change.Message;
                    NotificationManager.ClearBadgeNotification();
                    break;
                case DictationState.Listening:
                    StatusMessage = SpeechHubStrings.DictationListeningStatus;
                    SafeDisplayBadge("alert");
                    break;
                case DictationState.Stopped:
                    StatusMessage = string.Empty;
                    NotificationManager.ClearBadgeNotification();
                    break;
            }
        }

        private void HandleReadAloudStateChanged(ReadAloudState state)
        {
            ReadAloudState = state;
            OnPropertyChanged(nameof(CanShowReadAloudControls));
            OnPropertyChanged(nameof(IsReadAloudPaused));
            OnPropertyChanged(nameof(IsReadAloudPlaying));
            OnPropertyChanged(nameof(ReadAloudControlsVisibility));

            if (state == ReadAloudState.Playing)
                SafeDisplayBadge("playing");
            else if (state == ReadAloudState.Idle)
                NotificationManager.ClearBadgeNotification();
        }

        private void HandleReadAloudProgressChanged(double value) => ReadAloudProgress = value;

        private static void SafeDisplayBadge(string glyph)
        {
            try { NotificationManager.DisplayBadgeNotification(glyph); }
            catch { /* unpackaged or no notifications API */ }
        }
    }
}
