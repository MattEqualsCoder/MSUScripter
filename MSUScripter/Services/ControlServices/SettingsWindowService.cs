using System;
using Avalonia;
using Avalonia.Styling;
using AvaloniaControls.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class SettingsWindowService(SettingsService settingsService, ConverterService converterService, MsuPcmService msuPcmService, PyMusicLooperService pyMusicLooperService) : ControlService
{
    private SettingsWindowViewModel _model = new();

    public SettingsWindowViewModel InitializeModel()
    {
        converterService.ConvertViewModel(settingsService.Settings, _model);
        _model.CanSetPyMusicLooperPath = OperatingSystem.IsWindows();
        return _model;
    }

    public void SaveSettings()
    {
        converterService.ConvertViewModel(_model, settingsService.Settings);
        settingsService.SaveSettings();
        Application.Current!.RequestedThemeVariant = settingsService.Settings.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    public bool ValidateMsuPcm()
    {
        return msuPcmService.ValidateMsuPcmPath(_model.MsuPcmPath!, out _);
    }
    
    public bool ValidatePyMusicLooper()
    {
        return pyMusicLooperService.TestService(out _, _model.PyMusicLooperPath);
    }
}