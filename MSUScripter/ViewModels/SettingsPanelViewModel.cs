using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public enum DefaultSongPanel
{
    Prompt,
    Basic,
    Advanced,
}

public class SettingsPanelViewModel : SavableViewModelBase
{
    [Reactive] public bool CheckForUpdates { get; set; }
    
    [Reactive] public DefaultSongPanel DefaultSongPanel { get; set; }
    
    [Reactive] public int LoopDuration { get; set; } = 5;
    [Reactive] public decimal UiScaling { get; set; } = 1;
    [Reactive] public bool HideSubTracksSubChannelsWarning { get; set; }
    
    [Reactive] public bool AutomaticallyRunPyMusicLooper { get; set; }
    
    [Reactive] public bool RunMsuPcmWithKeepTemps { get; set; }

    public Settings Settings { get; set; } = new();
     
    public override ViewModelBase DesignerExample()
    {
        return new SettingsPanelViewModel();
    }

    public override void SaveChanges()
    {
        Settings.CheckForUpdates = CheckForUpdates;
        Settings.LoopDuration = LoopDuration;
        Settings.DefaultSongPanel = DefaultSongPanel;
        Settings.UiScaling = UiScaling;
        Settings.HideSubTracksSubChannelsWarning = HideSubTracksSubChannelsWarning;
        Settings.AutomaticallyRunPyMusicLooper = AutomaticallyRunPyMusicLooper;
        Settings.RunMsuPcmWithKeepTemps = RunMsuPcmWithKeepTemps;
    }

    public void LoadSettings(Settings? settings = null)
    {
        if (settings != null)
        {
            Settings = settings;
        }
        CheckForUpdates = Settings.CheckForUpdates;
        LoopDuration = Settings.LoopDuration;
        DefaultSongPanel = Settings.DefaultSongPanel;
        UiScaling = Settings.UiScaling;
        HideSubTracksSubChannelsWarning = Settings.HideSubTracksSubChannelsWarning;
        AutomaticallyRunPyMusicLooper = Settings.AutomaticallyRunPyMusicLooper;
        RunMsuPcmWithKeepTemps = Settings.RunMsuPcmWithKeepTemps;
    }
}