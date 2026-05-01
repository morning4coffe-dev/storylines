using Storylines.Models;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Storylines.Views.Pages
{
    public sealed partial class BranchingDialoguePage : Page
    {
        private readonly IBranchingDialogueService _service;
        public BranchingDialogueViewModel ViewModel { get; }

        public bool unappliedChanges { get; set; }
        public static BranchingDialoguePage current { get; private set; }

        public BranchingDialoguePage()
        {
            InitializeComponent();

            _service = App.GetService<IBranchingDialogueService>();
            ViewModel = App.GetService<BranchingDialogueViewModel>();
            DataContext = ViewModel;

            current = this;
            AppView.current.page = AppView.Pages.BranchingDialogue;

            canvasControl.ViewModel = ViewModel;
            canvasControl.NodeSelected += OnCanvasNodeSelected;
            canvasControl.NodeDoubleTapped += OnCanvasNodeDoubleTapped;
            canvasControl.CanvasBackgroundTapped += OnCanvasBackgroundTapped;
            canvasControl.CanvasDoubleTapped += OnCanvasDoubleTapped;
            canvasControl.ConnectionRequested += OnCanvasConnectionRequested;

            simulatorControl.ViewModel = ViewModel;

            ViewModel.GraphRefreshed += OnGraphRefreshed;
            ViewModel.PropertyChanged += OnViewModel_PropertyChanged;

            BuildExportMenuItems();
            canvasControl.RedrawCanvas();
            UpdateTagsTextBox();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string chapterToken && !string.IsNullOrEmpty(chapterToken))
                ViewModel.NavigatedTo(chapterToken);
        }

        #region Canvas Control Events

        private void OnCanvasNodeSelected(BranchingDialogueNodeData node)
        {
            ViewModel.SelectedNode = node;
        }

        private void OnCanvasNodeDoubleTapped(BranchingDialogueNodeData node)
        {
            ViewModel.SelectedNode = node;

            // Ensure side panel is visible
            if (!ViewModel.IsNodeListVisible)
            {
                ViewModel.IsNodeListVisible = true;
                sidePanelColumn.Width = new GridLength(340);
                sidePanel.Visibility = Visibility.Visible;
            }
        }

        private void OnCanvasDoubleTapped(double x, double y)
        {
            ViewModel.CreateNodeAtPosition(x, y);
        }

        private void OnCanvasBackgroundTapped()
        {
            ViewModel.SelectedNode = null;
        }

        private void OnCanvasConnectionRequested(BranchingDialogueNodeData source, BranchingDialogueNodeData target)
        {
            var chapterId = ViewModel.SelectedChapter?.Token;
            if (string.IsNullOrWhiteSpace(chapterId))
                return;

            _service.AddChoice(chapterId, source.Id, null, target.Id);
            _service.NotifyGraphChanged(chapterId);
            ViewModel.RefreshFilteredNodes();
            ViewModel.ValidateGraphCommand.Execute(null);
        }

        private void OnGraphRefreshed()
        {
            canvasControl.RedrawCanvas();
        }

        private void BuildExportMenuItems()
        {
            if (exportImportFlyout == null)
                return;

            while (exportImportFlyout.Items.Count > 0 && exportImportFlyout.Items[0] is MenuFlyoutItem)
                exportImportFlyout.Items.RemoveAt(0);

            var insertIndex = 0;
            foreach (var format in ViewModel.BranchingExportFormats)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = format.MenuText,
                };
                menuItem.Click += async (sender, args) => await ViewModel.ExportGraphAsync(format.Definition.Id);
                exportImportFlyout.Items.Insert(insertIndex++, menuItem);
            }
        }

        #endregion

        #region Side Panel Events

        private void OnToggleNodeList_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleNodeListCommand.Execute(null);
            sidePanelColumn.Width = ViewModel.IsNodeListVisible
                ? new GridLength(340)
                : new GridLength(0);
            sidePanel.Visibility = ViewModel.IsNodeListVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void OnNodesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.SelectedNode != null)
            {
                canvasControl.ScrollToNode(ViewModel.SelectedNode);
                UpdateTagsTextBox();
            }
        }

        private void OnValidationIssue_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.Tag is BranchingDialogueValidationIssueItemViewModel issueItem))
                return;

            if (!ViewModel.SelectValidationIssue(issueItem))
                return;

            if (!ViewModel.IsNodeListVisible)
            {
                ViewModel.IsNodeListVisible = true;
                sidePanelColumn.Width = new GridLength(340);
                sidePanel.Visibility = Visibility.Visible;
            }

            if (ViewModel.SelectedNode != null)
            {
                nodesList?.ScrollIntoView(ViewModel.SelectedNode);
                canvasControl.ScrollToNode(ViewModel.SelectedNode);
                UpdateTagsTextBox();
            }
        }

        private void OnChoiceTargetComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
                comboBox.ItemsSource = ViewModel.AllNodeTargets;
        }

        private void OnRemoveChoice_Click(object sender, RoutedEventArgs e)
        {
            ExecuteChoiceCommand(sender, ViewModel.RemoveChoiceCommand);
        }

        private void OnSpeakerSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text?.Trim() ?? string.Empty;
                var filtered = ViewModel.CharacterSuggestions
                    .Where(s => s.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    .ToList();
                sender.ItemsSource = filtered;
            }
        }

        private void OnSpeakerSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (ViewModel.SelectedNode != null)
            {
                ViewModel.SelectedNode.Speaker = args.QueryText ?? args.ChosenSuggestion?.ToString();
            }
        }

        #endregion

        #region Tags

        private void UpdateTagsTextBox()
        {
            if (ViewModel.SelectedNode?.Tags != null)
                tagsTextBox.Text = string.Join(", ", ViewModel.SelectedNode.Tags);
            else
                tagsTextBox.Text = string.Empty;
        }

        private void OnTagsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedNode == null)
                return;

            var text = tagsTextBox.Text ?? string.Empty;
            ViewModel.SelectedNode.Tags = text
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }

        private void OnViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedNode))
            {
                UpdateTagsTextBox();
                canvasControl.RedrawCanvas();
            }
        }

        #endregion

        #region Actions

        private void OnRemoveAction_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element) || !(element.DataContext is BranchingDialogueActionData action))
                return;

            var node = ViewModel.SelectedNode;
            if (node?.Actions == null)
                return;

            var index = node.Actions.IndexOf(action);
            if (index >= 0 && ViewModel.RemoveActionCommand.CanExecute(index))
                ViewModel.RemoveActionCommand.Execute(index);
        }

        #endregion

        #region Conditions

        private void OnToggleConditions_Click(object sender, RoutedEventArgs e)
        {
            // Walk up to find the parent StackPanel that contains the conditionsPanel
            if (!(sender is FrameworkElement element))
                return;

            // The button is inside Grid > StackPanel > StackPanel(root of DataTemplate)
            // The conditionsPanel is a sibling StackPanel in the root StackPanel
            var parent = element.Parent as FrameworkElement;
            while (parent != null && !(parent is StackPanel sp && sp.FindName("conditionsPanel") != null))
            {
                parent = parent.Parent as FrameworkElement;
            }

            if (parent is StackPanel rootPanel)
            {
                // Find the conditionsPanel child
                foreach (var child in rootPanel.Children)
                {
                    if (child is StackPanel panel && panel.Name == "conditionsPanel")
                    {
                        panel.Visibility = panel.Visibility == Visibility.Visible
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                        break;
                    }
                }
            }
        }

        private void OnConditionOperatorComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is ComboBox comboBox))
                return;

            var names = Enum.GetNames(typeof(ConditionOperator));
            comboBox.ItemsSource = names;

            if (comboBox.DataContext is BranchingDialogueConditionData condition)
                comboBox.SelectedItem = condition.Operator.ToString();
        }

        private void OnConditionOperatorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ComboBox comboBox) || !(comboBox.DataContext is BranchingDialogueConditionData condition))
                return;

            if (comboBox.SelectedItem is string selected && Enum.TryParse<ConditionOperator>(selected, out var op))
                condition.Operator = op;
        }

        private void OnAddCondition_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element) || !(element.DataContext is BranchingDialogueChoiceData choice))
                return;

            if (choice.Conditions == null)
                choice.Conditions = new List<BranchingDialogueConditionData>();

            choice.Conditions.Add(new BranchingDialogueConditionData());

            // Trigger redraw of the choices list
            if (ViewModel.SelectedNode != null && ViewModel.SelectedChapter != null)
                _service.NotifyGraphChanged(ViewModel.SelectedChapter.Token);
        }

        private void OnRemoveCondition_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element) || !(element.DataContext is BranchingDialogueConditionData condition))
                return;

            // Walk up to find the choice that owns this condition
            var parent = element.Parent as FrameworkElement;
            while (parent != null)
            {
                if (parent.DataContext is BranchingDialogueChoiceData choice && choice.Conditions != null)
                {
                    choice.Conditions.Remove(condition);

                    if (ViewModel.SelectedChapter != null)
                        _service.NotifyGraphChanged(ViewModel.SelectedChapter.Token);
                    break;
                }
                parent = parent.Parent as FrameworkElement;
            }
        }

        #endregion

        private void OnPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                if (canvasControl.CancelConnectionMode())
                {
                    e.Handled = true;
                    return;
                }

                if (ViewModel.SelectedNode != null)
                {
                    ViewModel.SelectedNode = null;
                    e.Handled = true;
                }

                return;
            }

            if (e.Key == VirtualKey.Delete && ViewModel.SelectedNode != null)
            {
                var focused = FocusManager.GetFocusedElement();
                if (focused is TextBox || focused is AutoSuggestBox)
                    return;

                ViewModel.DeleteSelectedNodeCommand.Execute(null);
                e.Handled = true;
            }
        }

        private static void ExecuteChoiceCommand(object sender, ICommand command)
        {
            if (!(sender is FrameworkElement element) || !(element.DataContext is BranchingDialogueChoiceData choice))
                return;

            if (command?.CanExecute(choice) == true)
                command.Execute(choice);
        }
    }
}
