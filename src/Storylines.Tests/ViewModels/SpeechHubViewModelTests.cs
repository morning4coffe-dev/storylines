using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Tests.Stubs;
using Storylines.ViewModels;
using Xunit;

namespace Storylines.Tests.ViewModels;

public class SpeechHubViewModelTests
{
    [Fact]
    public void DictationResult_InsertsTextWithTrailingSpace()
    {
        var dictation = new DictationServiceStub();
        var speech = new SpeechService(dictation);
        var editor = new TextEditorServiceStub();
        var settings = new AppSettingsServiceStub();
        var vm = new SpeechHubViewModel(speech, editor, settings);

        dictation.EmitResult("hello world");

        Assert.Single(editor.InsertedFragments);
        Assert.Equal("hello world ", editor.InsertedFragments[0]);
    }

    [Fact]
    public void DictationResult_EmptyText_DoesNotInsert()
    {
        var dictation = new DictationServiceStub();
        var speech = new SpeechService(dictation);
        var editor = new TextEditorServiceStub();
        var settings = new AppSettingsServiceStub();
        var vm = new SpeechHubViewModel(speech, editor, settings);

        dictation.EmitResult(string.Empty);

        Assert.Empty(editor.InsertedFragments);
    }

    [Fact]
    public void Mode_ReflectsSpeechServiceState()
    {
        var dictation = new DictationServiceStub();
        var speech = new SpeechService(dictation);
        var editor = new TextEditorServiceStub();
        var settings = new AppSettingsServiceStub();
        var vm = new SpeechHubViewModel(speech, editor, settings);

        dictation.EmitStateChange(new DictationStateChange(DictationState.Listening));

        Assert.Equal(SpeechMode.Dictating, vm.Mode);
        Assert.True(vm.IsDictating);
        Assert.False(vm.IsReading);
        Assert.False(vm.IsIdle);
    }

    [Fact]
    public void PermissionDenied_FlagsSurfaceState()
    {
        var dictation = new DictationServiceStub();
        var speech = new SpeechService(dictation);
        var editor = new TextEditorServiceStub();
        var settings = new AppSettingsServiceStub();
        var vm = new SpeechHubViewModel(speech, editor, settings);

        dictation.EmitStateChange(new DictationStateChange(DictationState.PermissionDenied, "denied"));

        Assert.True(vm.IsPermissionDenied);
        Assert.Contains("denied", vm.StatusMessage, System.StringComparison.OrdinalIgnoreCase);
    }
}
