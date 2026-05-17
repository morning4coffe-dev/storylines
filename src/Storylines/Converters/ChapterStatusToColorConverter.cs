using Windows.UI;

namespace Storylines.Converters;

public class ChapterStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ChapterStatus status)
        {
            switch (status)
            {
                case ChapterStatus.Draft: return Color.FromArgb(255, 158, 158, 158);    // Gray
                case ChapterStatus.Writing: return Color.FromArgb(255, 66, 165, 245);    // Blue
                case ChapterStatus.Revision: return Color.FromArgb(255, 255, 167, 38);   // Orange
                case ChapterStatus.Final: return Color.FromArgb(255, 102, 187, 106);     // Green
            }
        }
        return Color.FromArgb(255, 158, 158, 158);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
