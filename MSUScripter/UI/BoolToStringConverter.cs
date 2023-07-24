using System;
using System.Globalization;
using System.Windows.Data;

namespace MSUScripter.UI;

[ValueConversion(typeof(bool?), typeof(string))]
public sealed class BoolToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? BoolComboBoxItemsSource.Yes : BoolComboBoxItemsSource.No;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return BoolComboBoxItemsSource.Yes == value as string;
    }
}