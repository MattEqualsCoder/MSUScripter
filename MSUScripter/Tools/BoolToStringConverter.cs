using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MSUScripter.Tools;

public sealed class BoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (bool?)value == true ? NullableBoolComboBoxItemsSource.Yes : NullableBoolComboBoxItemsSource.No;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch ((string?)value)
        {
            case NullableBoolComboBoxItemsSource.Yes:
                return true;
            default:
                return false;
        }
    }
}