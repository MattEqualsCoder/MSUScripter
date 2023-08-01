using System.Collections.Generic;

namespace MSUScripter.Configs;

public class Settings
{
    public string? MsuPcmPath { get; set; }
    public bool PromptOnUpdate { get; set; } = true;
    public bool PromptOnPreRelease { get; set; }
    public ICollection<RecentProject> RecentProjects { get; set; } = new List<RecentProject>();
    public double Volume { get; set; } = 1;
}