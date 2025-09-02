using System;
using Avalonia;
using Avalonia.Styling;
using AvaloniaControls.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class SettingsWindowService(SettingsService settingsService) : ControlService
{
    private readonly SettingsWindowViewModel _model = new();

    public SettingsWindowViewModel InitializeModel()
    {
        _model.SettingsPanelViewModel.LoadSettings(settingsService.Settings);
        return _model;
    }

    public void SaveSettings()
    {
        _model.SettingsPanelViewModel.SaveChanges();
        settingsService.SaveSettings();
    }
}