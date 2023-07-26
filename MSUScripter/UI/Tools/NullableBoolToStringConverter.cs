using System;
using System.Globalization;
using System.Windows.Data;

namespace MSUScripter.UI.Tools;

[ValueConversion(typeof(bool?), typeof(string))]
public sealed class NullableBoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return NullableBoolComboBoxItemsSource.Unspecified;

        return (bool)value ? NullableBoolComboBoxItemsSource.Yes : NullableBoolComboBoxItemsSource.No;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        switch ((string)value)
        {
            case NullableBoolComboBoxItemsSource.Yes:
                return true;
            case NullableBoolComboBoxItemsSource.No:
                return false;
            default:
                return null;
        }
    }
}