using Storylines.Services.Interfaces;
using Storylines.Models;
using Storylines.ViewModels;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Storylines.Helpers;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ExportDialogue : ContentDialog
    {
        public ExportDialogViewModel ViewModel { get; }

        public ExportDialogue(ExportTarget initialTarget = ExportTarget.None)
        {
            InitializeComponent();
            DialogHelper.EnsureXamlRoot(this);

            ViewModel = App.GetService<ExportDialogViewModel>();
            DataContext = ViewModel;

            InitializeClickOutToClose();

            RequestedTheme = App.GetService<WindowContext>().AppView.ActualTheme;

            AppView.currentlyOpenedDialogue = this;
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
                await new ExportDialogue(target).ShowAsync();
            }
            catch (Exception ex)
            {
                App.TryGetService<ILogger>()?.Warning($"Failed to open export dialog: {ex.Message}");
            }
        }

        private async void OnExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (await ViewModel.SubmitAsync())
                Hide();
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

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            App.GetService<WindowContext>().RootElement.PointerPressed -= OnWindowPointerPressed;
            AppView.currentlyOpenedDialogue = null;
        }

        private void ContentDialog_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && ViewModel.CanSubmit)
                OnExportButton_Click(sender, new RoutedEventArgs());
        }

        bool isFlyoutOpen = false;
        private void Flyout_Opened(object sender, object e) => isFlyoutOpen = true;

        private void Flyout_Closed(object sender, object e) => isFlyoutOpen = false;

        bool isHide = true;
        private void InitializeClickOutToClose()
        {
            App.GetService<WindowContext>().RootElement.PointerPressed += OnWindowPointerPressed;

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }

        private void OnWindowPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (isHide && !isFlyoutOpen)
                Hide();
        }
    }
}
