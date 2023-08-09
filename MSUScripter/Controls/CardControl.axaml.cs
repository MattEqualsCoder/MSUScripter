using Avalonia;
using Avalonia.Controls;

namespace MSUScripter.Controls;

public class CardControl : ContentControl
{
    public static readonly StyledProperty<string> HeaderTextProperty = AvaloniaProperty.Register<CardControl, string>(
        "HeaderText");

    public string HeaderText
    {
        get => GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public static readonly StyledProperty<object?> HeaderButtonsProperty = AvaloniaProperty.Register<CardControl, object?>(
        "HeaderButtons");

    public object? HeaderButtons
    {
        get => GetValue(HeaderButtonsProperty);
        set => SetValue(HeaderButtonsProperty, value);
    }

    public static readonly StyledProperty<bool> DisplayHeaderButtonsProperty = AvaloniaProperty.Register<CardControl, bool>(
        "DisplayHeaderButtons");

    public bool DisplayHeaderButtons
    {
        get => GetValue(DisplayHeaderButtonsProperty);
        set => SetValue(DisplayHeaderButtonsProperty, value);
    }

}