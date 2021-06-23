using Storylines.DialogueWindows;
using Storylines.Pages;
using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Text;
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

        //private bool selectedTextIsColoured = false;

        private bool searchingInTextBox = false;

        public ChapterTextBox()
        {
            this.InitializeComponent();

            MainPage.chapterText = this;
        }
        #region TextBox
        private void OnTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (MainPage.chapterList.chaptersListView.SelectedItem != null)
            {
                textBox.Document.GetText(TextGetOptions.FormatRtf, out var txt);

                if (MainPage.chapterList.chapters[MainPage.chapterList.chaptersListView.SelectedIndex].text != txt && !searchingInTextBox)
                {
                    MainPage.chapterList.chapters[MainPage.chapterList.chaptersListView.SelectedIndex].text = txt;

                    MainPage.mainPage.UpdateDownBar();
                    MainPage.mainPage.SomethingChanged();

                    if (dialoguesOn)
                    {

                    }
                }
            }
        }

        private void OnTextBox_SelectionChanging(RichEditBox sender, RichEditBoxSelectionChangingEventArgs args)
        {
            if (MainPage.chapterList.chaptersListView.SelectedItem != null)
            {
                MainPage.mainPage.UpdateDownBar();

                CheckForFormatting();

                if (TextHighlighter.selectedTool != TextHighlighter.Tool.None)
                    TextFormatters.MarkTextBackground(false);
            }
        }

        private void OnTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (MainPage.chapterList.chaptersListView.SelectedItem != null && dialoguesOn)
            {
                if (e.Key == VirtualKey.Enter)
                {
                    PopulateFlyout();

                    var position = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                    textBoxDialogueNamesFlyout.ShowAt(MainPage.mainPage, new Point(position.X, position.Y));
                }
            }
        }

        public void ChangeTextColor()
        {
            var txtRange = textBox.Document.GetRange(0, TextConstants.MaxUnitCount);

            if (textBox.ActualTheme == ElementTheme.Dark)
            {
                txtRange.CharacterFormat.ForegroundColor = Colors.White;
            }
            else
            if (textBox.ActualTheme == ElementTheme.Light)
            {
                txtRange.CharacterFormat.ForegroundColor = Colors.Black;
            }
        }

        public void TextBoxWhiteBackground(bool whiteBackground)
        {
            if (whiteBackground)
            {
                MainPage.chapterText.textBox.RequestedTheme = ElementTheme.Light;
                MainPage.chapterText.textBoxScrollViewer.RequestedTheme = ElementTheme.Light;
                (App.Current.Resources["MainTextBoxAcrylicBackground"] as AcrylicBrush).AlwaysUseFallback = true;
                (App.Current.Resources["MainTextBoxAcrylicBackgroundPointerOverAndFocused"] as AcrylicBrush).AlwaysUseFallback = true;
                (App.Current.Resources["MainTextBoxAcrylicBackgroundDisabled"] as AcrylicBrush).AlwaysUseFallback = true;
            }
            else
            {
                MainPage.chapterText.textBox.RequestedTheme = MainPage.mainPage.RequestedTheme;
                MainPage.chapterText.textBoxScrollViewer.RequestedTheme = MainPage.mainPage.RequestedTheme;
                (App.Current.Resources["MainTextBoxAcrylicBackground"] as AcrylicBrush).AlwaysUseFallback = false;
                (App.Current.Resources["MainTextBoxAcrylicBackgroundPointerOverAndFocused"] as AcrylicBrush).AlwaysUseFallback = false;
                (App.Current.Resources["MainTextBoxAcrylicBackgroundDisabled"] as AcrylicBrush).AlwaysUseFallback = false;
            }
        }

        #region CommandBarFlyout
        private void Menu_Opening(object sender, object e)
        {
            CommandBarFlyout myFlyout = sender as CommandBarFlyout;
            if (myFlyout.Target == textBox)
            {
                var font = new FontFamily("Segoe Fluent Icons") ?? new FontFamily("Segoe MDL2 Assets");
                AppBarToggleButton myButton = new AppBarToggleButton() { Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "" }, IsChecked = selectedTextIsStriked };

                myButton.Click += OnStrikethroughTextButton_Click;
                myButton.Command = new StandardUICommand();
                myFlyout.PrimaryCommands.Add(myButton);
            }
        }

        private void REBCustom_Loaded(object sender, RoutedEventArgs e)
        {
            textBox.SelectionFlyout.Opening += Menu_Opening;
            textBox.ContextFlyout.Opening += Menu_Opening;
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

            Color highlightForegroundColor = SettingsPage.appColorEnabled ? SettingsPage.appColor : (Color)Application.Current.Resources["SystemAccentColor"];

            if (textToFind != null)
            {
                ITextRange searchRange = textBox.Document.GetRange(0, 0);
                while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, FindOptions.None) > 0)
                {
                    searchRange.CharacterFormat.ForegroundColor = highlightForegroundColor;
                }
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
            {
                HideSearch();
            }
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

        private void OnSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (textBox.Document.Selection.Length > 0)
            {
                searchTextBox.Text = textBox.Document.Selection.Text;
            }

            SearchBoxHighlightMatches(searchTextBox.Text);

            if (ActualWidth < 800)
            {
                chapterTextCommandBar.Visibility = Visibility.Collapsed;
            }

            searchTextBox.Width = ActualWidth < 800 ? ActualWidth - 15 : 320;

            if (searchTextBox.Visibility == Visibility.Collapsed)
            {
                searchButton.Visibility = Visibility.Collapsed;
                searchTextBox.Visibility = Visibility.Visible;
            }

            searchTextBox.Focus(FocusState.Keyboard);
        }
        #endregion
        #endregion

        #region CommandBar
        #region Format

        public void CheckForFormatting()
        {
            var format = MainPage.chapterText.textBox.Document.Selection.CharacterFormat;

            selectedTextIsBold = format.Bold != FormatEffect.Off;
            selectedTextIsItalic = format.Italic != FormatEffect.Off;
            selectedTextIsUnderlined = format.Underline != UnderlineType.None;
            selectedTextIsStriked = format.Strikethrough != FormatEffect.Off;
            //seletctedTextIsColoured = format.BackgroundColor == FormatEffect.Off ? false : true;

            boldTextButton.IsChecked = selectedTextIsBold;
            italicTextButton.IsChecked = selectedTextIsItalic;
            underlineTextButton.IsChecked = selectedTextIsUnderlined;
            strikethroughAddButton.IsChecked = selectedTextIsStriked;
        }

        private void OnBoldTextButton_Click(object sender, RoutedEventArgs e)
        {
            TextFormatters.BoldChapterTextBox(selectedTextIsBold);
            selectedTextIsBold = !selectedTextIsBold;
        }

        private void OnItalicTextButton_Click(object sender, RoutedEventArgs e)
        {
            TextFormatters.ItalicChapterTextBox(selectedTextIsItalic);
            selectedTextIsItalic = !selectedTextIsItalic;
        }

        private void OnUnderlineTextButton_Click(object sender, RoutedEventArgs e)
        {
            TextFormatters.UnderlineChapterTextBox(selectedTextIsUnderlined);
            selectedTextIsUnderlined = !selectedTextIsUnderlined;
        }

        private void OnStrikethroughTextButton_Click(object sender, RoutedEventArgs e)
        {
            TextFormatters.StrikethroughChapterTextBox(selectedTextIsStriked);
            selectedTextIsStriked = !selectedTextIsStriked;
        }

        private void OnHighlighterButton_Click(object sender, RoutedEventArgs e)
        {
            if (TextHighlighter.selectedTool != TextHighlighter.Tool.None)
            {
                TextHighlighter.lastTool = TextHighlighter.selectedTool;
                TextHighlighter.ChangeColor(TextHighlighter.Tool.None);
                highlighterButton.Background = new SolidColorBrush(TextHighlighter.color);
            }
            else
            {
                TextHighlighter.ChangeColor(TextHighlighter.lastTool);
                highlighterButton.Background = new SolidColorBrush(TextHighlighter.color);
                TextFormatters.MarkTextBackground(false);
            }
        }

        private void OnHighlighterMoreButton_Click(object sender, RoutedEventArgs e)
        {
            OnHightighterButton_Holding(sender, new HoldingRoutedEventArgs());
        }

        private void OnHightighterButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (!highlighterButtonFlyout.IsOpen)
            {
                highlighterButtonFlyout.ShowAt(highlighterButton);
            }
            else
            {
                highlighterButtonFlyout.Hide();
            }
        }

        private void OnHightighterButton_RightClick(object sender, RightTappedRoutedEventArgs e)
        {
            OnHightighterButton_Holding(sender, new HoldingRoutedEventArgs());
        }

        private void OnHighlighterColorButton_Click(object sender, RoutedEventArgs e)
        {
            TextHighlighter.Tool tool = (TextHighlighter.Tool)Enum.Parse(typeof(TextHighlighter.Tool), (sender as Button).Tag.ToString());
            TextHighlighter.ChangeColor(tool);
            highlighterButton.Background = new SolidColorBrush(TextHighlighter.color);

            TextFormatters.MarkTextBackground(false);
            highlighterButtonFlyout.Hide();
        }
        #endregion 

        private void PopulateFlyout()
        {
            textBoxDialogueNamesFlyout.Items.Clear();

            for (int i = 0; i < Characters.characters.Count; i++)
            {
                var item = new MenuFlyoutItem() { Text = Characters.characters[i].name };
                item.Click += OnTextBoxDialogueNamesFlyoutItem_Click;
                textBoxDialogueNamesFlyout.Items.Add(item);
            }
        }

        private void OnTextBoxDialogueNamesFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            textBox.Document.GetText(TextGetOptions.None, out var txt);

            var newParagraph = txt.Length > 2 ? "\n" : "";

            textBox.Document.Selection.TypeText($"{newParagraph}{(sender as MenuFlyoutItem).Text}: ");//\\b dolor\\b0 

            //cant type it away
            //            < MenuFlyout x: Name = "MyFlyout"
            //            Closing = "MyFlyout_Closing"
            //            Closed = "MyFlyout_Closed"
            //    < MenuFlyoutItem />
            //    < MenuFlyoutItem />
            //</ MenuFlyout >

            //private void MyFlyout_Closing(Windows.UI.Xaml.Controls.Primitives.FlyoutBase sender, Windows.UI.Xaml.Controls.Primitives.FlyoutBaseClosingEventArgs args)
            //            {
            //                // Whatever logic you need to decide
            //                args.Cancel = true;
            //            }
        }

        private void OnAddDialogueButton_Click(object sender, RoutedEventArgs e)
        {
            PopulateFlyout();

            textBoxDialogueNamesFlyout.ShowAt(newDialogueButton);
        }

        private void OnDialoguesOnOffButton_Click(object sender, RoutedEventArgs e)
        {
            dialoguesOn = (bool)dialoguesOnOffButton.IsChecked;
            newDialogueButton.IsEnabled = (bool)dialoguesOnOffButton.IsChecked;
        }

        private void OnDictationButton_Click(object sender, RoutedEventArgs e)
        {
            //textBox.Focus(FocusState.Pointer);

            //InputInjector inputInjector = InputInjector.TryCreate();

            //var win = new InjectedInputKeyboardInfo();
            //win.VirtualKey = (ushort)VirtualKey.LeftWindows;
            //win.KeyOptions = InjectedInputKeyOptions.None;


            //var h = new InjectedInputKeyboardInfo();
            //h.VirtualKey = (ushort)VirtualKey.H;
            //h.KeyOptions = InjectedInputKeyOptions.None;


            //inputInjector.InjectKeyboardInput(new[] { win, h });
            //	  <rescap:Capability Name="inputInjectionBrokered"/>
        }
        #endregion

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid.SetColumn(searchTextBox, ActualWidth < 850 ? 0 : 1);
            chapterTextCommandBar.Visibility = searchTextBox.Visibility == Visibility.Visible && ActualWidth < 780 ? Visibility.Collapsed : Visibility.Visible;
            searchTextBox.Width = searchTextBox.Visibility == Visibility.Visible && ActualWidth < 850 ? ActualWidth-15 : 320;

            chapterTextCommandBar.OverflowButtonVisibility = ActualWidth < 588 ? CommandBarOverflowButtonVisibility.Visible : CommandBarOverflowButtonVisibility.Collapsed;

            highlighterButtonMoreColors.Visibility = ActualWidth < 465 ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
