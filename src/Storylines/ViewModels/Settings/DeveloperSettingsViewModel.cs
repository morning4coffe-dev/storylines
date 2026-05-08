using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Storylines.Helpers;
using Storylines.Services.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Resources;

namespace Storylines.ViewModels.Settings
{
    public partial class DeveloperSettingsViewModel : ObservableObject
    {
        private static readonly ResourceLoader _resources = ResourceLoader.GetForViewIndependentUse();

        private readonly IDeveloperDiagnosticsService _developerDiagnostics;
        private readonly INotificationService _notifications;
        private readonly IDialogService _dialogs;

        [ObservableProperty]
        private string _currentPage = string.Empty;

        [ObservableProperty]
        private string _currentTheme = string.Empty;

        [ObservableProperty]
        private string _windowSize = string.Empty;

        [ObservableProperty]
        private string _currentDialog = string.Empty;

        [ObservableProperty]
        private int _invisibleControlCount;

        [ObservableProperty]
        private int _attachedBehaviorCount;

        public ObservableCollection<DeveloperDiagnosticItem> InvisibleControls { get; } = new ObservableCollection<DeveloperDiagnosticItem>();
        public ObservableCollection<DeveloperDiagnosticItem> AttachedBehaviors { get; } = new ObservableCollection<DeveloperDiagnosticItem>();

        public DeveloperSettingsViewModel(
            IDeveloperDiagnosticsService developerDiagnostics,
            INotificationService notifications,
            IDialogService dialogs)
        {
            _developerDiagnostics = developerDiagnostics;
            _notifications = notifications;
            _dialogs = dialogs;
        }

        [RelayCommand]
        private void RefreshSnapshot()
        {
            var snapshot = _developerDiagnostics.CaptureSnapshot();

            CurrentPage = string.IsNullOrWhiteSpace(snapshot.CurrentPage)
                ? _resources.GetString("developerUnknownValue")
                : snapshot.CurrentPage;
            CurrentTheme = snapshot.CurrentTheme;
            WindowSize = string.IsNullOrWhiteSpace(snapshot.WindowSize)
                ? _resources.GetString("developerUnknownValue")
                : snapshot.WindowSize;
            CurrentDialog = snapshot.CurrentDialog == "None"
                ? _resources.GetString("developerNoDialogOpen")
                : snapshot.CurrentDialog;
            InvisibleControlCount = snapshot.InvisibleControlCount;
            AttachedBehaviorCount = snapshot.AttachedBehaviorCount;

            ReplaceItems(InvisibleControls, snapshot.InvisibleControls);
            ReplaceItems(AttachedBehaviors, snapshot.AttachedBehaviors);
        }

        [RelayCommand]
        private void ShowTransientNotification()
        {
            _notifications.ShowNotification(
                InfoBarSeverity.Informational,
                _resources.GetString("developerTransientNotificationTitle"),
                _resources.GetString("developerTransientNotificationMessage"));
        }

        [RelayCommand]
        private void ShowReviewPrompt()
        {
            MicrosoftStoreFunctions.ShowReviewPromptPreview();
            RefreshSnapshot();
        }

        [RelayCommand]
        private void DismissPersistentNotification()
        {
            _notifications.DismissPersistentNotification();
            RefreshSnapshot();
        }

        [RelayCommand]
        private void OpenLoadDialog()
        {
            _dialogs.OpenLoadDialogue();
            RefreshSnapshot();
        }

        [RelayCommand]
        private void OpenShortcutsDialog()
        {
            _dialogs.OpenShortcuts();
            RefreshSnapshot();
        }

        [RelayCommand]
        private void OpenModePickerDialog()
        {
            _dialogs.OpenModePicker();
            RefreshSnapshot();
        }

        [RelayCommand]
        private void OpenProjectInfoDialog()
        {
            _dialogs.OpenProjectFileInfo();
            RefreshSnapshot();
        }

        private static void ReplaceItems(
            ObservableCollection<DeveloperDiagnosticItem> collection,
            IEnumerable<DeveloperDiagnosticItem> items)
        {
            collection.Clear();
            foreach (var item in items)
                collection.Add(item);
        }
    }
}
