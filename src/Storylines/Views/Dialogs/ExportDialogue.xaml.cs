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
    public sealed partial class ExportDialogue : StorylinesContentDialog
    {
        public ExportDialogViewModel ViewModel { get; }

        public ExportDialogue(ExportTarget initialTarget = ExportTarget.None)
        {
            InitializeComponent();

            ViewModel = App.GetService<ExportDialogViewModel>();
            DataContext = ViewModel;

            CloseOnOutsideTap = true;
            ViewModel.Initialize(initialTarget);

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

        private async void OnExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (await ViewModel.SubmitAsync())
            {
                Hide();
                MicrosoftStoreFunctions.OnExportCompleted();
            }
        }

        private void OnChooseExportChaptersButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectTarget(ExportTarget.Chapters);
        }

        private void OnChooseExportDialoguesButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectTarget(ExportTarget.Dialogues);
        }

        private void OnChooseExportCharactersButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectTarget(ExportTarget.Characters);
        }

        private async void OnExportToLocationButton_Click(object sender, RoutedEventArgs e) => await ViewModel.PickFolderAsync();

        private async void OnExportLocationFrame_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => await ViewModel.PickFolderAsync();

        private void OnCancelButton_Click(object sender, RoutedEventArgs e) => Hide();

        private void OnErrorInfoBar_CloseButtonClick(Microsoft.UI.Xaml.Controls.InfoBar sender, object args)
        {
            ViewModel.DismissError();
        }

        private void ContentDialog_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && ViewModel.CanSubmit)
                OnExportButton_Click(sender, new RoutedEventArgs());
        }

        bool isFlyoutOpen = false;
        private void Flyout_Opened(object sender, object e) => isFlyoutOpen = true;

        private void Flyout_Closed(object sender, object e) => isFlyoutOpen = false;

        protected override bool CanCloseOnOutsideTap()
        {
            return !isFlyoutOpen;
        }
    }
}
