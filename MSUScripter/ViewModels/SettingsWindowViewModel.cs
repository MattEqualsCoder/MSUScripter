using System.Collections.Generic;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    [Reactive] public string? MsuPcmPath { get; set; }
    [Reactive] public bool PromptOnUpdate { get; set; }
    [Reactive] public bool PromptOnPreRelease { get; set; }
    [Reactive] public bool DarkTheme { get; set; }
    [Reactive] public int LoopDuration { get; set; } = 5;
    [Reactive] public decimal UiScaling { get; set; }
    [Reactive] public ICollection<RecentProject> RecentProjects { get; set; } = [];
    [Reactive] public double Volume { get; set; }
    [Reactive] public string? PreviousPath { get; set; }
    [Reactive] public bool RunMsuPcmWithKeepTemps { get; set; }
    [Reactive] public bool AutomaticallyRunPyMusicLooper { get; set; }
    [Reactive] public bool HideSubTracksSubChannelsWarning { get; set; }
    public bool HasDoneFirstTimeSetup { get; set; }

    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}