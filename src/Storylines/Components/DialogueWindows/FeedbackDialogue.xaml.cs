using Windows.UI.Xaml.Controls;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class FeedbackDialogue : ContentDialog
    {
        //public static FeedbackDialogue feedbackDialogue;

        //private enum FeedbackType { None, Suggestion, Problem };
        //private static FeedbackType feedbackType;

        public FeedbackDialogue()
        {
            //InitializeComponent();
            //feedbackDialogue = this;

            //feedbackDialogue.RequestedTheme = AppView.current.RequestedTheme;

            //AppView.currentlyOpenedDialogue = feedbackDialogue;

            //SomethingChanged();
        }

        //public static void Open()
        //{
        //    _ = new FeedbackDialogue().ShowAsync();
        //}


        //public void SomethingChanged()
        //{

        //}



        //private void OnExportButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    //Scripts.Functions.MicrosoftStoreAndAppCenterFunctions.SendAnalyticData_Feedback(feedbackType.ToString(), feedbackText.Text, feedbackLongText.Text, (bool)analyticsCheckBox.IsChecked);
        //    Hide();
        //}

        //private void OnSuggestionButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    chooseExportChaptersPanel.Visibility = (bool)chooseExportChaptersButton.IsChecked ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
        //    if ((bool)chooseExportChaptersButton.IsChecked)
        //    {
        //        analyticsCheckBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

        //        feedbackType = FeedbackType.Suggestion;

        //        chooseExportDialoguesButton.IsChecked = false;
        //    }
        //}

        //private void OnProblemButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    chooseExportChaptersPanel.Visibility = (bool)chooseExportDialoguesButton.IsChecked ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
        //    analyticsCheckBox.Visibility = Windows.UI.Xaml.Visibility.Visible;

        //    feedbackType = FeedbackType.Problem;

        //    chooseExportChaptersButton.IsChecked = false; 
        //}

        //private void OnCancelButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    Hide();
        //}

        //private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        //{
        //    AppView.currentlyOpenedDialogue = null;
        //}

        //private void OnNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    SomethingChanged();
        //}
    }
}
