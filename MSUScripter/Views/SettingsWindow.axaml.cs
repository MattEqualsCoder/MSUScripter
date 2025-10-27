using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class SettingsWindow : ScalableWindow
{
    private readonly SettingsWindowService? _service;

    public SettingsWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = new SettingsWindowViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<SettingsWindowService>();
            DataContext = _service?.InitializeModel();
        }
    }
    
    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveSettings();
        Close();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}