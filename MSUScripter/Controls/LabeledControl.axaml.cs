using Avalonia;
using Avalonia.Controls;

namespace MSUScripter.Controls;

public class LabeledControl : ContentControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<LabeledControl, string>(
        "Text");

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<string?> HintProperty = AvaloniaProperty.Register<LabeledControl, string?>(
        "Hint");

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public static readonly StyledProperty<bool> DisplayHintProperty = AvaloniaProperty.Register<LabeledControl, bool>(
        "DisplayHint");

    public bool DisplayHint
    {
        get => GetValue(DisplayHintProperty);
        set => SetValue(DisplayHintProperty, value);
    }
}