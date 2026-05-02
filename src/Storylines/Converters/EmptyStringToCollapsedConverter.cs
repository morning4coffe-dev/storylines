using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Storylines.Converters
{
    /// <summary>
    /// Returns <see cref="Visibility.Collapsed"/> when the string value is null or empty,
    /// <see cref="Visibility.Visible"/> otherwise.
    /// </summary>
    public class EmptyStringToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string s)
                return string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
