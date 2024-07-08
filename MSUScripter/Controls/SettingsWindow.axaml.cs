using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using AvaloniaControls.Controls;
using AvaloniaControls.Models;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class SettingsWindow : ScalableWindow
{
    private readonly SettingsService? _settingsService;
    private readonly ConverterService? _converterService;
    private readonly SettingsViewModel _model = new();
    private readonly MsuPcmService? _msuPcmService;

    public SettingsWindow() : this(null, null, null)
    {
        
    }
    
    public SettingsWindow(SettingsService? settingsService, ConverterService? converterService, MsuPcmService? msuPcmService)
    {
        _settingsService = settingsService;
        _converterService = converterService;
        _msuPcmService = msuPcmService;
        InitializeComponent();
        if (_converterService == null || _settingsService == null) return;
        _converterService.ConvertViewModel(_settingsService.Settings, _model);
        DataContext = _model;
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_converterService == null || _settingsService == null) return;
        _converterService.ConvertViewModel(_model, _settingsService.Settings);
        _settingsService.SaveSettings();
        Application.Current!.RequestedThemeVariant = _settingsService.Settings.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        Close();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void ValidateMsuPcmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var isSuccessful = _msuPcmService?.ValidateMsuPcmPath(_model.MsuPcmPath!, out var error);
        if (isSuccessful != true)
        {
            await new MessageWindow(new MessageWindowRequest
            {
                Message = "There was an error verifying msupcm++. Please verify that the application runs independently.",
                Icon = MessageWindowIcon.Error,
                Buttons = MessageWindowButtons.OK,
            }).ShowDialog(this);
        }
        else
        {
            await new MessageWindow(new MessageWindowRequest
            {
                Message = "msupcm++ verification successful!",
                Icon = MessageWindowIcon.Info,
                Buttons = MessageWindowButtons.OK,
                Title = "Success"
            }).ShowDialog(this);
        }
    }
}