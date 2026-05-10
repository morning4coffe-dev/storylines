using Storylines.Services.Interfaces;
using Storylines.Models;
using Storylines.ViewModels;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Storylines.Services;
using Storylines.Helpers;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ExportDialogue : AppShellDialog
    {
        public ExportDialogViewModel ViewModel { get; }

        public ExportDialogue(ExportTarget initialTarget = ExportTarget.None)
        {
            InitializeComponent();

            ViewModel = App.GetService<ExportDialogViewModel>();
            DataContext = ViewModel;

            DialogTitle = Storylines.Resources.ExportDialogue.Title;
            PrimaryActionText = Storylines.Resources.ExportDialogue.Submit;
            PrimaryActionGlyph = "\uE792";
            CloseOnOutsideTap = true;
            ViewModel.Initialize(initialTarget);
            IsPrimaryActionEnabled = ViewModel.CanSubmit;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateCardHighlightStates();

            if (initialTarget != ExportTarget.None)
                chooseWhatToExportAnimation.FromVerticalOffset = 0;
        }

        public static void Open(ExportTarget target = ExportTarget.None)
            => _ = OpenAsync(target);

        public static async Task OpenAsync(ExportTarget target = ExportTarget.None)
        {
            try
            {
                await App.GetService<IDialogService>().ShowAsync(new ExportDialogue(target));
            }
            catch (Exception ex)
            {
                App.TryGetService<ILogger>()?.Warning($"Failed to open export dialog: {ex.Message}");
            }
        }

        protected override async Task<bool> ExecutePrimaryActionAsync()
        {
            if (await ViewModel.SubmitAsync())
            {
                MicrosoftStoreFunctions.OnExportCompleted();
                return true;
            }

            return false;
        }

        private void OnChooseExportChaptersButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectTarget(ExportTarget.Chapters);
            UpdateCardHighlightStates();
        }

        private void OnChooseExportDialoguesButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectTarget(ExportTarget.Dialogues);
            UpdateCardHighlightStates();
        }

        private void OnChooseExportCharactersButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectTarget(ExportTarget.Characters);
            UpdateCardHighlightStates();
        }

        private async void OnExportToLocationButton_Click(object sender, RoutedEventArgs e) => await ViewModel.PickFolderAsync();

        private async void OnExportLocationFrame_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => await ViewModel.PickFolderAsync();

        private void OnErrorInfoBar_CloseButtonClick(Microsoft.UI.Xaml.Controls.InfoBar sender, object args)
        {
            ViewModel.DismissError();
        }

        private void Flyout_Opened(object sender, object e) => NotifyTransientOpened();

        private void Flyout_Closed(object sender, object e) => NotifyTransientClosed();

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.CanSubmit))
                IsPrimaryActionEnabled = ViewModel.CanSubmit;

            if (e.PropertyName == nameof(ViewModel.IsChaptersSelected)
                || e.PropertyName == nameof(ViewModel.IsDialoguesSelected)
                || e.PropertyName == nameof(ViewModel.IsCharactersSelected))
            {
                UpdateCardHighlightStates();
            }
        }

        private void UpdateCardHighlightStates()
        {
            UpdateCardHighlight(chooseExportChaptersButton, ViewModel.IsChaptersSelected);
            UpdateCardHighlight(chooseExportDialoguesButton, ViewModel.IsDialoguesSelected);
            UpdateCardHighlight(chooseExportCharactersButton, ViewModel.IsCharactersSelected);
        }

        private static void UpdateCardHighlight(Button card, bool selected)
        {
            card.BorderThickness = selected
                ? new Thickness(2)
                : new Thickness(1);
            card.BorderBrush = selected
                ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemControlHighlightAccentBrush"]
                : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
        }
    }
}
