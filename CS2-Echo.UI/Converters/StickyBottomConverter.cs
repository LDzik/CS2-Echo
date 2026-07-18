using System;
using System.Globalization;
using System.Windows.Data;

namespace CS2_Echo.UI.Converters;

public class StickyBottomConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 ||
            values[0] is not double verticalOffset ||
            values[1] is not double viewportHeight ||
            values[2] is not double elementHeight)
        {
            return 0.0;
        }

        double bottomPadding = 40.0;

        return verticalOffset + viewportHeight - elementHeight - bottomPadding;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}