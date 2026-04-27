using Storylines.Models;
using Storylines.ViewModels;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Views.Controls
{
    public sealed partial class DialogueSimulatorControl : UserControl
    {
        public BranchingDialogueViewModel ViewModel { get; set; }

        public DialogueSimulatorControl()
        {
            InitializeComponent();
        }

        public void UpdateUi()
        {
            if (ViewModel == null)
                return;

            breadcrumbText.Text = ViewModel.SimulatorBreadcrumb ?? string.Empty;
            currentSpeakerText.Text = ViewModel.SimulatorCurrentSpeaker ?? string.Empty;
            currentText.Text = ViewModel.SimulatorCurrentText ?? string.Empty;
            statusText.Text = ViewModel.SimulatorStatus ?? string.Empty;
            choicesList.ItemsSource = ViewModel.SimulationChoices;
            variablesList.ItemsSource = ViewModel.SimulationVariables;
        }

        private void OnStart_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.StartSimulationCommand.Execute(null);
            UpdateUi();
        }

        private void OnRestart_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.RestartSimulationCommand.Execute(null);
            UpdateUi();
        }

        private void OnStop_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.StopSimulationCommand.Execute(null);
            UpdateUi();
        }

        private void OnChoice_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element) || !(element.DataContext is BranchingDialogueChoiceData choice))
                return;

            if (ViewModel?.ChooseSimulationChoiceCommand?.CanExecute(choice) == true)
            {
                ViewModel.ChooseSimulationChoiceCommand.Execute(choice);
                UpdateUi();
            }
        }
    }
}
