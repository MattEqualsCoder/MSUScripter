using System;

namespace MSUScripter.Configs;

public class MsuSongInfo
{
    public int TrackNumber { get; set; }
    public string? TrackName { get; set; }
    public string? SongName { get; set; }
    public string? Artist { get; set; } 
    public string? Album { get; set; }
    public string? Url { get; set; }
    public string? OutputPath { get; set; } = "";
    public bool IsAlt { get; set; }
    public bool IsComplete { get; set; }
    public bool CheckCopyright { get; set; } = true;
    public bool ShowPanel { get; set; } = true;
    public DateTime LastModifiedDate { get; set; }
    public DateTime LastGeneratedDate { get; set; }

    public MsuSongMsuPcmInfo MsuPcmInfo { get; set; } = new();
}