using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NJsonSchema.Annotations;
using YamlDotNet.Serialization;

namespace MSUScripter.Configs;

[Description("Details that are passed to msupcm++ for generation")]
public class MsuSongMsuPcmInfo
{
    [Description("The loop point of the current track, relative to this track/sub-track/sub-channel, in samples")]
    public int? Loop { get; set; }
    
    [Description("Trim the start of the current track at the specified sample")]
    public int? TrimStart { get; set; }
    
    [Description("Trim the end of the current track at the specified sample")]
    public int? TrimEnd { get; set; }
    
    [Description("Apply a fade in effect to the current track lasting a specified number of samples")]
    public int? FadeIn { get; set; }
    
    [Description("Apply a fade out effect to the current track lasting a specified number of samples")]
    public int? FadeOut { get; set; }
    
    [Description("Apply a cross fade effect from the end of the current track to its loop point lasting a specified number of samples")]
    public int? CrossFade { get; set; }
    
    [Description("Pad the beginning of the current track with a specified number of silent samples")]
    public int? PadStart { get; set; }
    
    [Description("Pad the end of the current track with a specified number of silent samples")]
    public int? PadEnd { get; set; }
    
    [Description("Alter the tempo of the current track by a specified ratio")]
    public double? Tempo { get; set; }
    
    [Description("Normalize the current track to the specified RMS level, overrides the global normalization value")]
    public double? Normalization { get; set; }
    
    [Description("Apply dynamic range compression to the current track")]
    public bool? Compression { get; set; }
    
    [JsonSchemaIgnore]
    public DateTime LastModifiedDate { get; set; }
    
    [JsonSchemaIgnore]
    public string? Output { get; set; }
    
    [Description("The file to be used as the input for this track/sub-track/sub-channel")]
    public string? File { get; set; }
    
    [JsonSchemaIgnore]
    public bool ShowPanel { get; set; } = true;
    
    [Description("Files which will be concatenated together to form the input to the parent track")]
    public List<MsuSongMsuPcmInfo> SubTracks { get; set; } = new();
    
    [Description("Files which will be mixed together to form the input to the parent track")]
    public List<MsuSongMsuPcmInfo> SubChannels { get; set; } = new();

    public void ClearFieldsForYaml()
    {
        LastModifiedDate = new DateTime();
        ShowPanel = false;
        foreach (var subItem in SubChannels.Concat(SubTracks))
        {
            subItem.ClearFieldsForYaml();
        }
    }

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

    [YamlIgnore]
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