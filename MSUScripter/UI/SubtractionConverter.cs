using System;
using System.Windows.Data;

namespace MSUScripter.UI;

public class SubtractionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, 
        System.Globalization.CultureInfo culture)
    {
        return (double)value - 25.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, 
        System.Globalization.CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}