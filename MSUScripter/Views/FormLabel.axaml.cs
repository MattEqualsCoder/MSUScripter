using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MSUScripter.Views;

public partial class FormLabel : UserControl
{
    public FormLabel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (ToolTipText.Length > ToolTipCharacterLimit)
        {
            AbbreviatedToolTip = string.Concat(ToolTipText.AsSpan(0, ToolTipCharacterLimit - 3), "...");
        }
        else
        {
            AbbreviatedToolTip = ToolTipText;
        }
    }

    public static readonly StyledProperty<string> LabelTextProperty = AvaloniaProperty.Register<FormLabel, string>(
        nameof(LabelText), defaultValue: "Label");

    public string LabelText
    {
        get => GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }
    
    public static readonly StyledProperty<string> ToolTipTextProperty = AvaloniaProperty.Register<FormLabel, string>(
        nameof(ToolTipText), defaultValue: "Label ToolTip");

    public string ToolTipText
    {
        get => GetValue(ToolTipTextProperty);
        set
        {
            SetValue(ToolTipTextProperty, value);
            if (value.Length > ToolTipCharacterLimit)
            {
                SetValue(AbbreviatedToolTipProperty, string.Concat(value.AsSpan(0, ToolTipCharacterLimit - 3), "..."));
            }
            else
            {
                SetValue(AbbreviatedToolTipProperty, value);
            }
        }
    }
    
    public static readonly StyledProperty<bool> DisplayToolTipIconProperty = AvaloniaProperty.Register<FormLabel, bool>(
        nameof(DisplayToolTipIcon), defaultValue: true);

    public bool DisplayToolTipIcon
    {
        get => GetValue(DisplayToolTipIconProperty);
        set => SetValue(DisplayToolTipIconProperty, value);
    }
    
    public static readonly StyledProperty<bool> CanClickToolTipIconProperty = AvaloniaProperty.Register<FormLabel, bool>(
        nameof(CanClickToolTipIcon), defaultValue: true);

    public bool CanClickToolTipIcon
    {
        get => GetValue(CanClickToolTipIconProperty);
        set => SetValue(CanClickToolTipIconProperty, value);
    }
    
    public static readonly StyledProperty<string> AbbreviatedToolTipProperty = AvaloniaProperty.Register<FormLabel, string>(
        nameof(AbbreviatedToolTip), defaultValue: "Label ToolTip");

    public string AbbreviatedToolTip
    {
        get => GetValue(AbbreviatedToolTipProperty);
        set => SetValue(AbbreviatedToolTipProperty, value);
    }
    
    public static readonly StyledProperty<int> ToolTipCharacterLimitProperty = AvaloniaProperty.Register<FormLabel, int>(
        nameof(ToolTipCharacterLimit), defaultValue: 100);

    public int ToolTipCharacterLimit
    {
        get => GetValue(ToolTipCharacterLimitProperty);
        set => SetValue(ToolTipCharacterLimitProperty, value);
    }
}