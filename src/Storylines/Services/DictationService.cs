using Storylines.Services.Interfaces;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;

namespace Storylines.Services
{
    /// <summary>
    /// UWP / WinUI 2 implementation of <see cref="IDictationService"/>. Uses
    /// <see cref="SpeechRecognizer"/> in continuous-dictation mode and pushes finalised
    /// hypotheses through <see cref="ResultRecognized"/>.
    /// </summary>
    internal sealed class DictationService : IDictationService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        private SpeechRecognizer _recognizer;
        private bool _disposed;

        public DictationService(ILogger logger)
        {
            _logger = logger;
        }

        public bool IsListening { get; private set; }

        public event Action<DictationResult> ResultRecognized;
        public event Action<DictationStateChange> StateChanged;

        public async Task StartAsync(string languageTag = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return;

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (IsListening)
                    return;

                cancellationToken.ThrowIfCancellationRequested();

                if (!await TryRequestMicrophoneAccessAsync().ConfigureAwait(false))
                {
                    RaiseStateChanged(DictationState.PermissionDenied);
                    return;
                }

                try
                {
                    _recognizer = CreateRecognizer(languageTag);
                    var compilation = await _recognizer.CompileConstraintsAsync();
                    if (compilation.Status != SpeechRecognitionResultStatus.Success)
                    {
                        RaiseStateChanged(DictationState.Error, $"Compile failed: {compilation.Status}");
                        await DisposeRecognizerAsync().ConfigureAwait(false);
                        return;
                    }

                    _recognizer.ContinuousRecognitionSession.ResultGenerated += OnResultGenerated;
                    _recognizer.ContinuousRecognitionSession.Completed += OnRecognitionCompleted;

                    await _recognizer.ContinuousRecognitionSession.StartAsync();
                    IsListening = true;
                    RaiseStateChanged(DictationState.Listening);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger?.Warning($"Dictation start unauthorized: {ex.Message}");
                    RaiseStateChanged(DictationState.PermissionDenied, ex.Message);
                    await DisposeRecognizerAsync().ConfigureAwait(false);
                }
                catch (NotSupportedException ex)
                {
                    _logger?.Warning($"Dictation not supported: {ex.Message}");
                    RaiseStateChanged(DictationState.Unsupported, ex.Message);
                    await DisposeRecognizerAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Dictation start failed: {ex.Message}");
                    RaiseStateChanged(DictationState.Error, ex.Message);
                    await DisposeRecognizerAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task StopAsync()
        {
            if (_disposed)
                return;

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsListening || _recognizer == null)
                    return;

                try
                {
                    if (_recognizer.State == SpeechRecognizerState.Capturing
                        || _recognizer.State == SpeechRecognizerState.Processing
                        || _recognizer.State == SpeechRecognizerState.SoundStarted
                        || _recognizer.State == SpeechRecognizerState.SpeechDetected)
                    {
                        await _recognizer.ContinuousRecognitionSession.StopAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Dictation stop failed: {ex.Message}");
                }
                finally
                {
                    IsListening = false;
                    await DisposeRecognizerAsync().ConfigureAwait(false);
                    RaiseStateChanged(DictationState.Stopped);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _ = DisposeRecognizerAsync();
            _gate.Dispose();
        }

        private SpeechRecognizer CreateRecognizer(string languageTag)
        {
            if (string.IsNullOrWhiteSpace(languageTag))
                return new SpeechRecognizer();

            try
            {
                return new SpeechRecognizer(new Windows.Globalization.Language(languageTag));
            }
            catch
            {
                return new SpeechRecognizer();
            }
        }

        private async Task<bool> TryRequestMicrophoneAccessAsync()
        {
            try
            {
                var capture = new Windows.Media.Capture.MediaCapture();
                await capture.InitializeAsync(new Windows.Media.Capture.MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Audio,
                });
                capture.Dispose();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Microphone probe failed: {ex.Message}");
                return false;
            }
        }

        private void OnResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if (args?.Result == null || string.IsNullOrEmpty(args.Result.Text))
                return;

            ResultRecognized?.Invoke(new DictationResult(args.Result.Text, args.Result.RawConfidence));
        }

        private void OnRecognitionCompleted(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            IsListening = false;
            RaiseStateChanged(DictationState.Stopped, args?.Status.ToString());
        }

        private async Task DisposeRecognizerAsync()
        {
            var recognizer = _recognizer;
            _recognizer = null;
            if (recognizer == null) return;

            try
            {
                recognizer.ContinuousRecognitionSession.ResultGenerated -= OnResultGenerated;
                recognizer.ContinuousRecognitionSession.Completed -= OnRecognitionCompleted;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Recognizer detach failed: {ex.Message}");
            }

            await Task.Yield();

            try
            {
                recognizer.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Recognizer dispose failed: {ex.Message}");
            }
        }

        private void RaiseStateChanged(DictationState state, string message = null)
            => StateChanged?.Invoke(new DictationStateChange(state, message));
    }
}
