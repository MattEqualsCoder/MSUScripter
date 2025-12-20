using System;
using MSUScripter.Configs;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public enum DefaultSongPanel
{
    Prompt,
    Basic,
    Advanced,
}

public partial class SettingsPanelViewModel : SavableViewModelBase
{
    [Reactive] public partial bool CheckForUpdates { get; set; }
    
    [Reactive] public partial DefaultSongPanel DefaultSongPanel { get; set; }
    
    [Reactive] public partial int LoopDuration { get; set; }
    [Reactive] public partial decimal UiScaling { get; set; }
    [Reactive] public partial bool HideSubTracksSubChannelsWarning { get; private set; }
    
    [Reactive] public partial bool AutomaticallyRunPyMusicLooper { get; set; }
    
    [Reactive] public partial bool RunMsuPcmWithKeepTemps { get; set; }
    public bool ShowDesktopFileButton => OperatingSystem.IsLinux();

    public Settings Settings { get; private set; } = new();

    public SettingsPanelViewModel()
    {
        LoopDuration = 5;
        UiScaling = 1;
    }
    
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