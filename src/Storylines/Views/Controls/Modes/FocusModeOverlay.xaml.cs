using Storylines.Services;
using Storylines.ViewModels;
using Storylines.ViewModels.Modes;
using Storylines.Views.Controls;
using Storylines.Views.Pages;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace Storylines.Views.Controls.Modes
{
    public sealed partial class FocusModeOverlay : UserControl
    {
        // Exposed as a field so x:Bind in XAML can reach it without a full VM wrapper.
        internal readonly CommandBarViewModel _cmdBarVm;
        internal readonly ChaptersListViewModel _chaptersVm;
        private readonly WindowContext _windowContext;

        public FocusModeOverlay(FocusModeViewModel vm)
        {
            _cmdBarVm = App.GetService<CommandBarViewModel>();
            _chaptersVm = App.GetService<ChaptersListViewModel>();
            _windowContext = App.GetService<WindowContext>();
            InitializeComponent();

            var resources = ResourceLoader.GetForViewIndependentUse();
            modeTitleText.Text = resources.GetString("FocusMode.Label");

            var leaveLabel = resources.GetString("FocusModeLeaveDialogueLeave");
            ToolTipService.SetToolTip(leaveFocusModeButton, leaveLabel);
            AutomationProperties.SetName(leaveFocusModeButton, leaveLabel);
        }

        private void OnAutosaveToggle_Click(object sender, RoutedEventArgs e)
        {
            _cmdBarVm.ToggleAutosaveCommand.Execute(null);
        }

        private void OnReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            var speechHub = App.GetService<SpeechHubViewModel>();
            if (speechHub.StartReadAloudCommand.CanExecute(null))
                speechHub.StartReadAloudCommand.Execute(null);
        }

        private void OnLeaveFocusModeButton_Click(object sender, RoutedEventArgs e)
        {
            _windowContext?.AppView?.TryExitActiveMode();
        }

        private void OnCreateChapterHyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            _chaptersVm.OpenCreateChapterDialogCommand.Execute(null);
        }
    }
}
