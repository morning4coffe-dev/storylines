using Storylines.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Storylines.Tests.Stubs;

/// <summary>
/// Test double for <see cref="IDictationService"/> that lets tests drive lifecycle and result
/// events without a real <c>SpeechRecognizer</c>.
/// </summary>
internal sealed class DictationServiceStub : IDictationService
{
    public bool IsListening { get; private set; }
    public event Action<DictationResult> ResultRecognized;
    public event Action<DictationStateChange> StateChanged;

    public Task StartAsync(string languageTag = null, CancellationToken cancellationToken = default)
    {
        IsListening = true;
        StateChanged?.Invoke(new DictationStateChange(DictationState.Listening));
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        IsListening = false;
        StateChanged?.Invoke(new DictationStateChange(DictationState.Stopped));
        return Task.CompletedTask;
    }

    public void EmitResult(string text, double confidence = 1.0)
        => ResultRecognized?.Invoke(new DictationResult(text, confidence));

    public void EmitStateChange(DictationStateChange change)
        => StateChanged?.Invoke(change);
}
