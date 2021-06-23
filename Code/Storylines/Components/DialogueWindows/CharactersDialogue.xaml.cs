using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Storylines.Components.DialogueWindows
{
    public sealed partial class CharactersDialogue : ContentDialog
    {
        public static CharactersDialogue charactersDialogue;
        public bool isEscape = true;

        public Character currentlySelectedCharacter;
        public Button currentlySelectedButton;

        public CharactersDialogue()
        {
            this.InitializeComponent();
            charactersDialogue = this;

            MainPage.currentlyOpenedDialogue = charactersDialogue;
        }

        public static void Open()
        {
            var charactersDialogue = new CharactersDialogue();
            _ = charactersDialogue.ShowAsync();

            charactersDialogue.RequestedTheme = MainPage.mainPage.RequestedTheme;
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            for (int i = 0; i < Characters.characters.Count; i++)
            {
                charactersHolder.Children.Add(NewCharacterButton(Characters.characters[i]));
            }

            CheckIfProjectsHolderIsEmpty();
        }

        private void OnAddNewCharacter_Click(object sender, RoutedEventArgs e)
        {
            EnableEdit(true);

            characterNameBox.Text = String.Empty;
            characterDescriptionBox.Text = String.Empty;

            DisplayCharacterStats(true);
            addNewCharacterButton.Visibility = Visibility.Collapsed;
            editCharacterButton.Visibility = Visibility.Collapsed;
        }

        private void OnEditCharacter_Click(object sender, RoutedEventArgs e)
        {
            EnableEdit(true);

            DisplayCharacterStats(true);
            addNewCharacterButton.Visibility = Visibility.Collapsed;
            editCharacterButton.Visibility = Visibility.Collapsed;
        }

        private Button NewCharacterButton(Character character)
        {
            var bt = new Button()
            {
                Content = character.name,
                Name = character.tag,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 40,
                CornerRadius = new CornerRadius(4),
                Style = (Style)Application.Current.Resources["ButtonRevealStyle"],
            };

            ToolTip toolTip = new ToolTip
            {
                Content = character.name
            };
            ToolTipService.SetToolTip(bt, toolTip);

            bt.Click += OnOpenCharacter_Click;
            bt.RightTapped += OnOpenCharacter_RightTapped;
            return bt;
        }

        private void OnOpenCharacter_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Characters.characters.Count; i++)
            {
                if (Characters.characters[i].tag == (sender as Button).Name)
                {
                    currentlySelectedCharacter = Characters.characters[i];
                }
            }
            currentlySelectedButton = sender as Button;

            characterNameBox.Text = currentlySelectedCharacter.name;
            characterDescriptionBox.Text = currentlySelectedCharacter.description;

            addNewCharacterButton.Visibility = Visibility.Collapsed;
            editCharacterButton.Visibility = Visibility.Visible;

            EnableEdit(false);

            DisplayCharacterStats(true);
        }

        private void OnOpenCharacter_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender as TextBlock != null)
            {
                sender = VisualTreeHelper.GetParent((FrameworkElement)sender);
                sender = VisualTreeHelper.GetParent((FrameworkElement)sender) as Grid;
            }

            if (sender as Grid != null)
            {
                sender = VisualTreeHelper.GetParent((FrameworkElement)sender) as Button;
            }

            if (sender as Button != null)
            {
                charactersHolderFlyout.ShowAt((Button)sender, e.GetPosition((Button)sender));
                characterToRemove = (Button)sender;
            }
        }

        public void DisplayCharacterStats(bool characterStats)
        {
            if (characterStats)
            {
                characterStatsGrid.Visibility = Visibility.Visible;
                charactersHolderGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                characterStatsGrid.Visibility = Visibility.Collapsed;
                charactersHolderGrid.Visibility = Visibility.Visible;
            }
        }

        public void EnableEdit(bool enable)
        {
            submitOrCancelHolder.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            backButton.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;

            characterNameBox.IsEnabled = enable;
            characterDescriptionBox.IsEnabled = enable;
        }

        private Button characterToRemove;

        private void OnProjectRemove_Click(object sender, RoutedEventArgs e)
        {
            if (characterToRemove != null)
            {
                charactersHolder.Children.Remove(characterToRemove);
                Character.Remove(characterToRemove.Name);

                CheckIfProjectsHolderIsEmpty();
            }
        }

        public void CloseMenu()
        {
            this.Hide();
        }

        public void CheckIfProjectsHolderIsEmpty()
        {
            if (charactersHolder.Children.Count > 0)
            {
                noFilesText.Visibility = Visibility.Collapsed;
            }
            else
            {
                noFilesText.Visibility = Visibility.Visible;
            }
        }

        private void OnSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentlySelectedCharacter == null)
            {
                var character = Character.CreateNew(characterNameBox.Text, characterDescriptionBox.Text);
                charactersHolder.Children.Add(NewCharacterButton(character));
                CheckIfProjectsHolderIsEmpty();
            }
            else
            {
                for (int i = 0; i < charactersHolder.Children.Count; i++)
                {
                    if (currentlySelectedButton == charactersHolder.Children[i])
                    {
                        (charactersHolder.Children[i] as Button).Content = characterNameBox.Text;
                        Character.Change(i, characterNameBox.Text, characterDescriptionBox.Text);
                        MainPage.mainPage.SomethingChanged();
                    }
                }
            }

            currentlySelectedCharacter = null;

            DisplayCharacterStats(false);

            MainPage.mainPage.SomethingChanged();

            addNewCharacterButton.Visibility = Visibility.Visible;
            editCharacterButton.Visibility = Visibility.Collapsed;
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayCharacterStats(false);

            currentlySelectedCharacter = null;

            addNewCharacterButton.Visibility = Visibility.Visible;
            editCharacterButton.Visibility = Visibility.Collapsed;
        }

        private void OnBackButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayCharacterStats(false);

            currentlySelectedCharacter = null;

            addNewCharacterButton.Visibility = Visibility.Visible;
            editCharacterButton.Visibility = Visibility.Collapsed;
        }

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
