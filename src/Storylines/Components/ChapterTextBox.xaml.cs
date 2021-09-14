using Storylines.DialogueWindows;
using Storylines.Pages;
using Storylines.Scripts.Services;
using Storylines.Scripts.Variables;
using System;
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

            MainPage.chapterText = this;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TextHighlighter.selectedTool = TextHighlighter.Tool.Yellow;
            MarkTextBackground();
        }

        #region TextBox
        private void OnTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (MainPage.chapterList.listView.SelectedItem != null)
            {
                textBox.Document.GetText(TextGetOptions.FormatRtf, out var txt);

                if (Chapter.chapters[MainPage.chapterList.listView.SelectedIndex].text != txt && !searchingInTextBox)
                {
                    Chapter.chapters[MainPage.chapterList.listView.SelectedIndex].text = txt;

                    MainPage.current.UpdateDownBar();
                    Scripts.Functions.TimeTravelChapter.SomethingChanged(Scripts.Functions.TimeTravelChapter.Changed.Text, MainPage.chapterList.listView.SelectedItem as Chapter, 0);

                    if (MainPage.focusMode != null)
                        MainPage.focusMode.TextChanged();
                }
            }
        }

        private void OnTextBox_SelectionChanging(RichEditBox sender, RichEditBoxSelectionChangingEventArgs args)
        {
            if (MainPage.chapterList.listView.SelectedItem != null)
            {
                MainPage.current.UpdateDownBar();

                CheckForFormatting();
            }
        }

        private void OnTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (MainPage.chapterList.listView.SelectedItem != null && dialoguesOn)
                if (e.Key == VirtualKey.Enter)
                {
                    PopulateFlyout();

                    Point position = CoreWindow.GetForCurrentThread().PointerPosition;
                    textBoxDialogueNamesFlyout.ShowAt(MainPage.current, new Point(position.X, position.Y));
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
                MainPage.chapterText.textBox.RequestedTheme = ElementTheme.Light;
                MainPage.chapterText.textBoxScrollViewer.RequestedTheme = ElementTheme.Light;
                //(App.Current.Resources["MainTextBoxAcrylicBackground"] as AcrylicBrush).AlwaysUseFallback = true;
                //(App.Current.Resources["MainTextBoxAcrylicBackgroundPointerOverAndFocused"] as AcrylicBrush).AlwaysUseFallback = true;
                //(App.Current.Resources["MainTextBoxAcrylicBackgroundDisabled"] as AcrylicBrush).AlwaysUseFallback = true;
            }
            else
            {
                MainPage.chapterText.textBox.RequestedTheme = MainPage.current.RequestedTheme;
                MainPage.chapterText.textBoxScrollViewer.RequestedTheme = MainPage.current.RequestedTheme;
                //(App.Current.Resources["MainTextBoxAcrylicBackground"] as AcrylicBrush).AlwaysUseFallback = false;
                //(App.Current.Resources["MainTextBoxAcrylicBackgroundPointerOverAndFocused"] as AcrylicBrush).AlwaysUseFallback = false;
                //(App.Current.Resources["MainTextBoxAcrylicBackgroundDisabled"] as AcrylicBrush).AlwaysUseFallback = false;
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
        #endregion
        #endregion

        #region CommandBar
        #region Storymarkdown
        private void PopulateFlyout()
        {
            isFlyoutOpen = true;
            textBoxDialogueNamesFlyout.Items.Clear();

            for (int i = 0; i < Character.characters.Count; i++)
            {
                var item = new MenuFlyoutItem() { Tag = Character.characters[i], Text = Character.characters[i].name };
                item.Click += OnTextBoxDialogueNamesFlyoutItem_Click;;
                textBoxDialogueNamesFlyout.Items.Add(item);
            }

            textBoxDialogueNamesFlyout.Closed += TextBoxDialogueNamesFlyout_Closed;
        }

        private void TextBoxDialogueNamesFlyout_Closed(object sender, object e)
        {
            isFlyoutOpen = false;
        }

        private void OnTextBoxDialogueNamesFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            textBox.Document.GetText(TextGetOptions.None, out string txt);
            _ = textBox.Focus(FocusState.Keyboard);

            var newParagraph = txt.Length > 2;
            if (currentDialogueMode == DialogueMode.Complex)
            {
                string dialogueFullText = Dialogue.Create((sender as MenuFlyoutItem).Tag as Character, newParagraph);
                textBox.Document.Selection.TypeText(dialogueFullText);

                int selectionStartInt = textBox.Document.Selection.StartPosition;
                int textLength = selectionStartInt + dialogueFullText.Length - 17;
                textBox.Document.Selection.SetRange(textLength, textLength);
            }
            else
            {
                string dialogueFullText = Dialogue.CreateSimple((sender as MenuFlyoutItem).Tag as Character, newParagraph);
                textBox.Document.Selection.TypeText(dialogueFullText);
            }
        }

        private void OnTextBoxDialogueNamesFlyout_Closing(Windows.UI.Xaml.Controls.Primitives.FlyoutBase sender, Windows.UI.Xaml.Controls.Primitives.FlyoutBaseClosingEventArgs args)
        {
            //args.Cancel = true;
        }

        public void DialoguesOnOff(bool enabled)
        {
            MainPage.commandBar.dialoguesEnableButton.IsChecked = enabled;
            dialoguesOn = enabled;
        }

        public enum DialogueMode { Complex, Simple }
        public DialogueMode currentDialogueMode;
        public void AddDialogue()
        {
            currentDialogueMode = DialogueMode.Complex;
            PopulateFlyout();
            textBoxDialogueNamesFlyout.ShowAt(textBox);
        }

        public void AddSimpleDialogue()
        {
            currentDialogueMode = DialogueMode.Simple;
            PopulateFlyout();
            textBoxDialogueNamesFlyout.ShowAt(textBox);
        }
        #endregion

        #region Format
        public void CheckForFormatting()
        {
            var format = MainPage.chapterText.textBox.Document.Selection.CharacterFormat;

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
            if (MainPage.chapterList.listView.SelectedItem != null && MainPage.chapterText.textBox.Document.Selection != null)
            {
                textBox.Document.Selection.CharacterFormat.Bold = selectedTextIsBold ? FormatEffect.Off : FormatEffect.On;
                selectedTextIsBold = !selectedTextIsBold; 

                boldTextButton.IsChecked = selectedTextIsBold;
            }
        }

        public void ItalicChapterTextBox()
        {
            if (MainPage.chapterList.listView.SelectedItem != null && MainPage.chapterText.textBox.Document.Selection != null)
            {
                MainPage.chapterText.textBox.Document.Selection.CharacterFormat.Italic = selectedTextIsItalic ? FormatEffect.Off : FormatEffect.On;
                selectedTextIsItalic = !selectedTextIsItalic;
                
                italicTextButton.IsChecked = selectedTextIsItalic;
            }
        }

        public void UnderlineChapterTextBox()
        {
            if (MainPage.chapterList.listView.SelectedItem != null && MainPage.chapterText.textBox.Document.Selection != null)
            {
                MainPage.chapterText.textBox.Document.Selection.CharacterFormat.Underline = selectedTextIsUnderlined ? UnderlineType.None : UnderlineType.Thin;
                selectedTextIsUnderlined = !selectedTextIsUnderlined;

                underlineTextButton.IsChecked = selectedTextIsUnderlined;
            }
        }

        public void StrikethroughChapterTextBox()
        {
            if (MainPage.chapterList.listView.SelectedItem != null && MainPage.chapterText.textBox.Document.Selection != null)
            {
                MainPage.chapterText.textBox.Document.Selection.CharacterFormat.Strikethrough = selectedTextIsStriked ? FormatEffect.Off : FormatEffect.On;
                selectedTextIsStriked = !selectedTextIsStriked;

                strikethroughButton.IsChecked = selectedTextIsStriked;
            }
        }

        public void MarkTextBackground()
        {
            if (TextHighlighter.selectedTool != TextHighlighter.Tool.None)
            {
                highlighterButtonColor.Background = new SolidColorBrush(TextHighlighter.ChangeColor(TextHighlighter.selectedTool));

                if (MainPage.chapterText.textBox.Document.Selection != null && MainPage.chapterList.listView.SelectedItem != null)
                    MainPage.chapterText.textBox.Document.Selection.CharacterFormat.BackgroundColor = TextHighlighter.ChangeColor(TextHighlighter.selectedTool);
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

                int scrollValue = (int)(double)(Windows.Storage.ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ZoomValue] ?? MainPage.current.textBoxZoomSlider.Value);

                if (scrollValue + localScrollValue >= 5 && scrollValue + localScrollValue <= 100)
                {
                    scrollValue += localScrollValue;
                    MainPage.current.textBoxZoomSlider.Value = scrollValue;
                }
            }
        }

        private void OnTextBoxRectangle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Chapter.chapters.Count == 0)
            {
                Chapter.Add(Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("chapterWithoutName"));
                MainPage.chapterList.listView.SelectedIndex = Chapter.chapters.Count - 1;
            }

            if (MainPage.chapterList.listView.SelectedItem != null)
            {
                textBox.Document.Selection.SetRange(TextConstants.MaxUnitCount, TextConstants.MaxUnitCount);
            }
        }
    }

    public class MyRichEditBox : RichEditBox
    {
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);

            if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                if (e.Key == VirtualKey.R || e.Key == VirtualKey.Z || e.Key == VirtualKey.Y || e.Key == VirtualKey.I || e.Key == VirtualKey.B)
                    return;
            base.OnKeyDown(e);
        }
    }
}
