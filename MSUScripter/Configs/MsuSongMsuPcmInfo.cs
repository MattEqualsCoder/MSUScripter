using System;
using System.Collections.Generic;
using System.Linq;

namespace MSUScripter.Configs;

public class MsuSongMsuPcmInfo
{
    public int? Loop { get; set; }
    public int? TrimStart { get; set; }
    public int? TrimEnd { get; set; }
    public int? FadeIn { get; set; }
    public int? FadeOut { get; set; }
    public int? CrossFade { get; set; }
    public int? PadStart { get; set; }
    public int? PadEnd { get; set; }
    public double? Tempo { get; set; }
    public double? Normalization { get; set; }
    public bool? Compression { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? Output { get; set; }
    public string? File { get; set; }
    
    public List<MsuSongMsuPcmInfo> SubTracks { get; set; } = new();
    public List<MsuSongMsuPcmInfo> SubChannels { get; set; } = new();

    public List<string> GetFiles()
    {
        List<string> files = new List<string>();

        if (!string.IsNullOrEmpty(File))
        {
            files.Add(File);
        }
        
        files.AddRange(SubTracks.SelectMany(x => x.GetFiles()));
        files.AddRange(SubChannels.SelectMany(x => x.GetFiles()));

        return files;
    }

    public bool HasBothSubTracksAndSubChannels
    {
        get
        {
            return (SubTracks.Count > 0 && SubChannels.Count > 0) ||
                   SubChannels.Any(x => x.HasBothSubTracksAndSubChannels) ||
                   SubTracks.Any(x => x.HasBothSubTracksAndSubChannels);
        }
    }
}