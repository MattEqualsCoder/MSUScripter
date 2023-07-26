using System.Windows;
using System.Windows.Controls;

namespace MSUScripter.UI;

public class LabeledControl : ContentControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text),
            propertyType: typeof(string),
            ownerType: typeof(LabeledControl),
            typeMetadata: new PropertyMetadata("Label"));
    
    public static readonly DependencyProperty HintProperty =
        DependencyProperty.Register(nameof(Hint),
            propertyType: typeof(string),
            ownerType: typeof(LabeledControl),
            typeMetadata: new PropertyMetadata(""));
    
    public static readonly DependencyProperty HintVisibilityProperty =
        DependencyProperty.Register(nameof(HintVisibility),
            propertyType: typeof(Visibility),
            ownerType: typeof(LabeledControl),
            typeMetadata: new PropertyMetadata(Visibility.Collapsed));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public Visibility HintVisibility
    {
        get => (Visibility)GetValue(HintVisibilityProperty);
        set => SetValue(HintVisibilityProperty, value);
    }
}