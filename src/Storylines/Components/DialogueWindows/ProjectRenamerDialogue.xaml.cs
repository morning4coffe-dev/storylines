using Storylines.Pages;
using Storylines.Scripts.Variables;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class ProjectRenamerDialogue : ContentDialog
    {
        public static ProjectRenamerDialogue projectRenamer;

        public ProjectRenamerDialogue()
        {
            this.InitializeComponent();
            projectRenamer = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = projectRenamer;
            projectRenamer.RequestedTheme = AppView.current.RequestedTheme;
        }

        public static async System.Threading.Tasks.Task Open()
        {
            await new ProjectRenamerDialogue().ShowAsync();
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if (string.IsNullOrEmpty(SaveSystem.currentProject.projectName))
                titleText.Text = ResourceLoader.GetForCurrentView().GetString("chapterDialogueCreate");
            else
                titleText.Text = ResourceLoader.GetForCurrentView().GetString("chapterDialogueRename");

            chapterNameBox.Text = SaveSystem.currentProject.projectName;
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSystem.currentProject.projectName = chapterNameBox.Text;
            AppView.current.UpdateTitleBar();

            projectRenamer.Hide();
            //_ = MainPage.chapterText.textBox.Focus(FocusState.Keyboard);
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            projectRenamer.Hide();
        }

        private void ContentDialog_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && submitButton.IsEnabled)
                OnSubmitButton_Click(sender, new RoutedEventArgs());
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            AppView.currentlyOpenedDialogue = null;
        }

        bool isHide = true;
        private void InitializeClickOutToClose()
        {
            Window.Current.CoreWindow.PointerPressed += (s, e) =>
            {
                if (isHide)
                    Hide();
            };

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }
    }
}
