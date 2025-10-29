using Microsoft.UI.Xaml.Data;
using System;

namespace Storylines.WinUI.Converters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double d)
            {
                return d * 4;
            }
            return 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is double d)
            {
                return d / 4;
            }
            return 25;
        }
    }
}
