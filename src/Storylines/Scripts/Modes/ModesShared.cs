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
                Background = MainPage.CommandBar.navigationGrid.Background,
                BorderBrush = MainPage.CommandBar.navigationGrid.BorderBrush,
                BorderThickness = MainPage.CommandBar.navigationGrid.BorderThickness,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(8, 0, 8, 0),
            };

            return g;
        }

        public static void RemoveChapterTextCommandBar()
        {
            MainPage.ChapterText.gridHolder.RowDefinitions.RemoveAt(0);
            MainPage.ChapterText.gridHolder.RowDefinitions.RemoveAt(0);
            MainPage.ChapterText.gridCommandBarHolder.Visibility = Visibility.Collapsed;
        }
    }
}
