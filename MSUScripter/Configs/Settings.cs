using System.Collections.Generic;

namespace MSUScripter.Configs;

public class Settings
{
    public string? MsuPcmPath { get; set; }
    public bool PromptOnUpdate { get; set; } = true;
    public bool PromptOnPreRelease { get; set; }
    public bool DarkTheme { get; set; } = true;
    public int LoopDuration { get; set; } = 5;
    public decimal UiScaling { get; set; } = 1;
    public ICollection<RecentProject> RecentProjects { get; set; } = new List<RecentProject>();
    public double Volume { get; set; } = 1;
    public string? PreviousPath { get; set; }
    public bool HideSubTracksSubChannelsWarning { get; set; }
    public bool AutomaticallyRunPyMusicLooper { get; set; } = true;
    public bool RunMsuPcmWithKeepTemps { get; set; }
    public bool HasDoneFirstTimeSetup { get; set; }
    public string? PyMusicLooperPath { get; set; }
}