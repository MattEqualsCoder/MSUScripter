using System;

namespace MSUScripter.Configs;

public class RecentProject
{
    public required string ProjectName { get; set; }
    public required string ProjectPath { get; set; }
    public DateTime Time { get; set; }
}