using System.Collections.Generic;
using MSUScripter.Models;

namespace MSUScripter.Configs;

public class Settings
{
    public string? MsuPcmPath { get; set; }
    public bool PromptOnUpdate { get; set; } = true;
    public bool PromptOnPreRelease { get; set; }
    public bool DarkTheme { get; set; } = true;
    public ICollection<RecentProject> RecentProjects { get; set; } = new List<RecentProject>();
    public double Volume { get; set; } = 1;
    public string? PreviousPath { get; set; }
    public WindowRestoreDetails? MainWindowRestoreDetails { get; set; }
}