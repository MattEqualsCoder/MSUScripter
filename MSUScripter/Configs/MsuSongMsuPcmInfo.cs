using System.Collections.Generic;

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
    public string? Output { get; set; }
    public string? File { get; set; }
    
    public List<MsuSongMsuPcmInfo> SubTracks { get; set; } = new();
    public List<MsuSongMsuPcmInfo> SubChannels { get; set; } = new();
}