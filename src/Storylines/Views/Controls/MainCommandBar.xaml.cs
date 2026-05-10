using Storylines.Views.Pages;
using Storylines.Helpers;
using Storylines.Services;
using Storylines.Models;
using Storylines.Services.Interfaces;
using Storylines.ViewModels;
using System;
using Windows.Storage;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Storylines.Views.Dialogs;
using Storylines.Services.Modes;
using System.ComponentModel;

namespace Storylines.Views.Controls
{
    public sealed partial class MainCommandBar : UserControl
    {
        private readonly ChaptersListViewModel _chaptersListViewModel;
        private readonly IChapterWorkflowService _chapterWorkflow;
        private readonly INavigationService _navigation;
        private readonly ProjectState _projectState;
        private readonly ITextEditorService _textEditor;
        private readonly CommandBarViewModel _viewModel;
        private readonly EditorModeService _modeService;
        private readonly SpeechHubViewModel _speechHub;
        private readonly WindowContext _windowContext;

        public CommandBarViewModel ViewModel => _viewModel;
        public SpeechHubViewModel SpeechHub => _speechHub;

        private MainPage CurrentMainPage => _windowContext?.MainPage;

        private Storylines.Views.Controls.DialogueEditor.BranchingDialogueEditor CurrentChapterText => _windowContext?.ChapterText;

        public MainCommandBar()
        {
            _windowContext = App.GetService<WindowContext>();
            _chaptersListViewModel = App.GetService<ChaptersListViewModel>();
            _chapterWorkflow = App.GetService<IChapterWorkflowService>();
            _navigation = App.GetService<INavigationService>();
            _projectState = App.GetService<ProjectState>();
            _textEditor = App.GetService<ITextEditorService>();
            _viewModel = App.GetService<CommandBarViewModel>();
            _modeService = App.TryGetService<EditorModeService>();
            _speechHub = App.GetService<SpeechHubViewModel>();

            this.InitializeComponent();

            if(App.TryGetService<Storylines.Services.Modes.EditorModeService>()?.Current.Id == "edit"
               || App.TryGetService<Storylines.Services.Modes.EditorModeService>() is null)
            {
                _windowContext.CommandBar = this;
            }

            UpdateExperimentalFeaturesVisibility();
            var events = App.GetService<EventAggregator>();
            events.Subscribe<SettingChangedEvent>(OnSettingChanged);
            events.Subscribe<TextFormattingStateChangedEvent>(OnTextFormattingStateChanged);

            if (_modeService is not null)
            {
                _modeService.ModeChanged += UpdateModeButtonStates;
                UpdateModeButtonStates(_modeService.Current);
            }

            Unloaded += OnUnloaded;
            _speechHub.PropertyChanged += OnSpeechHubPropertyChanged;

            // Restore persisted dialogue mode state
            dialoguesEnableButton.IsChecked = SettingsValues.dialogueModeEnabled;
            RefreshSpeechCommandAvailability();
        }

        private void OnSettingChanged(SettingChangedEvent e)
        {
            if (e.SettingKey == SettingsValueStrings.ExperimentalFeaturesEnabled)
                UpdateExperimentalFeaturesVisibility();
        }

        private void OnTextFormattingStateChanged(TextFormattingStateChangedEvent e)
        {
            mainBoldButton.IsChecked = e.IsBold;
            mainItalicButton.IsChecked = e.IsItalic;
            mainUnderlineButton.IsChecked = e.IsUnderlined;
            mainStrikethroughButton.IsChecked = e.IsStrikethrough;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_modeService is not null)
                _modeService.ModeChanged -= UpdateModeButtonStates;

            _speechHub.PropertyChanged -= OnSpeechHubPropertyChanged;
        }

