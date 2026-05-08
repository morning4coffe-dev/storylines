using Microsoft.UI;
using Storylines.Constants;
using Storylines.Helpers;
using Storylines.Views.Pages;
using Storylines.Services;
using Storylines.Services.Modes;
using Storylines.Models;
using Storylines.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Microsoft.UI.Text;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Storylines.Views.Controls
{
    public sealed partial class ChapterTextBox : UserControl
    {
        private readonly EventAggregator _events;
        private readonly ProjectState _projectState;
        private readonly ResourceLoader _resources;
        private readonly ITextEditorService _textEditor;
        private readonly IWritingSessionService _writingSession;
        private readonly WindowContext _windowContext;

        private readonly ObservableCollection<Character> _dialoguePopupCharacters = new ObservableCollection<Character>();
        private readonly ObservableCollection<string> _recentDialogueCharacterTokens = new ObservableCollection<string>();

        // True when the popup was triggered by the Enter key — newline was already inserted,
        // so InsertDialogue must not prepend another one.
        private bool _dialoguePopupEnteredViaKey = false;

        public bool dialoguesOn = false;

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(ChapterTextBox),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        public static readonly DependencyProperty FormattingBarVisibilityProperty =
            DependencyProperty.Register(
                nameof(FormattingBarVisibility),
                typeof(Visibility),
                typeof(ChapterTextBox),
                new PropertyMetadata(Visibility.Visible, OnFormattingBarVisibilityChanged));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public Visibility FormattingBarVisibility
        {
            get => (Visibility)GetValue(FormattingBarVisibilityProperty);
            set => SetValue(FormattingBarVisibilityProperty, value);
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ChapterTextBox)d).ApplyChromeState();
        }

        private static void OnFormattingBarVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ChapterTextBox)d).ApplyChromeState();
        }

        private void ApplyChromeState()
        {
            // Can fire before InitializeComponent wires up the named children.
            if (textBox is null || gridCommandBarHolder is null) return;

            bool readOnly = IsReadOnly;
            textBox.IsReadOnly = readOnly;
            gridCommandBarHolder.Visibility = !readOnly && FormattingBarVisibility == Visibility.Visible
                ? Visibility.Visible
                : Visibility.Collapsed;
            CurrentMainPage?.RefreshFormattingCommandAvailability();
        }

        private bool selectedTextIsBold = false;
        private bool selectedTextIsItalic = false;
        private bool selectedTextIsUnderlined = false;
        private bool selectedTextIsStriked = false;
        private bool searchingInTextBox = false;
        private int _chapterLocationCaptureSuppressionDepth;
        private string _loadedChapterToken;

        private bool _isTypewriterModeActive = false;
        private bool _isTypewriterCentering = false;

        public bool IsTypewriterModeActive
        {
            get => _isTypewriterModeActive;
            set
            {
                _isTypewriterModeActive = value;
                if (value && textBox?.Document?.Selection?.Length > 0)
                {
                    var selection = textBox.Document.Selection;
                    selection.SetRange(selection.EndPosition, selection.EndPosition);
                }

                if (value)
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, CenterCaretInViewport);
            }
        }

        private void CenterCaretInViewport()
        {
            if (!_isTypewriterModeActive || textBoxScrollViewer is null || textBox is null)
                return;

            textBox.Document.Selection.GetRect(PointOptions.ClientCoordinates, out Rect caretRect, out _);

            double targetOffset = textBoxScrollViewer.VerticalOffset + caretRect.Y - textBoxScrollViewer.ViewportHeight / 2;
            targetOffset = Math.Max(0, targetOffset);

            if (Math.Abs(targetOffset - textBoxScrollViewer.VerticalOffset) < 1.0)
                return;

            _isTypewriterCentering = true;
            textBoxScrollViewer.ChangeView(null, targetOffset, null, disableAnimation: true);
        }

        private MainPage CurrentMainPage => _windowContext?.MainPage;

        private MainCommandBar CurrentCommandBar => _windowContext?.CommandBar;

        private double CurrentShellWidth
        {
            get
            {
                var shellWidth = _windowContext?.AppView?.ActualWidth ?? 0;
                if (shellWidth > 0)
                    return shellWidth;

                return ActualWidth > 0 ? ActualWidth : LayoutConstants.DefaultWindowWidth;
            }
        }

        private bool IsCompactSearchLayout => CurrentShellWidth < LayoutConstants.SearchBarBreakpoint;

        public ChapterTextBox()
        {
            InitializeComponent();

            _windowContext = App.GetService<WindowContext>();
            _events = App.GetService<EventAggregator>();
            _projectState = App.GetService<ProjectState>();
            _resources = ResourceLoader.GetForViewIndependentUse();
            _textEditor = App.GetService<ITextEditorService>();
            _writingSession = App.GetService<IWritingSessionService>();

            dialoguePopupList.ItemsSource = _dialoguePopupCharacters;

            _windowContext.ChapterText = this;

            _events.Subscribe<SettingChangedEvent>(OnSettingChanged);
            _events.Subscribe<ChapterSelectedEvent>(OnChapterSelected);

            // Restore persisted dialogue mode
            dialoguesOn = SettingsValues.dialogueModeEnabled;

            UpdateReplaceAllButtonContent();
        }

        private void OnSettingChanged(SettingChangedEvent e)
        {
            if (e.SettingKey == SettingsValueStrings.TextBoxSolidBackground)
                TextBoxWhiteBackground((bool)e.Value);
            else if (e.SettingKey == SettingsValueStrings.EditorFontFamily)
            {
                var fontFamily = new Microsoft.UI.Xaml.Media.FontFamily((string)e.Value);
                textBox.FontFamily = fontFamily;
                // Apply to existing text in the document
                var range = textBox.Document.GetRange(0, Microsoft.UI.Text.TextConstants.MaxUnitCount);
                range.CharacterFormat.Name = (string)e.Value;
            }
            else if (e.SettingKey == SettingsValueStrings.EditorFontSize)
            {
                var size = (double)e.Value;
                textBox.FontSize = size;
                // Apply to existing text in the document
                var range = textBox.Document.GetRange(0, Microsoft.UI.Text.TextConstants.MaxUnitCount);
                range.CharacterFormat.Size = (float)size;
            }
        }

        private void OnChapterSelected(ChapterSelectedEvent e)
        {
            _loadedChapterToken = e.HasSelection
                && e.SelectedIndex >= 0
                && e.SelectedIndex < _projectState.Chapters.Count
                ? _projectState.Chapters[e.SelectedIndex].Token
                : null;

            if (!e.HasSelection)
            {
                _textEditor?.Clear();
                return;
            }

            ChangeTextColor();
            CheckForFormatting();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _windowContext.Highlighter.SelectedTool = TextHighlighter.Tool.Yellow;
            UpdateHighlighterButtonColor();

            // Apply saved font preferences
            textBox.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(SettingsValues.editorFontFamily);
            textBox.FontSize = SettingsValues.editorFontSize;

            ApplyChromeState();
            CurrentCommandBar?.RefreshSpeechCommandAvailability();
        }

        #region TextBox
        private void OnTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            // During undo/redo the action manages model state directly.
            // Skip to avoid RTF round-trip differences from creating
            // ghost entries or corrupting the snapshot chain.
            if (TimeTravelChapter.IsExecuting)
            {
                CurrentCommandBar?.RefreshSpeechCommandAvailability();
                return;
            }

            if (_textEditor?.IsProgrammaticChangeInProgress == true)
            {
                CurrentCommandBar?.RefreshSpeechCommandAvailability();
                return;
            }

            var chapter = GetLoadedChapter();
            if (chapter is not null)
            {
                textBox.Document.GetText(TextGetOptions.FormatRtf, out var txt);
                var oldText = chapter.Text;

                if (oldText != txt && !searchingInTextBox)
                {
                    chapter.Text = txt;

                    CurrentMainPage?.UpdateDownBar();
                    TimeTravelChapter.RecordTextChange(chapter.Token, oldText, txt);

                    App.TryGetService<EditorModeService>()?.Current.OnTextChanged();

                    // Start session timer on first edit; update word count goal bar
                    CurrentMainPage?.StartSessionTimer();
                    CurrentMainPage?.ViewModel.UpdateWordGoalBar();

                    // Record words for streak tracking
                    textBox.Document.GetText(TextGetOptions.None, out var plain);
                    int words = plain.Split(new char[] { ' ', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
                    _writingSession.RecordWords(words);

                    QueueChapterLocationCapture();
                }
            }

            CurrentCommandBar?.RefreshSpeechCommandAvailability();
        }

        private void OnTextBox_SelectionChanging(RichEditBox sender, RichEditBoxSelectionChangingEventArgs args)
        {
            if (GetLoadedChapter() is not null)
            {
                CurrentMainPage?.UpdateDownBar();

                CheckForFormatting();
                QueueChapterLocationCapture();
            }

            if (_isTypewriterModeActive)
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, CenterCaretInViewport);
        }

        private void OnTextBoxScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_isTypewriterCentering)
            {
                if (!e.IsIntermediate)
                    _isTypewriterCentering = false;
                return;
            }

            CacheCurrentChapterLocation();

            if (_isTypewriterModeActive && !(_textEditor?.IsProgrammaticChangeInProgress == true))
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, CenterCaretInViewport);
        }

        private void OnTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (ShouldBlockTypewriterDeletion(e))
            {
                e.Handled = true;
                return;
            }

            // When the dialogue popup is open, intercept navigation keys
            if (dialoguePopup.IsOpen)
            {
                switch (e.Key)
                {
                    case VirtualKey.Down:
                        NavigateDialoguePopup(1);
                        e.Handled = true;
                        return;
                    case VirtualKey.Up:
                        NavigateDialoguePopup(-1);
                        e.Handled = true;
                        return;
                    case VirtualKey.Tab:
                        ConfirmDialoguePopupSelection();
                        e.Handled = true;
                        return;
                    case VirtualKey.Escape:
                        dialoguePopup.IsOpen = false;
                        e.Handled = true;
                        return;
                    case VirtualKey.Enter:
                        if (dialoguePopupList.SelectedItem is Character)
                        {
                            ConfirmDialoguePopupSelection();
                            e.Handled = true;
                        }
                        else
                        {
                            dialoguePopup.IsOpen = false;
                        }
                        return;
                    default:
                        // Any other key dismisses the popup and lets the user keep typing
                        dialoguePopup.IsOpen = false;
                        return;
                }
            }

            // Show dialogue popup when Enter is pressed and dialogue mode is on
            if (_textEditor.SelectedChapterIndex >= 0 && dialoguesOn && e.Key == VirtualKey.Enter)
            {
                // Shift+Enter: plain newline only, skip dialogue mode
                var shiftDown = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                    & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
                if (shiftDown)
                    return;

                // Prevent the default newline so we control exactly one insertion
                e.Handled = true;
                textBox.Document.Selection.TypeText("\r");
                ShowDialoguePopup(enteredViaKey: true);
            }
        }

        private bool ShouldBlockTypewriterDeletion(KeyRoutedEventArgs e)
        {
            if (!_isTypewriterModeActive || textBox?.Document?.Selection is null)
                return false;

            var controlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            bool controlDown = (controlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

            if (e.Key == VirtualKey.Back || e.Key == VirtualKey.Delete)
                return true;

            if (controlDown && e.Key == VirtualKey.X)
                return true;

            return controlDown
                && e.Key == VirtualKey.V
                && textBox.Document.Selection.Length > 0;
        }

        public void ChangeTextColor()
        {
            ITextRange txtRange = textBox.Document.GetRange(0, TextConstants.MaxUnitCount);

            if (textBox.ActualTheme == ElementTheme.Dark)
                txtRange.CharacterFormat.ForegroundColor = Colors.White;
            else
            if (textBox.ActualTheme == ElementTheme.Light)
                txtRange.CharacterFormat.ForegroundColor = Colors.Black;
        }

        public void TextBoxWhiteBackground(bool whiteBackground)
        {
            if (whiteBackground)
            {
                textBox.RequestedTheme = ElementTheme.Light;
                textBoxScrollViewer.RequestedTheme = ElementTheme.Light;
                textBoxScrollViewer.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            }
            else
            {
                textBox.RequestedTheme = CurrentMainPage?.RequestedTheme ?? ElementTheme.Default;
                textBoxScrollViewer.RequestedTheme = CurrentMainPage?.RequestedTheme ?? ElementTheme.Default;
                textBoxScrollViewer.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"];
            }
        }

        #region CommandBarFlyout
        private void Menu_Opening(object sender, object e)
        {
            isFlyoutOpen = true;

            Microsoft.UI.Xaml.Controls.TextCommandBarFlyout myFlyout = sender as Microsoft.UI.Xaml.Controls.TextCommandBarFlyout;
            if (myFlyout.Target == textBox)
            {
                var font = new FontFamily("Segoe Fluent Icons") ?? new FontFamily("Segoe MDL2 Assets");
                AppBarToggleButton myButton = new AppBarToggleButton() { Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "" }, IsChecked = selectedTextIsStriked };

                myButton.Click += OnFormatterButton_Click;
                myButton.Tag = "Strikethrough";
                myButton.Command = new StandardUICommand();
                myFlyout.PrimaryCommands.Add(myButton);
            }
        }

        private void SelectMenu_Opening(object sender, object e)
        {
            // WinUI 3 desktop apps always use mouse interaction
            (sender as Microsoft.UI.Xaml.Controls.TextCommandBarFlyout).Hide();
        }

        private bool isFlyoutOpen = false;

        private void REBCustom_Loaded(object sender, RoutedEventArgs e)
        {
            textBox.SelectionFlyout.Opening += SelectMenu_Opening;
            textBox.ContextFlyout.Opening += Menu_Opening;
            textBox.ContextFlyout.Closing += Menu_Closing;
            textBox.SelectionFlyout.Closing += Menu_Closing;
        }

        private void Menu_Closing(Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase sender, Microsoft.UI.Xaml.Controls.Primitives.FlyoutBaseClosingEventArgs args)
        {
            isFlyoutOpen = false;
        }

        private void REBCustom_Unloaded(object sender, RoutedEventArgs e)
        {
            textBox.SelectionFlyout.Opening -= Menu_Opening;
            textBox.ContextFlyout.Opening -= Menu_Opening;
        }
        #endregion

        #region Search
        private void OnSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchingInTextBox = true;
            SearchBoxHighlightMatches(searchTextBox.Text);
        }

        private void SearchBoxHighlightMatches(string textToFind)
        {
            SearchBoxRemoveHighlights();

            if (string.IsNullOrEmpty(textToFind))
                return;

            Color highlightForegroundColor = ThemeSettings.GetCurrentAccentColor();

            ITextRange searchRange = textBox.Document.GetRange(0, 0);
            while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, FindOptions.None) > 0)
                searchRange.CharacterFormat.ForegroundColor = highlightForegroundColor;
        }

        /// <summary>
        /// Removes search highlighting by restoring matched ranges to the default
        /// foreground color. Uses the document's saved RTF to identify which ranges
        /// were highlighted, so it does NOT wipe intentional formatting.
        /// </summary>
        private void SearchBoxRemoveHighlights()
        {
            Color accentColor = ThemeSettings.GetCurrentAccentColor();
            SolidColorBrush defaultBrush = textBox.Foreground as SolidColorBrush;
            if (defaultBrush is null) return;
            Color defaultColor = defaultBrush.Color;

            // Walk through the document and only reset ranges whose foreground
            // matches the accent highlight color (i.e. ranges WE colored).
            ITextRange range = textBox.Document.GetRange(0, 0);
            while (range.MoveStart(TextRangeUnit.Character, 1) > 0)
            {
                range.MoveEnd(TextRangeUnit.Character, 0); // collapse to start
                range.Expand(TextRangeUnit.Character);

                if (range.CharacterFormat.ForegroundColor == accentColor)
                    range.CharacterFormat.ForegroundColor = defaultColor;
            }
        }

        private void OnSearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            searchingInTextBox = true;
        }

        private void OnSearchTextBox_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if ((sender as TextBox).Name != "searchTextBox")
                HideSearch();
        }

        private void OnSearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            HideSearch();
        }

        private void HideSearch()
        {
            searchingInTextBox = false;
            SearchBoxRemoveHighlights();

            searchButton.Visibility = Visibility.Visible;
            searchTextBox.Visibility = Visibility.Collapsed;

            chapterTextCommandBar.Visibility = Visibility.Visible;
        }

        public void EnableSeach()
        {
            searchingInTextBox = true;

            if (textBox.Document.Selection.Length > 0)
                searchTextBox.Text = textBox.Document.Selection.Text;

            SearchBoxHighlightMatches(searchTextBox.Text);

            if (IsCompactSearchLayout)
                chapterTextCommandBar.Visibility = Visibility.Collapsed;

            searchTextBox.Width = IsCompactSearchLayout ? ActualWidth - 15 : 320;

            if (searchTextBox.Visibility == Visibility.Collapsed)
            {
                searchButton.Visibility = Visibility.Collapsed;
                searchTextBox.Visibility = Visibility.Visible;
            }

            searchTextBox.Focus(FocusState.Keyboard);
        }

        private void OnSearchButton_Click(object sender, RoutedEventArgs e)
        {
            EnableSeach();
        }

        // --- Search & Replace ---
        private int _currentMatchIndex = -1;
        private int _totalMatches = 0;

        public void OpenSearchAndReplace()
        {
            HideSearch();
            searchReplacePanel.Visibility = Visibility.Visible;
            searchingInTextBox = true;
            UpdateReplaceAllButtonContent();

            // Sync toggle button in MainCommandBar
            if (CurrentCommandBar is not null)
                CurrentCommandBar.searchReplaceButton.IsChecked = true;

            if (textBox.Document.Selection.Length > 0)
                searchReplaceFindBox.Text = textBox.Document.Selection.Text;

            searchReplaceFindBox.Focus(FocusState.Keyboard);
            SearchReplaceHighlight();
        }

        private void OnSearchReplaceFindBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchReplaceHighlight();
        }

        private void OnMatchCaseToggle_Click(object sender, RoutedEventArgs e)
        {
            SearchReplaceHighlight();
        }

        private void OnWholeWordToggle_Click(object sender, RoutedEventArgs e)
        {
            SearchReplaceHighlight();
        }

        private FindOptions GetFindOptions()
        {
            var findOptions = FindOptions.None;
            if (matchCaseToggle.IsChecked == true)
                findOptions |= FindOptions.Case;
            if (wholeWordToggle.IsChecked == true)
                findOptions |= FindOptions.Word;
            return findOptions;
        }

        private void SearchReplaceHighlight()
        {
            SearchBoxRemoveHighlights();

            var textToFind = searchReplaceFindBox.Text;
            if (string.IsNullOrEmpty(textToFind))
            {
                _totalMatches = 0;
                _currentMatchIndex = -1;
                searchMatchCount.Text = string.Empty;
                return;
            }

            var findOptions = GetFindOptions();
            Color highlightColor = ThemeSettings.GetCurrentAccentColor();
            int matchCount = 0;

            ITextRange searchRange = textBox.Document.GetRange(0, 0);
            while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, findOptions) > 0)
            {
                searchRange.CharacterFormat.ForegroundColor = highlightColor;
                matchCount++;
            }

            _totalMatches = matchCount;
            if (_currentMatchIndex >= matchCount)
                _currentMatchIndex = matchCount > 0 ? 0 : -1;

            searchMatchCount.Text = matchCount > 0
                ? $"{matchCount} match{(matchCount == 1 ? "" : "es")}"
                : "No matches";
        }

        // ─── Next / Previous match navigation ─────────────────────────

        private void OnNextMatch_Click(object sender, RoutedEventArgs e)
        {
            NavigateMatch(forward: true);
        }

        private void OnPrevMatch_Click(object sender, RoutedEventArgs e)
        {
            NavigateMatch(forward: false);
        }

        private void NavigateMatch(bool forward)
        {
            var textToFind = searchReplaceFindBox.Text;
            if (string.IsNullOrEmpty(textToFind) || _totalMatches == 0) return;

            var findOptions = GetFindOptions();
            var selection = textBox.Document.Selection;

            // Search forward from end of current selection, or backward from start
            int searchFrom = forward ? selection.EndPosition : selection.StartPosition;
            int length = forward ? TextConstants.MaxUnitCount : -TextConstants.MaxUnitCount;

            ITextRange range = textBox.Document.GetRange(searchFrom, searchFrom);
            if (range.FindText(textToFind, length, findOptions) > 0)
            {
                selection.SetRange(range.StartPosition, range.EndPosition);
                return;
            }

            // Wrap around: search from beginning (forward) or end (backward)
            int wrapFrom = forward ? 0 : TextConstants.MaxUnitCount;
            range = textBox.Document.GetRange(wrapFrom, wrapFrom);
            if (range.FindText(textToFind, length, findOptions) > 0)
            {
                selection.SetRange(range.StartPosition, range.EndPosition);
            }
        }

        // ─── Replace single ───────────────────────────────────────────

        private void OnReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            var textToFind = searchReplaceFindBox.Text;
            var replaceWith = searchReplaceBox.Text;
            if (string.IsNullOrEmpty(textToFind)) return;

            var findOptions = GetFindOptions();
            var selection = textBox.Document.Selection;

            // If the current selection already matches, replace it
            bool selectionMatchesSearch = false;
            if (selection.Length > 0)
            {
                string selectedText = selection.Text;
                bool caseSensitive = (findOptions & FindOptions.Case) != 0;
                selectionMatchesSearch = string.Equals(
                    selectedText, textToFind,
                    caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            }

            if (selectionMatchesSearch)
            {
                selection.SetText(TextSetOptions.None, replaceWith);
                // Advance to next match after replacement
                NavigateMatch(forward: true);
            }
            else
            {
                // Find the next match first; user can then click Replace again to confirm
                NavigateMatch(forward: true);
            }

            SearchReplaceHighlight();
        }

        // ─── Replace all (current chapter) ────────────────────────────

        private void OnReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (allChaptersScopeToggle.IsChecked == true)
            {
                ReplaceAllAcrossChapters();
                return;
            }

            var textToFind = searchReplaceFindBox.Text;
            var replaceWith = searchReplaceBox.Text;
            if (string.IsNullOrEmpty(textToFind)) return;
            // Guard against infinite loop: if the replacement contains the search term
            // the loop would never terminate.
            if (replaceWith is not null && ContainsWithOptions(replaceWith, textToFind, GetFindOptions()))
            {
                ReplaceAllSafe(textToFind, replaceWith, GetFindOptions());
                return;
            }

            var findOptions = GetFindOptions();
            int replacements = 0;
            ITextRange searchRange = textBox.Document.GetRange(0, 0);
            while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, findOptions) > 0)
            {
                searchRange.SetText(TextSetOptions.None, replaceWith);
                replacements++;
            }

            if (replacements > 0)
                TimeTravelSystem.SomethingChanged();
            searchMatchCount.Text = $"{replacements} replacement{(replacements == 1 ? "" : "s")} made";
            SearchReplaceHighlight();
        }

        /// <summary>
        /// Safe replace-all that collects all match positions first, then replaces
        /// in reverse order to avoid offset shifting and infinite loops.
        /// </summary>
        private void ReplaceAllSafe(string textToFind, string replaceWith, FindOptions findOptions)
        {
            var matches = new System.Collections.Generic.List<(int start, int end)>();
            ITextRange searchRange = textBox.Document.GetRange(0, 0);
            while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, findOptions) > 0)
            {
                matches.Add((searchRange.StartPosition, searchRange.EndPosition));
            }

            // Replace in reverse order so earlier offsets remain valid
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var range = textBox.Document.GetRange(matches[i].start, matches[i].end);
                range.SetText(TextSetOptions.None, replaceWith);
            }

            if (matches.Count > 0)
                TimeTravelSystem.SomethingChanged();
            searchMatchCount.Text = $"{matches.Count} replacement{(matches.Count == 1 ? "" : "s")} made";
            SearchReplaceHighlight();
        }

        private static bool ContainsWithOptions(string haystack, string needle, FindOptions options)
        {
            var comparison = (options & FindOptions.Case) != 0
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            return haystack.IndexOf(needle, comparison) >= 0;
        }

        // ─── Replace all across chapters ──────────────────────────────

        private void ReplaceAllAcrossChapters()
        {
            var textToFind = searchReplaceFindBox.Text;
            var replaceWith = searchReplaceBox.Text;
            if (string.IsNullOrEmpty(textToFind)) return;

            var findOptions = GetFindOptions();
            bool needsSafeReplace = replaceWith is not null && ContainsWithOptions(replaceWith, textToFind, findOptions);

            int replacements = 0;
            int selectedIndex = _textEditor.SelectedChapterIndex;

            foreach (var chapter in _projectState.Chapters)
            {
                if (string.IsNullOrEmpty(chapter.Text)) continue;

                var box = new RichEditBox();
                box.Document.SetText(TextSetOptions.FormatRtf, chapter.Text);

                if (needsSafeReplace)
                {
                    // Collect matches first, replace in reverse
                    var matches = new System.Collections.Generic.List<(int start, int end)>();
                    ITextRange range = box.Document.GetRange(0, 0);
                    while (range.FindText(textToFind, TextConstants.MaxUnitCount, findOptions) > 0)
                        matches.Add((range.StartPosition, range.EndPosition));

                    for (int i = matches.Count - 1; i >= 0; i--)
                    {
                        var r = box.Document.GetRange(matches[i].start, matches[i].end);
                        r.SetText(TextSetOptions.None, replaceWith);
                    }
                    replacements += matches.Count;
                }
                else
                {
                    ITextRange range = box.Document.GetRange(0, 0);
                    while (range.FindText(textToFind, TextConstants.MaxUnitCount, findOptions) > 0)
                    {
                        range.SetText(TextSetOptions.None, replaceWith);
                        replacements++;
                    }
                }

                box.Document.GetText(TextGetOptions.FormatRtf, out string newRtf);
                chapter.Text = newRtf;
            }

            // Reload the current chapter in the editor
            if (selectedIndex >= 0 && selectedIndex < _projectState.Chapters.Count)
            {
                searchingInTextBox = true;
                _textEditor?.LoadChapterContent(_projectState.Chapters[selectedIndex]);
            }

            if (replacements > 0)
            {
                TimeTravelSystem.SomethingChanged();
                searchMatchCount.Text = $"{replacements} replacement{(replacements == 1 ? "" : "s")} made across all chapters";
            }
            else
            {
                searchMatchCount.Text = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse().GetString("noMatchesFound");
            }

            SearchReplaceHighlight();
        }

        private void OnAllChaptersScopeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateReplaceAllButtonContent();
        }

        private void OnSearchReplaceClose_Click(object sender, RoutedEventArgs e)
        {
            CloseSearchAndReplace();
        }

        public void CloseSearchAndReplace()
        {
            searchingInTextBox = false;
            SearchBoxRemoveHighlights();
            searchReplacePanel.Visibility = Visibility.Collapsed;

            // Sync toggle button in MainCommandBar
            if (CurrentCommandBar is not null)
                CurrentCommandBar.searchReplaceButton.IsChecked = false;
        }

        private void UpdateReplaceAllButtonContent()
        {
            if (replaceAllButton is null)
                return;

            var replaceAllText = _resources.GetString("replaceAllButton.Content");
            if (allChaptersScopeToggle?.IsChecked == true)
            {
                var allChaptersText = _resources.GetString("allChaptersScopeToggle.Content");
                replaceAllButton.Content = $"{replaceAllText} · {allChaptersText}";
            }
            else
            {
                replaceAllButton.Content = replaceAllText;
            }
        }
        #endregion
        #endregion

        #region CommandBar
        #region Dialogue popup
        private void ShowDialoguePopup(bool enteredViaKey = false)
        {
            _dialoguePopupEnteredViaKey = enteredViaKey;

            if (_projectState.Characters.Count == 0)
            {
                NoCharactersYet();
                return;
            }

            RefreshDialoguePopupCharacters();

            // Get caret position and place popup near it
            textBox.Document.Selection.GetRect(PointOptions.ClientCoordinates, out Rect caretRect, out _);
            var transform = textBox.TransformToVisual(gridHolder);
            var point = transform.TransformPoint(new Point(caretRect.X, caretRect.Bottom));

            dialoguePopup.HorizontalOffset = Math.Max(0, point.X);
            dialoguePopup.VerticalOffset = point.Y + 4;
            dialoguePopup.IsOpen = true;

            // Pre-select the most recent character, or the first one
            if (_dialoguePopupCharacters.Count > 0)
                dialoguePopupList.SelectedIndex = 0;

            // Keep focus in the text editor so the user can keep typing
            textBox.Focus(FocusState.Keyboard);
        }

        private void RefreshDialoguePopupCharacters()
        {
            _dialoguePopupCharacters.Clear();

            // Show recent characters first
            foreach (var token in _recentDialogueCharacterTokens)
            {
                var character = _projectState.Characters.FirstOrDefault(c => c.Token == token);
                if (character is not null)
                    _dialoguePopupCharacters.Add(character);
            }

            // Then add remaining characters alphabetically
            var recentTokens = _recentDialogueCharacterTokens.ToHashSet();
            foreach (var character in _projectState.Characters
                .Where(c => !recentTokens.Contains(c.Token))
                .OrderBy(c => c.Name))
            {
                _dialoguePopupCharacters.Add(character);
            }

            dialoguePopupEmptyText.Visibility = _dialoguePopupCharacters.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void NavigateDialoguePopup(int direction)
        {
            if (_dialoguePopupCharacters.Count == 0) return;

            int currentIndex = dialoguePopupList.SelectedIndex;
            int newIndex = currentIndex + direction;

            if (newIndex < 0) newIndex = _dialoguePopupCharacters.Count - 1;
            else if (newIndex >= _dialoguePopupCharacters.Count) newIndex = 0;

            dialoguePopupList.SelectedIndex = newIndex;
            dialoguePopupList.ScrollIntoView(dialoguePopupList.SelectedItem);
        }

        private void ConfirmDialoguePopupSelection()
        {
            if (dialoguePopupList.SelectedItem is Character character)
                InsertDialogue(character);
            else
                dialoguePopup.IsOpen = false;
        }

        private void OnDialoguePopupCharacter_ItemClick(object sender, ItemClickEventArgs e)
        {
            InsertDialogue((Character)e.ClickedItem);
        }

        private void OnDialoguePopup_Closed(object sender, object e)
        {
            // Ensure the text editor keeps focus after popup closes
            if (_textEditor.SelectedChapterIndex >= 0)
                textBox.Focus(FocusState.Keyboard);
        }

        private void InsertDialogue(Character character)
        {
            _ = textBox.Focus(FocusState.Keyboard);

            string dialogueFullText;
            if (_dialoguePopupEnteredViaKey)
            {
                // Enter already inserted exactly one newline — just append the speaker prefix
                dialogueFullText = $"{character.Name}: ";
            }
            else
            {
                // Triggered from the toolbar button; use Dialogue.Create which adds a
                // leading newline when the cursor is not at the start of the document
                var hasTextBeforeCursor = textBox.Document.Selection.StartPosition > 0;
                dialogueFullText = Dialogue.Create(character, hasTextBeforeCursor);
            }

            textBox.Document.Selection.TypeText(dialogueFullText);

            RememberRecentCharacter(character);
            dialoguePopup.IsOpen = false;
        }

        private void RememberRecentCharacter(Character character)
        {
            if (character is null) return;

            if (_recentDialogueCharacterTokens.Contains(character.Token))
                _recentDialogueCharacterTokens.Remove(character.Token);

            _recentDialogueCharacterTokens.Insert(0, character.Token);

            while (_recentDialogueCharacterTokens.Count > 4)
                _recentDialogueCharacterTokens.RemoveAt(_recentDialogueCharacterTokens.Count - 1);
        }

        public void DialoguesOnOff(bool enabled)
        {
            if (CurrentCommandBar is not null)
                CurrentCommandBar.dialoguesEnableButton.IsChecked = enabled;

            dialoguesOn = enabled;

            // Persist the setting
            App.GetService<IPreferencesService>().Set(SettingsValueStrings.DialogueModeEnabled, enabled);

            // Show teaching tip on first activation
            if (enabled && !SettingsValues.dialogueTeachingTipShown)
            {
                if (CurrentCommandBar is not null)
                {
                    dialogueTeachingTip.Target = CurrentCommandBar.dialoguesEnableButton;
                    dialogueTeachingTip.IsOpen = true;
                    App.GetService<IPreferencesService>().Set(SettingsValueStrings.DialogueTeachingTipShown, true);
                }
            }
        }

        public void AddDialogue()
        {
            if (_projectState.Characters.Count > 0)
                ShowDialoguePopup();
            else
                NoCharactersYet();
        }

        private void NoCharactersYet()
        {
            _ = App.GetService<IDialogService>().ShowNoCharactersDialogueAsync();
        }
        #endregion

        #region Format
        public void CheckForFormatting()
        {
            if (!TryGetSelection(out var selection))
            {
                selectedTextIsBold = false;
                selectedTextIsItalic = false;
                selectedTextIsUnderlined = false;
                selectedTextIsStriked = false;

                boldTextButton.IsChecked = false;
                italicTextButton.IsChecked = false;
                underlineTextButton.IsChecked = false;
                strikethroughButton.IsChecked = false;
                PublishFormattingState();
                return;
            }

            var format = selection.CharacterFormat;

            selectedTextIsBold = format.Bold == FormatEffect.On;
            selectedTextIsItalic = format.Italic == FormatEffect.On;
            selectedTextIsUnderlined = format.Underline != UnderlineType.None;
            selectedTextIsStriked = format.Strikethrough == FormatEffect.On;

            boldTextButton.IsChecked = selectedTextIsBold;
            italicTextButton.IsChecked = selectedTextIsItalic;
            underlineTextButton.IsChecked = selectedTextIsUnderlined;
            strikethroughButton.IsChecked = selectedTextIsStriked;

            PublishFormattingState();
        }

        private void PublishFormattingState()
        {
            _events.Publish(new TextFormattingStateChangedEvent
            {
                IsBold = selectedTextIsBold,
                IsItalic = selectedTextIsItalic,
                IsUnderlined = selectedTextIsUnderlined,
                IsStrikethrough = selectedTextIsStriked,
            });
        }

        public void BoldChapterTextBox()
        {
            if (_textEditor.SelectedChapterIndex >= 0 && textBox.Document.Selection is not null)
            {
                bool isBold = textBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On;
                ApplyChromeState();
                CurrentMainPage?.RefreshFormattingCommandAvailability();
                selectedTextIsBold = !isBold;

                boldTextButton.IsChecked = selectedTextIsBold;
                PublishFormattingState();
            }
        }

        public void ItalicChapterTextBox()
        {
            if (_textEditor.SelectedChapterIndex >= 0 && textBox.Document.Selection is not null)
            {
                bool isItalic = textBox.Document.Selection.CharacterFormat.Italic == FormatEffect.On;
                textBox.Document.Selection.CharacterFormat.Italic = isItalic ? FormatEffect.Off : FormatEffect.On;
                selectedTextIsItalic = !isItalic;

                italicTextButton.IsChecked = selectedTextIsItalic;
                PublishFormattingState();
            }
        }

        public void UnderlineChapterTextBox()
        {
            if (_textEditor.SelectedChapterIndex >= 0 && textBox.Document.Selection is not null)
            {
                bool isUnderlined = textBox.Document.Selection.CharacterFormat.Underline != UnderlineType.None;
                textBox.Document.Selection.CharacterFormat.Underline = isUnderlined ? UnderlineType.None : UnderlineType.Thin;
                selectedTextIsUnderlined = !isUnderlined;

                underlineTextButton.IsChecked = selectedTextIsUnderlined;
                PublishFormattingState();
            }
        }

        public void StrikethroughChapterTextBox()
        {
            if (_textEditor.SelectedChapterIndex >= 0 && textBox.Document.Selection is not null)
            {
                bool isStriked = textBox.Document.Selection.CharacterFormat.Strikethrough == FormatEffect.On;
                textBox.Document.Selection.CharacterFormat.Strikethrough = isStriked ? FormatEffect.Off : FormatEffect.On;
                selectedTextIsStriked = !isStriked;

                strikethroughButton.IsChecked = selectedTextIsStriked;
                PublishFormattingState();
            }
        }

        public void MarkTextBackground()
        {
            UpdateHighlighterButtonColor();

            if (_windowContext.Highlighter.SelectedTool == TextHighlighter.Tool.None
                || IsReadOnly
                || _textEditor.SelectedChapterIndex < 0
                || !TryGetSelection(out var selection))
            {
                return;
            }

            selection.CharacterFormat.BackgroundColor = _windowContext.Highlighter.ChangeColor(_windowContext.Highlighter.SelectedTool);
        }

        private void UpdateHighlighterButtonColor()
        {
            if (_windowContext.Highlighter.SelectedTool == TextHighlighter.Tool.None)
                return;

            highlighterButtonColor.Background = new SolidColorBrush(_windowContext.Highlighter.ChangeColor(_windowContext.Highlighter.SelectedTool));
        }

        private bool TryGetSelection(out ITextSelection selection)
        {
            selection = null;

            if (textBox?.Document is null)
                return false;

            try
            {
                selection = textBox.Document.Selection;
                return selection is not null;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private void OnFormatterButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as Control).Tag)
            {
                case "Bold":
                    BoldChapterTextBox();
                    break;
                case "Italic":
                    ItalicChapterTextBox();
                    break;
                case "Underline":
                    UnderlineChapterTextBox();
                    break;
                case "Strikethrough":
                    StrikethroughChapterTextBox();
                    break;
                case "Highlighter":
                    MarkTextBackground();
                    break;
            }
        }

        private void OnHighlighterMoreButton_Click(object sender, RoutedEventArgs e)
        {
            OnHightighterButton_Holding(sender, new HoldingRoutedEventArgs());
        }

        private void OnHightighterButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (!highlighterButtonFlyout.IsOpen)
                highlighterButtonFlyout.ShowAt(highlighterButton);
            else
                highlighterButtonFlyout.Hide();
        }

        private void OnHightighterButton_RightClick(object sender, RightTappedRoutedEventArgs e)
        {
            OnHightighterButton_Holding(sender, new HoldingRoutedEventArgs());
        }

        private void OnHighlighterColorButton_Click(object sender, RoutedEventArgs e)
        {
            _windowContext.Highlighter.SelectedTool = (TextHighlighter.Tool)Enum.Parse(typeof(TextHighlighter.Tool), (sender as Button).Tag.ToString());

            MarkTextBackground();
            highlighterButtonFlyout.Hide();
        }

        private void OnTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CurrentMainPage?.SetTextFormattingContextActive(true);
            CheckForFormatting();
            CurrentCommandBar?.RefreshSpeechCommandAvailability();
        }

        private void OnTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isFlyoutOpen || dialoguePopup.IsOpen)
                return;

            // When focus moves to a formatting button in the command bar,
            // don't reset the selection or button states — the user is applying
            // formatting and the state needs to be preserved.
            var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement() as DependencyObject;
            if (focused is not null && (IsChildOf(focused, gridCommandBarHolder)
                || (CurrentCommandBar?.IsFormattingContextElement(focused) ?? false)))
            {
                CurrentCommandBar?.RefreshSpeechCommandAvailability();
                return;
            }

            CacheCurrentChapterLocation();

            CurrentMainPage?.SetTextFormattingContextActive(false);
            CurrentCommandBar?.RefreshSpeechCommandAvailability();

            SuppressChapterLocationCaptureForPendingUiCycle();
            textBox.Document.Selection.SetRange(0, 0);

            boldTextButton.IsChecked = false;
            italicTextButton.IsChecked = false;
            underlineTextButton.IsChecked = false;
            strikethroughButton.IsChecked = false;

            CurrentCommandBar?.ClearFormattingCommandState();
            CurrentCommandBar?.RefreshSpeechCommandAvailability();
        }

        public bool IsFormattingContextElement(DependencyObject element)
        {
            if (element is null)
                return false;

            return IsChildOf(element, textBox)
                || IsChildOf(element, gridCommandBarHolder)
                || IsChildOf(element, highlighterButtonFlyout.Content as DependencyObject);
        }

        /// <summary>
        /// Checks whether <paramref name="child"/> is a visual descendant of <paramref name="parent"/>.
        /// </summary>
        private static bool IsChildOf(DependencyObject child, DependencyObject parent)
        {
            var current = child;
            while (current is not null)
            {
                if (current == parent) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void QueueChapterLocationCapture()
        {
            if (IsChapterLocationCaptureSuppressed)
                return;

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, CacheCurrentChapterLocation);
        }

        private void CacheCurrentChapterLocation()
        {
            if (IsChapterLocationCaptureSuppressed)
                return;

            var chapter = GetLoadedChapter();
            if (chapter is null)
                return;

            var selection = textBox?.Document?.Selection;
            if (selection is null)
                return;

            chapter.LastCaretPosition = Math.Max(0, selection.StartPosition);
            chapter.LastVerticalOffset = Math.Max(0, textBoxScrollViewer?.VerticalOffset ?? 0);
        }

        private void SuppressChapterLocationCaptureForPendingUiCycle()
        {
            _chapterLocationCaptureSuppressionDepth++;
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (_chapterLocationCaptureSuppressionDepth > 0)
                    _chapterLocationCaptureSuppressionDepth--;
            });
        }

        private bool IsChapterLocationCaptureSuppressed
            => _chapterLocationCaptureSuppressionDepth > 0 || _textEditor?.IsProgrammaticChangeInProgress == true;

        private Chapter GetLoadedChapter()
        {
            if (string.IsNullOrWhiteSpace(_loadedChapterToken))
                return null;

            return _projectState.FindChapter(_loadedChapterToken);
        }
        #endregion 
        #endregion

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid.SetColumn(searchTextBox, IsCompactSearchLayout ? 0 : 1);
            chapterTextCommandBar.Visibility = searchTextBox.Visibility == Visibility.Visible && IsCompactSearchLayout ? Visibility.Collapsed : Visibility.Visible;
            searchTextBox.Width = searchTextBox.Visibility == Visibility.Visible && IsCompactSearchLayout ? ActualWidth - 15 : 320;

            highlighterButtonMoreColors.Visibility = ActualWidth < 465 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnTextBox_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);

            if (_isTypewriterModeActive && (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) != Windows.UI.Core.CoreVirtualKeyStates.Down)
            {
                e.Handled = true;
                return;
            }

            if ((ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
            {
                int localScrollValue = e.GetCurrentPoint((UIElement)sender).Properties.MouseWheelDelta / 24;

                int fallbackZoomValue = Convert.ToInt32(CurrentMainPage?.ViewModel?.ZoomLevel ?? LayoutConstants.ZoomDefault);
                int scrollValue = App.GetService<IPreferencesService>().Get<int>(SettingsValueStrings.ZoomValue, fallbackZoomValue);

                if (scrollValue + localScrollValue >= 13 && scrollValue + localScrollValue <= 100)
                {
                    scrollValue += localScrollValue;
                    CurrentMainPage?.SetZoomValue(scrollValue);
                }
            }
        }

        private void OnTextBoxRectangle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_projectState.Chapters.Count == 0)
            {
                _projectState.AddChapter(ProjectState.GetRandomChapterName());
                _textEditor.SelectedChapterIndex = _projectState.Chapters.Count - 1;
            }

            if (_textEditor.SelectedChapterIndex >= 0)
                textBox.Document.Selection.SetRange(TextConstants.MaxUnitCount, TextConstants.MaxUnitCount);
        }
    }

    public class MyRichEditBox : RichEditBox
    {
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);

            if (ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                if (e.Key == VirtualKey.R || e.Key == VirtualKey.Z || e.Key == VirtualKey.Y || e.Key == VirtualKey.I || e.Key == VirtualKey.B)
                    return;

                if (e.Key == VirtualKey.V)
                {
                    _ = HandlePasteAsync();
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        private async Task HandlePasteAsync()
        {
            try
            {
                var content = Clipboard.GetContent();
                if (content.Contains(StandardDataFormats.Text))
                {
                    var text = await content.GetTextAsync();
                    Document.Selection.TypeText(text);
                }
            }
            catch (Exception ex)
            {
                App.TryGetService<ILogger>()?.Warning($"Failed to paste from clipboard: {ex.Message}");
            }
        }
    }
}
