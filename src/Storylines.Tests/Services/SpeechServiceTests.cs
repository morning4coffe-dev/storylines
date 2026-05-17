using Storylines.Tests.Stubs;
using Xunit;

namespace Storylines.Tests.Services;

public class SpeechServiceTests
{
    private static SpeechService CreateService(out DictationServiceStub dictation, out ReadAloudServiceStub readAloud)
    {
        dictation = new DictationServiceStub();
        readAloud = new ReadAloudServiceStub();
        return new SpeechService(dictation, readAloud);
    }

    [Fact]
    public void InitialState_IsIdle()
    {
        var service = CreateService(out _, out _);
        Assert.Equal(SpeechMode.Idle, service.Mode);
    }

    [Fact]
    public void DictationStart_TransitionsModeToDictating()
    {
        var service = CreateService(out var dictation, out _);

        dictation.EmitStateChange(new DictationStateChange(DictationState.Listening));

        Assert.Equal(SpeechMode.Dictating, service.Mode);
    }

    [Fact]
    public void DictationStop_TransitionsModeBackToIdle()
    {
        var service = CreateService(out var dictation, out _);

        dictation.EmitStateChange(new DictationStateChange(DictationState.Listening));
        dictation.EmitStateChange(new DictationStateChange(DictationState.Stopped));

        Assert.Equal(SpeechMode.Idle, service.Mode);
    }

    [Fact]
    public void ReadAloudPlaying_TransitionsModeToReading()
    {
        var service = CreateService(out _, out var readAloud);
        readAloud.EmitStateChange(ReadAloudState.Playing);

        Assert.Equal(SpeechMode.Reading, service.Mode);
    }

    [Fact]
    public void ReadAloudIdle_TransitionsBackToIdle()
    {
        var service = CreateService(out _, out var readAloud);

        readAloud.EmitStateChange(ReadAloudState.Playing);
        readAloud.EmitStateChange(ReadAloudState.Idle);

        Assert.Equal(SpeechMode.Idle, service.Mode);
    }

    [Fact]
    public void ModeChanged_FiresOnEachTransition()
    {
        var service = CreateService(out var dictation, out var readAloud);
        int events = 0;
        service.ModeChanged += _ => events++;

        readAloud.EmitStateChange(ReadAloudState.Playing);
        readAloud.EmitStateChange(ReadAloudState.Idle);
        dictation.EmitStateChange(new DictationStateChange(DictationState.Listening));

        Assert.Equal(3, events);
    }

    [Fact]
    public void PermissionDenied_DoesNotEnterDictatingMode()
    {
        var service = CreateService(out var dictation, out _);

        dictation.EmitStateChange(new DictationStateChange(DictationState.PermissionDenied, "denied"));

        Assert.Equal(SpeechMode.Idle, service.Mode);
    }
}