        private void OnSpeechHubPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == nameof(SpeechHubViewModel.IsDictating)
                || e.PropertyName == nameof(SpeechHubViewModel.ReadAloudState)
                || e.PropertyName == nameof(SpeechHubViewModel.CanShowReadAloudControls))
            {
                RefreshSpeechCommandAvailability();
            }
        }

        private void UpdateModeButtonStates(IEditorMode mode)
        {
            if (readOnlyModeButton is not null)
                readOnlyModeButton.IsChecked = mode?.Id == "readonly";
        }

        private void UpdateExperimentalFeaturesVisibility()
        {
            bool showBranching = false;

#if PRIVATE_PLUGINS
            try
            {
                showBranching = SettingsValues.experimentalFeaturesEnabled
                                && App.TryGetService<Storylines.Services.Interfaces.IBranchingDialogueService>() is not null;
            }
            catch
            {
                showBranching = false;
            }
#else
            showBranching = false;
#endif

            // Ensure the button exists in XAML and set its visibility
            if (branchingDialogueButton is not null)
                branchingDialogueButton.Visibility = showBranching ? Visibility.Visible : Visibility.Collapsed;
        }

        #region TEMP - NavigationView
        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (int.TryParse((sender.SelectedItem as Microsoft.UI.Xaml.Controls.NavigationViewItem).Tag.ToString(), out int i))
            {
                commandBarFile.Visibility = Visibility.Collapsed;
                commandBarInsert.Visibility = Visibility.Collapsed;
                commandBarView.Visibility = Visibility.Collapsed;
                commandBarHelp.Visibility = Visibility.Collapsed;

                switch (i)
                {
                    case 0:
                        commandBarFile.Visibility = Visibility.Visible;
                        break;
                    case 1:
                        commandBarInsert.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        commandBarView.Visibility = Visibility.Visible;
                        break;
                    case 3:
                        commandBarHelp.Visibility = Visibility.Visible;
                        break;
                }
            }
            else
                _windowContext.AppView.ChangePage(AppView.Pages.Settings);
        }
        #endregion

        #region FILE
        private void OnAutosaveToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleAutosaveCommand.Execute(null);
        }
        #endregion

        #region INSERT
        private void OnChapterAddButton_Click(object sender, RoutedEventArgs e)
        {
            if(_chaptersListViewModel.CanAdd)
                _chapterWorkflow.OpenCreateChapterDialog();
        }

        private void OnDialoguesEnableButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentChapterText?.DialoguesOnOff((bool)dialoguesEnableButton.IsChecked);
        }

        private void OnDialoguesAddButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentChapterText?.AddDialogue();
        }

        private void OnBranchingDialogueButton_Click(object sender, RoutedEventArgs e)
        {
    #if PRIVATE_PLUGINS
            _navigation?.NavigateTo(Storylines.Services.Interfaces.NavigationTarget.BranchingDialogue);
    #endif
        }
        #endregion

        #region FORMAT
        private void OnFormatterButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentChapterText is null)
                return;

            switch ((sender as Control).Tag?.ToString())
            {
                case "Bold":
                    CurrentChapterText.BoldChapterTextBox();
                    break;
                case "Italic":
                    CurrentChapterText.ItalicChapterTextBox();
                    break;
                case "Underline":
                    CurrentChapterText.UnderlineChapterTextBox();
                    break;
                case "Strikethrough":
                    CurrentChapterText.StrikethroughChapterTextBox();
                    break;
                case "Highlighter":
                    CurrentChapterText.MarkTextBackground();
                    break;
            }
        }

        private void OnMainHighlighterButton_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (!mainHighlighterFlyout.IsOpen)
                mainHighlighterFlyout.ShowAt(mainHighlighterButton);
            else
                mainHighlighterFlyout.Hide();
        }

        private void OnHighlighterColorButton_Click(object sender, RoutedEventArgs e)
        {
            _windowContext.Highlighter.SelectedTool = (TextHighlighter.Tool)Enum.Parse(typeof(TextHighlighter.Tool), (sender as Button).Tag.ToString());
            CurrentChapterText?.MarkTextBackground();
            mainHighlighterFlyout.Hide();
        }

        public void SetFormattingCommandsEnabled(bool enabled)
        {
            mainBoldButton.IsEnabled = enabled;
            mainItalicButton.IsEnabled = enabled;
            mainUnderlineButton.IsEnabled = enabled;
            mainStrikethroughButton.IsEnabled = enabled;
            mainHighlighterButton.IsEnabled = enabled;
        }

        public void ClearFormattingCommandState()
        {
            mainBoldButton.IsChecked = false;
            mainItalicButton.IsChecked = false;
            mainUnderlineButton.IsChecked = false;
            mainStrikethroughButton.IsChecked = false;
        }

        public bool IsFormattingContextElement(DependencyObject element)
        {
            if (element is null)
                return false;

            return IsChildOf(element, mainBoldButton)
                || IsChildOf(element, mainItalicButton)
                || IsChildOf(element, mainUnderlineButton)
                || IsChildOf(element, mainStrikethroughButton)
                || IsChildOf(element, mainHighlighterButton)
                || IsChildOf(element, typewriterModeButton)
                || IsChildOf(element, readAloudButton)
                || IsChildOf(element, dictationButton)
                || IsChildOf(element, mainHighlighterFlyout.Content as DependencyObject);
        }

        public void RefreshSpeechCommandAvailability()
        {
            if (readAloudButton is not null)
                readAloudButton.IsEnabled = _speechHub.CanShowReadAloudControls || HasReadableText();

            if (dictationButton is not null)
                dictationButton.IsEnabled = _speechHub.IsDictating || (CurrentMainPage?.IsEditorCommandContextActive ?? false);
        }

        private bool HasReadableText()
        {
            var selectedText = _textEditor.GetSelectedText();
            if (!string.IsNullOrWhiteSpace(selectedText))
                return true;

            return !string.IsNullOrWhiteSpace(_textEditor.GetText(TextFormat.PlainText));
        }

        private void OnFormattingSurface_GotFocus(object sender, RoutedEventArgs e)
            => CurrentMainPage?.SetTextFormattingContextActive(true);

        private void OnFormattingSurface_LostFocus(object sender, RoutedEventArgs e)
        {
            var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement() as DependencyObject;
            if (IsFormattingContextElement(focused)
                || (CurrentChapterText?.IsFormattingContextElement(focused) ?? false))
            {
                CurrentMainPage?.SetTextFormattingContextActive(true);
                return;
            }

            CurrentMainPage?.SetTextFormattingContextActive(false);
        }

        private static bool IsChildOf(DependencyObject child, DependencyObject parent)
        {
            var current = child;
            while (current is not null)
            {
                if (current == parent)
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }
        #endregion

        #region VIEW
        private void OnTypewriterModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentChapterText is not null)
                CurrentChapterText.IsTypewriterModeActive = typewriterModeButton.IsChecked == true;

            if (_textEditor.SelectedChapterIndex >= 0)
                _textEditor.Focus();
        }

        private void OnNotesToggleButton_Click(object sender, RoutedEventArgs e)
            => CurrentMainPage?.ToggleNotesPane(notesToggleButton.IsChecked == true);

        private void OnSearchReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (searchReplaceButton.IsChecked == true)
                CurrentChapterText?.OpenSearchAndReplace();
            else
                CurrentChapterText?.CloseSearchAndReplace();
        }

        private void OnPinboardButton_Click(object sender, RoutedEventArgs e)
            => _navigation.NavigateTo(Services.Interfaces.NavigationTarget.Pinboard);

        private void OnReadOnlyModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_modeService?.IsInMode("readonly") == true)
                _windowContext.AppView?.TryExitActiveMode();
            else
                ViewModel.OpenReadOnlyModeCommand.Execute(null);

            UpdateModeButtonStates(_modeService?.Current);
        }

        private void OnGlobalSearchButton_Click(object sender, RoutedEventArgs e)
            => _ = GlobalSearchDialogue.OpenAsync();

        private void OnWritingPromptsButton_Click(object sender, RoutedEventArgs e)
            => _ = WritingPromptsDialogue.OpenAsync();
        #endregion

        #region HELP
        #endregion
    }
}
