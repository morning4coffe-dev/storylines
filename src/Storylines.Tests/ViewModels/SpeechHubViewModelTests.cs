using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Tests.Stubs;
using Storylines.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Storylines.Tests.ViewModels;

public class SpeechHubViewModelTests
{
    [Fact]
    public void DictationResult_InsertsTextWithTrailingSpace()
    {
        var dictation = new DictationServiceStub();
        var readAloud = new ReadAloudServiceStub();
        var speech = new SpeechService(dictation, readAloud);
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
        var readAloud = new ReadAloudServiceStub();
        var speech = new SpeechService(dictation, readAloud);
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
        var readAloud = new ReadAloudServiceStub();
        var speech = new SpeechService(dictation, readAloud);
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
        var readAloud = new ReadAloudServiceStub();
        var speech = new SpeechService(dictation, readAloud);
        var editor = new TextEditorServiceStub();
        var settings = new AppSettingsServiceStub();
        var vm = new SpeechHubViewModel(speech, editor, settings);

        dictation.EmitStateChange(new DictationStateChange(DictationState.PermissionDenied, "denied"));

        Assert.True(vm.IsPermissionDenied);
        Assert.Contains("denied", vm.StatusMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PermissionDenied_ShowsLocalizedWarningNotification()
    {
        var dictation = new DictationServiceStub();
        var readAloud = new ReadAloudServiceStub();
        var speech = new SpeechService(dictation, readAloud);
        var editor = new TextEditorServiceStub();
        var settings = new AppSettingsServiceStub();
        var notifications = new NotificationServiceSpy();
        var vm = new SpeechHubViewModel(speech, editor, settings, notifications);

        dictation.EmitStateChange(new DictationStateChange(DictationState.PermissionDenied, "denied"));

        Assert.NotNull(notifications.LastNotification);
        Assert.Equal(InfoBarSeverity.Warning, notifications.LastNotification!.Severity);
        Assert.Equal("Microphone access denied", notifications.LastNotification.Title);
        Assert.Equal("Grant microphone access in Windows Settings to use dictation.", notifications.LastNotification.Message);
        Assert.True(notifications.LastNotification.Duration.HasValue);
    }

    private sealed class NotificationServiceSpy : INotificationService
    {
        public NotificationRequest? LastNotification { get; private set; }

        public void ShowNotification(NotificationRequest notification)
        {
            LastNotification = notification;
        }

        public void ShowNotification(InfoBarSeverity severity, string title, string message = "")
        {
            LastNotification = new NotificationRequest
            {
                Severity = severity,
                Title = title,
                Message = message
            };
        }

        public void ShowProgressBar(bool isIndeterminate)
        {
        }

        public void UpdateProgressBar(int value, ProgressBarState state = ProgressBarState.Normal)
        {
        }

        public void HideProgressBar()
        {
        }

        public void ShowPersistentNotification(PersistentNotificationRequest request)
        {
        }

        public void UpdatePersistentNotificationProgress(double value, bool isIndeterminate = false)
        {
        }

        public void DismissPersistentNotification()
        {
        }
    }
}
