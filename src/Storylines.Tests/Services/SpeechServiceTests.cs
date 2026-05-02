using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Tests.Stubs;
using Xunit;

namespace Storylines.Tests.Services;

public class SpeechServiceTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        var service = new SpeechService(new DictationServiceStub());
        Assert.Equal(SpeechMode.Idle, service.Mode);
    }

    [Fact]
    public void DictationStart_TransitionsModeToDictating()
    {
        var dictation = new DictationServiceStub();
        var service = new SpeechService(dictation);

        dictation.EmitStateChange(new DictationStateChange(DictationState.Listening));

        Assert.Equal(SpeechMode.Dictating, service.Mode);
    }

    [Fact]
    public void DictationStop_TransitionsModeBackToIdle()
    {
        var dictation = new DictationServiceStub();
        var service = new SpeechService(dictation);

        dictation.EmitStateChange(new DictationStateChange(DictationState.Listening));
        dictation.EmitStateChange(new DictationStateChange(DictationState.Stopped));

        Assert.Equal(SpeechMode.Idle, service.Mode);
    }

    [Fact]
    public void NotifyReadingStarted_SetsModeToReading()
    {
        var service = new SpeechService(new DictationServiceStub());
        service.NotifyReadingStarted();
        Assert.Equal(SpeechMode.Reading, service.Mode);
    }

    [Fact]
    public void NotifyReadingStopped_TransitionsBackToIdle()
    {
        var service = new SpeechService(new DictationServiceStub());

        service.NotifyReadingStarted();
        service.NotifyReadingStopped();

        Assert.Equal(SpeechMode.Idle, service.Mode);
    }

    [Fact]
    public void ModeChanged_FiresOnEachTransition()
    {
        var dictation = new DictationServiceStub();
        var service = new SpeechService(dictation);
        int events = 0;
        service.ModeChanged += _ => events++;

        service.NotifyReadingStarted();
        service.NotifyReadingStopped();
        dictation.EmitStateChange(new DictationStateChange(DictationState.Listening));

        Assert.Equal(3, events);
    }

    [Fact]
    public void PermissionDenied_DoesNotEnterDictatingMode()
    {
        var dictation = new DictationServiceStub();
        var service = new SpeechService(dictation);

        dictation.EmitStateChange(new DictationStateChange(DictationState.PermissionDenied, "denied"));

        Assert.Equal(SpeechMode.Idle, service.Mode);
    }
}
