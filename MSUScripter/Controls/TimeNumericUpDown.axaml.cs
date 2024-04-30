using System;
using System.Globalization;
using System.Net.Mime;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MSUScripter.Controls;

public partial class TimeNumericUpDown : UserControl
{
    private string? previousText;
        
    public TimeNumericUpDown()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public decimal? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    
    public static readonly StyledProperty<decimal?> ValueProperty =
        AvaloniaProperty.Register<TimeNumericUpDown, decimal?>(nameof(Value), 
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    
    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
    
    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<TimeNumericUpDown, string?>(nameof(Watermark), 
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

    private void NumericUpDownControl_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is not NumericUpDown control)
        {
            return;
        }
        previousText = control.Text;
    }

    private void NumericUpDownControl_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not NumericUpDown control)
        {
            return;
        }

        if (previousText?.Contains(':') != true)
        {
            return;
        }

        try
        {
            if (previousText.Length == 5)
            {
                control.Value = (int)TimeSpan.ParseExact(previousText, "mm\\:ss", CultureInfo.InvariantCulture).TotalSeconds;
            }
            else if (previousText.Length == 4)
            {
                control.Value = (int)TimeSpan.ParseExact(previousText, "m\\:ss", CultureInfo.InvariantCulture).TotalSeconds;
            }
        }
        catch
        {
            // Do nothing
        }
        
    }
}