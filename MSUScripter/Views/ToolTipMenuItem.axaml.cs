using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using AvaloniaControls.Controls;

namespace MSUScripter.Views;

public partial class ToolTipMenuItem : IconMenuItem
{
    protected override Type StyleKeyOverride => typeof(MenuItem);
    
    public ToolTipMenuItem()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Update();
    }
    
    public static readonly StyledProperty<string> LabelTextProperty = AvaloniaProperty.Register<ToolTipMenuItem, string>(
        nameof(LabelText), defaultValue: "Label");

    public string LabelText
    {
        get => GetValue(LabelTextProperty);
        set
        {
            SetValue(LabelTextProperty, value);
            Update();
        }
    }
    
    public static readonly StyledProperty<string> ToolTipTextProperty = AvaloniaProperty.Register<ToolTipMenuItem, string>(
        nameof(ToolTipText), defaultValue: "Label ToolTip");

    public string ToolTipText
    {
        get => GetValue(ToolTipTextProperty);
        set
        {
            SetValue(ToolTipTextProperty, value);
            Update();
        }
    }

    private void Update()
    {
        var label = this.Get<FormLabel>(nameof(LabelControl));
        label.LabelText = LabelText;
        label.ToolTipText = ToolTipText;
    }
}