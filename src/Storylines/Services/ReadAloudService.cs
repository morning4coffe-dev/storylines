using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Storylines.Services
{
    /// <summary>
    /// WinUI 3 / WinAppSDK implementation of <see cref="IReadAloudService"/>. Synthesises text via
    /// <see cref="SpeechSynthesizer"/> and plays the resulting audio stream through a headless
    /// <see cref="MediaPlayer"/>. Coordinates with <see cref="IDictationService"/> so the microphone
    /// and speakers do not contend.
    /// </summary>
    internal sealed class ReadAloudService : IReadAloudService, IDisposable
    {
        private static readonly string[] ParagraphSeparators = { "\r\n", "\n", "\r" };

        private readonly ILogger _logger;
        private readonly IAppSettingsService _settings;
        private readonly IDictationService _dictation;

        private readonly MediaPlayer _player;
        private SpeechSynthesizer _synthesizer;

        private List<string> _paragraphs = new();
        private int _index;
        private CancellationTokenSource _cts;
        private ReadAloudState _state = ReadAloudState.Idle;
        private bool _disposed;

        public ReadAloudService(ILogger logger, IAppSettingsService settings, IDictationService dictation)
        {
            _logger = logger;
            _settings = settings;
            _dictation = dictation;

            _player = new MediaPlayer { IsLoopingEnabled = false };
            _player.MediaEnded += OnMediaEnded;
            _player.MediaFailed += OnMediaFailed;
            _player.PlaybackSession.PositionChanged += OnPositionChanged;
            _player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
        }

        public ReadAloudState State
        {
            get => _state;
            private set
            {
                if (_state == value) return;
                _state = value;
                StateChanged?.Invoke(value);
            }
        }

        public double Progress { get; private set; }

        public int CurrentParagraphIndex => _index;

        public int TotalParagraphs => _paragraphs?.Count ?? 0;

        public event Action<ReadAloudState> StateChanged;

        public event Action<double> ProgressChanged;

        public event Action Completed;

        public Task SpeakAsync(string text, CancellationToken cancellationToken = default)
            => SpeakParagraphsAsync(new[] { text ?? string.Empty }, 0, cancellationToken);

        public async Task SpeakParagraphsAsync(IReadOnlyList<string> paragraphs, int startIndex = 0, CancellationToken cancellationToken = default)
        {
            if (_disposed) return;

            var prepared = SplitToParagraphs(paragraphs);
            if (prepared.Count == 0)
                return;

            // Halt any in-progress dictation before grabbing the audio path.
            if (_dictation.IsListening)
            {
                try { await _dictation.StopAsync().ConfigureAwait(false); }
                catch (Exception ex) { _logger?.Warning($"ReadAloud could not stop dictation: {ex.Message}"); }
            }

            StopInternal(notifyCompleted: false);

            _paragraphs = prepared;
            _index = Math.Clamp(startIndex, 0, prepared.Count - 1);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await PlayCurrentAsync().ConfigureAwait(false);
        }

        public async Task SpeakSampleAsync(CancellationToken cancellationToken = default)
        {
            const string sample = "The quick brown fox jumps over the lazy dog.";
            await SpeakAsync(sample, cancellationToken).ConfigureAwait(false);
        }

        public void Pause()
        {
            if (State != ReadAloudState.Playing) return;
            try { _player.Pause(); }
            catch (Exception ex) { _logger?.Warning($"ReadAloud pause failed: {ex.Message}"); }
        }

        public void Resume()
        {
            if (State != ReadAloudState.Paused) return;
            try { _player.Play(); }
            catch (Exception ex) { _logger?.Warning($"ReadAloud resume failed: {ex.Message}"); }
        }

        public void Stop() => StopInternal(notifyCompleted: false);

        public Task NextParagraphAsync()
        {
            if (_paragraphs.Count == 0) return Task.CompletedTask;
            _index = Math.Min(_index + 1, _paragraphs.Count);
            return _index >= _paragraphs.Count
                ? FinishAsync()
                : PlayCurrentAsync();
        }

        public Task PreviousParagraphAsync()
        {
            if (_paragraphs.Count == 0) return Task.CompletedTask;
            _index = Math.Max(_index - 1, 0);
            return PlayCurrentAsync();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _player.MediaEnded -= OnMediaEnded;
                _player.MediaFailed -= OnMediaFailed;
                _player.PlaybackSession.PositionChanged -= OnPositionChanged;
                _player.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
                _player.Dispose();
            }
            catch (Exception ex) { _logger?.Warning($"MediaPlayer dispose failed: {ex.Message}"); }

            DisposeSynthesizer();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task PlayCurrentAsync()
        {
            if (_disposed) return;
            if (_index < 0 || _index >= _paragraphs.Count)
            {
                await FinishAsync().ConfigureAwait(false);
                return;
            }

            var text = _paragraphs[_index];
            if (string.IsNullOrWhiteSpace(text))
            {
                _index++;
                await PlayCurrentAsync().ConfigureAwait(false);
                return;
            }

            State = ReadAloudState.Loading;
            Progress = 0;
            ProgressChanged?.Invoke(0);

            SpeechSynthesisStream stream = null;
            try
            {
                stream = await SynthesizeAsync(text).ConfigureAwait(false);
                if (stream is null)
                {
                    await FinishAsync().ConfigureAwait(false);
                    return;
                }

                _player.Volume = Math.Clamp(_settings.ReadAloudVolume / 100.0, 0.0, 1.0);
                _player.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
                _player.Play();
            }
            catch (OperationCanceledException) { /* user-cancelled */ }
            catch (Exception ex)
            {
                _logger?.Error($"ReadAloud synthesis failed: {ex.Message}", ex);
                stream?.Dispose();
                await FinishAsync().ConfigureAwait(false);
            }
        }

        private async Task<SpeechSynthesisStream> SynthesizeAsync(string text)
        {
            var synth = GetSynthesizer();
            return await synth.SynthesizeTextToStreamAsync(text);
        }

        private SpeechSynthesizer GetSynthesizer()
        {
            // Recreate if voice/rate/pitch settings would change. Keep one alive across paragraphs of the same session.
            _synthesizer ??= new SpeechSynthesizer();

            var voiceId = _settings.ReadAloudVoiceId;
            var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id == voiceId)
                        ?? SpeechSynthesizer.DefaultVoice;
            if (voice is not null && _synthesizer.Voice?.Id != voice.Id)
                _synthesizer.Voice = voice;

            _synthesizer.Options.SpeakingRate = Math.Clamp(_settings.ReadAloudRate, 0.5, 2.0);
            _synthesizer.Options.AudioPitch = Math.Clamp(_settings.ReadAloudPitch, 0.0, 2.0);
            return _synthesizer;
        }

        private Task FinishAsync()
        {
            StopInternal(notifyCompleted: true);
            return Task.CompletedTask;
        }

        private void StopInternal(bool notifyCompleted)
        {
            try
            {
                if (_player.PlaybackSession.PlaybackState != MediaPlaybackState.None
                    && _player.PlaybackSession.PlaybackState != MediaPlaybackState.Paused)
                {
                    _player.Pause();
                }
                _player.Source = null;
            }
            catch (Exception ex) { _logger?.Warning($"ReadAloud stop failed: {ex.Message}"); }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            Progress = 0;
            ProgressChanged?.Invoke(0);
            State = ReadAloudState.Idle;
            if (notifyCompleted)
                Completed?.Invoke();
        }

        private void OnMediaEnded(MediaPlayer sender, object args)
        {
            _index++;
            if (_index >= _paragraphs.Count)
                _ = FinishAsync();
            else
                _ = PlayCurrentAsync();
        }

        private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            _logger?.Warning($"ReadAloud media failed: {args.Error} {args.ErrorMessage}");
            _ = FinishAsync();
        }

        private void OnPositionChanged(MediaPlaybackSession session, object args)
        {
            try
            {
                var duration = session.NaturalDuration.TotalSeconds;
                if (duration <= 0) return;
                Progress = Math.Clamp(session.Position.TotalSeconds / duration, 0.0, 1.0);
                ProgressChanged?.Invoke(Progress);
            }
            catch { /* session may be torn down */ }
        }

        private void OnPlaybackStateChanged(MediaPlaybackSession session, object args)
        {
            switch (session.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    State = ReadAloudState.Playing;
                    break;
                case MediaPlaybackState.Paused when State == ReadAloudState.Playing:
                    State = ReadAloudState.Paused;
                    break;
            }
        }

        private static List<string> SplitToParagraphs(IReadOnlyList<string> input)
        {
            var result = new List<string>();
            if (input is null) return result;

            foreach (var raw in input)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                foreach (var line in raw.Split(ParagraphSeparators, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        result.Add(trimmed);
                }
            }
            return result;
        }

        private void DisposeSynthesizer()
        {
            try { _synthesizer?.Dispose(); }
            catch (Exception ex) { _logger?.Warning($"SpeechSynthesizer dispose failed: {ex.Message}"); }
            _synthesizer = null;
        }
    }
}
