using Storylines.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Storylines.Components
{
    public sealed partial class ChapterTextBox : UserControl
    {
        private readonly ObservableCollection<Character> dialoguePickerCharacters = new ObservableCollection<Character>();
        private readonly ObservableCollection<Character> recentDialoguePickerCharacters = new ObservableCollection<Character>();
        private readonly ObservableCollection<string> recentDialogueCharacterTokens = new ObservableCollection<string>();

        public bool dialoguesOn = false;

        private bool selectedTextIsBold = false;
        private bool selectedTextIsItalic = false;
        private bool selectedTextIsUnderlined = false;
        private bool selectedTextIsStriked = false;
        //private bool selectedTextIsColou = false;

        private bool searchingInTextBox = false;

        public ChapterTextBox()
        {
            InitializeComponent();

            dialoguePickerList.ItemsSource = dialoguePickerCharacters;
            dialoguePickerRecentList.ItemsSource = recentDialoguePickerCharacters;

            MainPage.ChapterText = this;

            ServiceLocator.Events.Subscribe<SettingChangedEvent>(OnSettingChanged);
        }

        private void OnSettingChanged(SettingChangedEvent e)
        {
            if (e.SettingKey == SettingsValueStrings.TextBoxSolidBackground)
                TextBoxWhiteBackground((bool)e.Value);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TextHighlighter.selectedTool = TextHighlighter.Tool.Yellow;
            MarkTextBackground();
        }

        #region TextBox
        private void OnTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            var selectedIndex = Scripts.Services.ServiceLocator.TextEditor.SelectedChapterIndex;
            if (selectedIndex >= 0 && selectedIndex < Scripts.Services.ServiceLocator.ProjectState.Chapters.Count)
            {
                textBox.Document.GetText(TextGetOptions.FormatRtf, out var txt);

                if (Scripts.Services.ServiceLocator.ProjectState.Chapters[selectedIndex].text != txt && !searchingInTextBox)
                {
                    Scripts.Services.ServiceLocator.ProjectState.Chapters[selectedIndex].text = txt;

                    MainPage.Current.UpdateDownBar();
                    Scripts.Functions.TimeTravelChapter.SomethingChanged(Scripts.Functions.TimeTravelChapter.Changed.Text, Scripts.Services.ServiceLocator.ProjectState.Chapters[selectedIndex], 0);

                    if (MainPage.FocusMode != null)
                        MainPage.FocusMode.TextChanged();
                }
            }
        }

        private void OnTextBox_SelectionChanging(RichEditBox sender, RichEditBoxSelectionChangingEventArgs args)
        {
            if (Scripts.Services.ServiceLocator.TextEditor.SelectedChapterIndex >= 0)
            {
                MainPage.Current.UpdateDownBar();

                CheckForFormatting();
            }
        }

        private void OnTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Scripts.Services.ServiceLocator.TextEditor.SelectedChapterIndex >= 0 && dialoguesOn)
                if (e.Key == VirtualKey.Enter)
                {
                    PopulateFlyout();

                    Point position = CoreWindow.GetForCurrentThread().PointerPosition;
                    textBoxDialogueNamesFlyout.ShowAt(MainPage.Current, new Point(position.X, position.Y));
                    e.Handled = true;
                }
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
            }
            else
            {
                textBox.RequestedTheme = MainPage.Current.RequestedTheme;
                textBoxScrollViewer.RequestedTheme = MainPage.Current.RequestedTheme;
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
            if (UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Mouse)
                (sender as Microsoft.UI.Xaml.Controls.TextCommandBarFlyout).Hide();
            else
                Menu_Opening(sender, e);
        }

        private bool isFlyoutOpen = false;

        private void REBCustom_Loaded(object sender, RoutedEventArgs e)
        {
            textBox.SelectionFlyout.Opening += SelectMenu_Opening;
            textBox.ContextFlyout.Opening += Menu_Opening;
            textBox.ContextFlyout.Closing += Menu_Closing;
            textBox.SelectionFlyout.Closing += Menu_Closing;
        }

        private void Menu_Closing(Windows.UI.Xaml.Controls.Primitives.FlyoutBase sender, Windows.UI.Xaml.Controls.Primitives.FlyoutBaseClosingEventArgs args)
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
            SearchBoxHighlightMatches(searchTextBox.Text);
        }

        private void SearchBoxHighlightMatches(string textToFind)
        {
            SearchBoxRemoveHighlights();

            Color highlightForegroundColor = ThemeSettings.GetCurrentAccentColor();

            if (textToFind != null)
            {
                ITextRange searchRange = textBox.Document.GetRange(0, 0);
                while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, FindOptions.None) > 0)
                    searchRange.CharacterFormat.ForegroundColor = highlightForegroundColor;
            }
        }

        private void SearchBoxRemoveHighlights()
        {
            ITextRange documentRange = textBox.Document.GetRange(0, TextConstants.MaxUnitCount);
            SolidColorBrush defaultForeground = textBox.Foreground as SolidColorBrush;

            documentRange.CharacterFormat.ForegroundColor = defaultForeground.Color;
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
            if (textBox.Document.Selection.Length > 0)
                searchTextBox.Text = textBox.Document.Selection.Text;

            SearchBoxHighlightMatches(searchTextBox.Text);

            if (AppView.current.ActualWidth < 950)
                chapterTextCommandBar.Visibility = Visibility.Collapsed;

            searchTextBox.Width = AppView.current.ActualWidth < 950 ? ActualWidth - 15 : 320;

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
        public void OpenSearchAndReplace()
        {
            HideSearch();
            searchButton.Visibility = Visibility.Collapsed;
            chapterTextCommandBar.Visibility = Visibility.Collapsed;
            searchReplacePanel.Visibility = Visibility.Visible;
            searchingInTextBox = true;

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

        private void SearchReplaceHighlight()
        {
            SearchBoxRemoveHighlights();

            var textToFind = searchReplaceFindBox.Text;
            if (string.IsNullOrEmpty(textToFind))
            {
                searchMatchCount.Text = string.Empty;
                return;
            }

            var findOptions = FindOptions.None;
            if (matchCaseToggle.IsChecked == true)
                findOptions |= FindOptions.Case;
            if (wholeWordToggle.IsChecked == true)
                findOptions |= FindOptions.Word;

            Color highlightColor = ThemeSettings.GetCurrentAccentColor();
            int matchCount = 0;

            ITextRange searchRange = textBox.Document.GetRange(0, 0);
            while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, findOptions) > 0)
            {
                searchRange.CharacterFormat.ForegroundColor = highlightColor;
                matchCount++;
            }

            searchMatchCount.Text = matchCount > 0 ? $"{matchCount} match{(matchCount == 1 ? "" : "es")}" : "No matches";
        }

        private void OnReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            var textToFind = searchReplaceFindBox.Text;
            var replaceWith = searchReplaceBox.Text;
            if (string.IsNullOrEmpty(textToFind)) return;

            var findOptions = FindOptions.None;
            if (matchCaseToggle.IsChecked == true) findOptions |= FindOptions.Case;
            if (wholeWordToggle.IsChecked == true) findOptions |= FindOptions.Word;

            // Find from current selection position
            var selection = textBox.Document.Selection;
            if (selection.FindText(textToFind, TextConstants.MaxUnitCount, findOptions) > 0)
            {
                selection.SetText(TextSetOptions.None, replaceWith);
            }

            SearchReplaceHighlight();
        }

        private void OnReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            var textToFind = searchReplaceFindBox.Text;
            var replaceWith = searchReplaceBox.Text;
            if (string.IsNullOrEmpty(textToFind)) return;

            var findOptions = FindOptions.None;
            if (matchCaseToggle.IsChecked == true) findOptions |= FindOptions.Case;
            if (wholeWordToggle.IsChecked == true) findOptions |= FindOptions.Word;

            int replacements = 0;
            ITextRange searchRange = textBox.Document.GetRange(0, 0);
            while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, findOptions) > 0)
            {
                searchRange.SetText(TextSetOptions.None, replaceWith);
                replacements++;
            }

            searchMatchCount.Text = $"{replacements} replacement{(replacements == 1 ? "" : "s")} made";
        }

        private void OnSearchReplaceClose_Click(object sender, RoutedEventArgs e)
        {
            HideSearchAndReplace();
        }

        private void HideSearchAndReplace()
        {
            searchingInTextBox = false;
            SearchBoxRemoveHighlights();
            searchReplacePanel.Visibility = Visibility.Collapsed;
            searchButton.Visibility = Visibility.Visible;
            chapterTextCommandBar.Visibility = Visibility.Visible;
        }
        #endregion
        #endregion

        #region CommandBar
        #region Storymarkdown
        private void PopulateFlyout()
        {
            isFlyoutOpen = true;
            dialoguePickerSearchBox.Text = string.Empty;
            RefreshDialoguePickerCharacters();
        }

        private void TextBoxDialogueNamesFlyout_Closed(object sender, object e)
        {
            isFlyoutOpen = false;
        }

        private void OnDialoguePickerCharacter_ItemClick(object sender, ItemClickEventArgs e)
        {
            InsertDialogue((Character)e.ClickedItem);
        }

        private void InsertDialogue(Character character)
        {
            _ = textBox.Focus(FocusState.Keyboard);

            var hasTextBeforeCursor = textBox.Document.Selection.StartPosition > 0;
            string dialogueFullText = Dialogue.Create(character, hasTextBeforeCursor);
            textBox.Document.Selection.TypeText(dialogueFullText);

            RememberRecentCharacter(character);
            textBoxDialogueNamesFlyout.Hide();
        }

        private void OnDialoguePickerSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshDialoguePickerCharacters();
        }

        private void OnTextBoxDialogueNamesFlyout_Opened(object sender, object e)
        {
            dialoguePickerSearchBox.Focus(FocusState.Programmatic);
        }

        private void RefreshDialoguePickerCharacters()
        {
            var query = dialoguePickerSearchBox?.Text?.Trim() ?? string.Empty;
            var characters = Scripts.Services.ServiceLocator.ProjectState.Characters
                .Where(character => DialogueCharacterMatches(character, query))
                .OrderBy(character => character.name)
                .ToList();

            dialoguePickerCharacters.Clear();
            foreach (var character in characters)
                dialoguePickerCharacters.Add(character);

            var recentCharacters = recentDialogueCharacterTokens
                .Select(token => Scripts.Services.ServiceLocator.ProjectState.Characters.FirstOrDefault(character => character.token == token))
                .Where(character => character != null && DialogueCharacterMatches(character, query))
                .ToList();

            recentDialoguePickerCharacters.Clear();
            foreach (var recentCharacter in recentCharacters)
                recentDialoguePickerCharacters.Add(recentCharacter);

            dialoguePickerRecentSection.Visibility = string.IsNullOrWhiteSpace(query) && recentDialoguePickerCharacters.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            dialoguePickerEmptyText.Visibility = dialoguePickerCharacters.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool DialogueCharacterMatches(Character character, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            var searchTarget = string.Join(" ", new[]
            {
                character?.name,
                character?.role,
                character?.traitsText,
                character?.description,
            });

            return searchTarget.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void RememberRecentCharacter(Character character)
        {
            if (character == null)
                return;

            if (recentDialogueCharacterTokens.Contains(character.token))
                recentDialogueCharacterTokens.Remove(character.token);

            recentDialogueCharacterTokens.Insert(0, character.token);

            while (recentDialogueCharacterTokens.Count > 4)
                recentDialogueCharacterTokens.RemoveAt(recentDialogueCharacterTokens.Count - 1);
        }

        private void OnTextBoxDialogueNamesFlyout_Closing(Windows.UI.Xaml.Controls.Primitives.FlyoutBase sender, Windows.UI.Xaml.Controls.Primitives.FlyoutBaseClosingEventArgs args)
        {
            //args.Cancel = true;
        }

        public void DialoguesOnOff(bool enabled)
        {
            MainPage.CommandBar.dialoguesEnableButton.IsChecked = enabled;
            dialoguesOn = enabled;
        }

        public void AddDialogue()
        {
            if (Scripts.Services.ServiceLocator.ProjectState.Characters.Count > 0)
            {
                PopulateFlyout();
                textBoxDialogueNamesFlyout.ShowAt(textBox);
            }
            else
                NoCharactersYet();
        }

        private void NoCharactersYet()
        {
            _ = NotificationManager.DisplayNoCharactersInProjectDialogue();
        }
        #endregion

        #region Format
        public void CheckForFormatting()
        {
            var format = MainPage.ChapterText.textBox.Document.Selection.CharacterFormat;

            selectedTextIsBold = format.Bold != FormatEffect.Off;
            selectedTextIsItalic = format.Italic != FormatEffect.Off;
            selectedTextIsUnderlined = format.Underline != UnderlineType.None;
            selectedTextIsStriked = format.Strikethrough != FormatEffect.Off;

            boldTextButton.IsChecked = selectedTextIsBold;
            italicTextButton.IsChecked = selectedTextIsItalic;
            underlineTextButton.IsChecked = selectedTextIsUnderlined;
            strikethroughButton.IsChecked = selectedTextIsStriked;
        }

        public void BoldChapterTextBox()
        {
            if (MainPage.ChapterList.listView.SelectedItem != null && MainPage.ChapterText.textBox.Document.Selection != null)
            {
                textBox.Document.Selection.CharacterFormat.Bold = selectedTextIsBold ? FormatEffect.Off : FormatEffect.On;
                selectedTextIsBold = !selectedTextIsBold; 

                boldTextButton.IsChecked = selectedTextIsBold;
            }
        }

        public void ItalicChapterTextBox()
        {
            if (MainPage.ChapterList.listView.SelectedItem != null && MainPage.ChapterText.textBox.Document.Selection != null)
            {
                MainPage.ChapterText.textBox.Document.Selection.CharacterFormat.Italic = selectedTextIsItalic ? FormatEffect.Off : FormatEffect.On;
                selectedTextIsItalic = !selectedTextIsItalic;
                
                italicTextButton.IsChecked = selectedTextIsItalic;
            }
        }

        public void UnderlineChapterTextBox()
        {
            if (MainPage.ChapterList.listView.SelectedItem != null && MainPage.ChapterText.textBox.Document.Selection != null)
            {
                MainPage.ChapterText.textBox.Document.Selection.CharacterFormat.Underline = selectedTextIsUnderlined ? UnderlineType.None : UnderlineType.Thin;
                selectedTextIsUnderlined = !selectedTextIsUnderlined;

                underlineTextButton.IsChecked = selectedTextIsUnderlined;
            }
        }

        public void StrikethroughChapterTextBox()
        {
            if (MainPage.ChapterList.listView.SelectedItem != null && MainPage.ChapterText.textBox.Document.Selection != null)
            {
                MainPage.ChapterText.textBox.Document.Selection.CharacterFormat.Strikethrough = selectedTextIsStriked ? FormatEffect.Off : FormatEffect.On;
                selectedTextIsStriked = !selectedTextIsStriked;

                strikethroughButton.IsChecked = selectedTextIsStriked;
            }
        }

        public void MarkTextBackground()
        {
            if (TextHighlighter.selectedTool != TextHighlighter.Tool.None)
            {
                highlighterButtonColor.Background = new SolidColorBrush(TextHighlighter.ChangeColor(TextHighlighter.selectedTool));

                if (MainPage.ChapterText.textBox.Document.Selection != null && MainPage.ChapterList.listView.SelectedItem != null)
                    MainPage.ChapterText.textBox.Document.Selection.CharacterFormat.BackgroundColor = TextHighlighter.ChangeColor(TextHighlighter.selectedTool);
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
            TextHighlighter.selectedTool = (TextHighlighter.Tool)Enum.Parse(typeof(TextHighlighter.Tool), (sender as Button).Tag.ToString());

            MarkTextBackground();
            highlighterButtonFlyout.Hide();
        }

        private void OnTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isFlyoutOpen)
            {
                textBox.Document.Selection.SetRange(0, 0);

                boldTextButton.IsChecked = false;
                italicTextButton.IsChecked = false;
                underlineTextButton.IsChecked = false;
                strikethroughButton.IsChecked = false;
            }
        }
        #endregion 
        #endregion

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid.SetColumn(searchTextBox, AppView.current.ActualWidth < 950 ? 0 : 1);
            chapterTextCommandBar.Visibility = searchTextBox.Visibility == Visibility.Visible && AppView.current.ActualWidth < 950 ? Visibility.Collapsed : Visibility.Visible;
            searchTextBox.Width = searchTextBox.Visibility == Visibility.Visible && AppView.current.ActualWidth < 950 ? ActualWidth - 15 : 320;

            highlighterButtonMoreColors.Visibility = ActualWidth < 465 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnTextBox_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);

            if ((ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
            {
                int localScrollValue = e.GetCurrentPoint((UIElement)sender).Properties.MouseWheelDelta / 24;

                int scrollValue = (int)(double)(Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ZoomValue] ?? MainPage.Current.textBoxZoomSlider.Value);

                if (scrollValue + localScrollValue >= 5 && scrollValue + localScrollValue <= 100)
                {
                    scrollValue += localScrollValue;
                    MainPage.Current.textBoxZoomSlider.Value = scrollValue;
                }
            }
        }

        private void OnTextBoxRectangle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Scripts.Services.ServiceLocator.ProjectState.Chapters.Count == 0)
            {
                Scripts.Services.ServiceLocator.ProjectState.AddChapter(Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("chapterWithoutName"));
                MainPage.ChapterList.listView.SelectedIndex = Scripts.Services.ServiceLocator.ProjectState.Chapters.Count - 1;
            }

            if (MainPage.ChapterList.listView.SelectedItem != null)
                textBox.Document.Selection.SetRange(TextConstants.MaxUnitCount, TextConstants.MaxUnitCount);
        }
    }

    public class MyRichEditBox : RichEditBox
    {
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);

            if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
            {
                if (e.Key == VirtualKey.R || e.Key == VirtualKey.Z || e.Key == VirtualKey.Y || e.Key == VirtualKey.I || e.Key == VirtualKey.B)
                    return;

                if (e.Key == VirtualKey.V)
                {
                    HandlePaste();
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        private async void HandlePaste()
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
            catch (Exception)
            {
                // Clipboard access can fail (e.g., remote desktop, locked clipboard)
            }
        }
    }
}
