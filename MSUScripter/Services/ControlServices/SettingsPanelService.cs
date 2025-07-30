using AvaloniaControls.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class SettingsPanelService (SettingsService settingsService) : ControlService
{
    private readonly SettingsPanelViewModel _viewModel = new();
    
    public SettingsPanelViewModel GetViewModel()
    {
        return _viewModel;
    }

    public void UpdateViewModel()
    {
        var settings = settingsService.Settings;
        _viewModel.CheckForUpdates = settings.CheckForUpdates;
        _viewModel.LoopDuration = settings.LoopDuration;
        _viewModel.DefaultSongPanel = settings.DefaultSongPanel;
        _viewModel.UiScaling = settings.UiScaling;
        _viewModel.HideSubTracksSubChannelsWarning = settings.HideSubTracksSubChannelsWarning;
        _viewModel.AutomaticallyRunPyMusicLooper = settings.AutomaticallyRunPyMusicLooper;
        _viewModel.RunMsuPcmWithKeepTemps = settings.RunMsuPcmWithKeepTemps;
    }

    public void SaveSettings()
    {
        var settings = settingsService.Settings;
        settings.CheckForUpdates = _viewModel.CheckForUpdates;
        settings.LoopDuration = _viewModel.LoopDuration;
        settings.DefaultSongPanel = _viewModel.DefaultSongPanel;
        settings.UiScaling = _viewModel.UiScaling;
        settings.HideSubTracksSubChannelsWarning = _viewModel.HideSubTracksSubChannelsWarning;
        settings.AutomaticallyRunPyMusicLooper = _viewModel.AutomaticallyRunPyMusicLooper;
        settings.RunMsuPcmWithKeepTemps = _viewModel.RunMsuPcmWithKeepTemps;
        settingsService.SaveSettings();
    }
}