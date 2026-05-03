using Storylines.Views.Pages;
using Storylines.Models;
using System;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Helpers;

namespace Storylines.Views.Dialogs
{
    public sealed partial class ProjectRenamerDialogue : ContentDialog
    {
        private static IProjectPersistenceService Persistence => App.GetService<IProjectPersistenceService>();

        public static ProjectRenamerDialogue projectRenamer;

        public ProjectRenamerDialogue()
        {
            this.InitializeComponent();
            DialogHelper.EnsureXamlRoot(this);
            projectRenamer = this;

            InitializeClickOutToClose();

            AppView.currentlyOpenedDialogue = projectRenamer;
            projectRenamer.RequestedTheme = AppView.current.ActualTheme;
        }

        public static async System.Threading.Tasks.Task Open()
        {
            await new ProjectRenamerDialogue().ShowAsync();
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if (string.IsNullOrEmpty(Persistence.CurrentProject?.projectName))
                titleText.Text = ResourceLoader.GetForViewIndependentUse().GetString("chapterDialogueCreate");
            else
                titleText.Text = ResourceLoader.GetForViewIndependentUse().GetString("chapterDialogueRename");

            chapterNameBox.Text = Persistence.CurrentProject?.projectName;
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (Persistence.CurrentProject != null)
                Persistence.CurrentProject.projectName = chapterNameBox.Text;
            App.GetService<EventAggregator>().Publish(new TitleBarUpdateEvent());

            projectRenamer.Hide();
            //_ = MainPage.ChapterText.textBox.Focus(FocusState.Keyboard);
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
            App.MainWindow.Content.PointerPressed -= OnWindowPointerPressed;
            AppView.currentlyOpenedDialogue = null;

            if (ReferenceEquals(projectRenamer, this))
                projectRenamer = null;
        }

        bool isHide = true;
        private void InitializeClickOutToClose()
        {
            App.MainWindow.Content.PointerPressed += OnWindowPointerPressed;

            PointerExited += (s, e) => isHide = true;
            PointerEntered += (s, e) => isHide = false;
        }

        private void OnWindowPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (isHide)
                Hide();
        }
    }
}
