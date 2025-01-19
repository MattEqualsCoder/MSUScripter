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

    private async void ValidateMsuPcmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var isSuccessful = _service?.ValidateMsuPcm();
        if (isSuccessful != true)
        {
            await MessageWindow.ShowErrorDialog(
                "There was an error verifying msupcm++. Please verify that the application runs independently.",
                "msupcm++ Error", this);
        }
        else
        {
            await MessageWindow.ShowInfoDialog("msupcm++ verification successful!", "Success", this);
        }
    }

    private async void ValidatePyMusicLooper_OnClick(object? sender, RoutedEventArgs e)
    {
        var isSuccessful = _service?.ValidatePyMusicLooper();
        if (isSuccessful != true)
        {
            await MessageWindow.ShowErrorDialog(
                "There was an error verifying PyMusicLooper. Please verify that the application runs independently.",
                "PyMusicLooper Error", this);
        }
        else
        {
            await MessageWindow.ShowInfoDialog("PyMusicLooper verification successful!", "Success", this);
        }
    }
}