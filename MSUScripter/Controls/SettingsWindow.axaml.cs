using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using MSUScripter.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class SettingsWindow : Window
{
    private readonly SettingsService? _settingsService;
    private readonly ConverterService? _converterService;
    private readonly SettingsViewModel _model = new();

    public SettingsWindow() : this(null, null)
    {
        
    }
    
    public SettingsWindow(SettingsService? settingsService, ConverterService? converterService)
    {
        _settingsService = settingsService;
        _converterService = converterService;
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
}