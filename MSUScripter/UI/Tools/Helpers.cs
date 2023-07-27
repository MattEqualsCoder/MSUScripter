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