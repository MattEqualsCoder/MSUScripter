using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MSUScripter.UI.Tools;

public abstract class Helpers
{
    public static void DecimalTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var newText = textBox.Text + e.Text;
        e.Handled = !IsDecimalTextAllowed(e.Text);
    }

    public static void DecimalTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = e.DataObject.GetData(typeof(string)) as string;
            if (!IsDecimalTextAllowed(text ?? ""))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }
    
    public static void DecimalTextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && !_decimalRegex.IsMatch(textBox.Text))
        {
            textBox.Text = "";
        }
    }
    
    public static void IntTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var newText = textBox.Text + e.Text;
        e.Handled = !IsIntTextAllowed(e.Text);
    }

    public static void IntTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = e.DataObject.GetData(typeof(string)) as string;
            if (!IsIntTextAllowed(text ?? ""))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }
    
    public static void IntTextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && !_intRegex.IsMatch(textBox.Text))
        {
            textBox.Text = "";
        }
    }
    
    public static bool ConvertViewModel<A, B>(A input, B output) where B : new()
    {
        var propertiesA = typeof(A).GetProperties().Where(x => x.CanWrite && x.PropertyType.Namespace?.Contains("MSU") != true).ToDictionary(x => x.Name, x => x);
        var propertiesB = typeof(B).GetProperties().Where(x => x.CanWrite && x.PropertyType.Namespace?.Contains("MSU") != true).ToDictionary(x => x.Name, x => x);
        var updated = false;

        if (propertiesA.Count != propertiesB.Count)
        {
            throw new InvalidOperationException($"Types {typeof(A).Name} and {typeof(B).Name} are not compatible");
        }

        foreach (var propA in propertiesA.Values)
        {
            if (!propertiesB.TryGetValue(propA.Name, out var propB))
            {
                continue;
            }

            if (propA.PropertyType == typeof(List<A>))
            {
                var aValue = propA.GetValue(input) as List<A>;
                var bValue = new List<B>();
                if (aValue != null)
                {
                    foreach (var aSubItem in aValue)
                    {
                        var bSubItem = new B();
                        ConvertViewModel(aSubItem, bSubItem);
                        bValue.Add(bSubItem);
                    }
                }
                propB.SetValue(output, bValue);
            }
            else
            {
                var value = propA.GetValue(input);
                var originalValue = propA.GetValue(input);
                updated |= value != originalValue;
                propB.SetValue(output, value);
            }
        }

        return updated;
    }
    
    private static readonly Regex _intRegex = new Regex(@"^-?[0-9]+$"); //regex that matches disallowed text
    private static readonly Regex _intCharacters = new Regex("[^0-9]+"); //regex that matches disallowed text
    private static readonly Regex _decimalRegex = new Regex(@"^-?[0-9]+\.?[0-9]*$"); //regex that matches disallowed text
    private static readonly Regex _decimalCharacters = new Regex("[^0-9.-]+"); //regex that matches disallowed text
    private static bool IsDecimalTextAllowed(string text)
    {
        return !_decimalCharacters.IsMatch(text);
    }
    private static bool IsIntTextAllowed(string text)
    {
        return !_intCharacters.IsMatch(text);
    }
    
}