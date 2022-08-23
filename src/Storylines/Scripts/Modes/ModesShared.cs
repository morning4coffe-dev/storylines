using Storylines.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Scripts.Modes
{
    class ModesShared
    {
        public static CommandBar NewCommandBar()
        {
            CommandBar cb = new CommandBar()
            {
                DefaultLabelPosition = CommandBarDefaultLabelPosition.Right,
                HorizontalAlignment = HorizontalAlignment.Left,
                Height = 48,
                Margin = new Thickness(8, 0, 8, 0),
            };
            return cb;
        }

        public static Grid NewCommandBarBackground()
        {
            Grid g = new Grid()
            {
                RequestedTheme = AppView.current.RequestedTheme,
                Background = MainPage.commandBar.navigationGrid.Background,
                BorderBrush = MainPage.commandBar.navigationGrid.BorderBrush,
                BorderThickness = MainPage.commandBar.navigationGrid.BorderThickness,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(8, 0, 8, 0),
            };

            return g;
        }

        public static void RemoveChapterTextCommandBar()
        {
            MainPage.chapterText.gridHolder.RowDefinitions.RemoveAt(0);
            MainPage.chapterText.gridHolder.RowDefinitions.RemoveAt(0);
            MainPage.chapterText.gridCommandBarHolder.Visibility = Visibility.Collapsed;
        }
    }
}
