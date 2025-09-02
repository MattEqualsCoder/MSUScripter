using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaControls;
using AvaloniaControls.Controls;
using MSUScripter.Models;

namespace MSUScripter.Views;

public partial class AboutPanel : UserControl
{
    public AboutPanel()
    {
        InitializeComponent();
    }

    private void LinkControlButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not LinkControl control)
        {
            return;
        }

        var url = ToolTip.GetTip(control) as string;

        if (!string.IsNullOrEmpty(url))
        {
            CrossPlatformTools.OpenUrl(url);
        }
        else
        {
            CrossPlatformTools.OpenDirectory(Directories.LogFolder);
        }
    }
}