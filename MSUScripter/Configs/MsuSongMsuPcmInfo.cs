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
    
    [Description("Whether or not to apply audio dither to the final output.")]
    public bool? Dither { get; set; }
    
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

    public bool AreFilesValid()
    {
        return GetFiles().All(System.IO.File.Exists);
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

    public bool HasData()
    {
        return Loop > 0 || TrimStart > 0 || TrimEnd > 0 || FadeIn > 0 || FadeOut > 0 || CrossFade > 0 || PadStart > 0 ||
               PadEnd > 0 || (Tempo.HasValue && Tempo != 0) || (Normalization.HasValue && Normalization != 0) ||
               !string.IsNullOrEmpty(File) || SubChannels.Count > 0 || SubTracks.Count > 0;
    }

    public bool HasAdvancedData()
    {
        return FadeIn > 0 || FadeOut > 0 || CrossFade > 0 || PadStart > 0 || PadEnd > 0 ||
               (Tempo.HasValue && Tempo != 0) || SubChannels.Count > 0 || SubTracks.Count > 0;
    }

    public bool HasFiles()
    {
        return GetFiles().Count > 0;
    }

    public int MoveSubInfo(MsuSongMsuPcmInfo info, bool toSubTrack, int index, MsuSongMsuPcmInfo? previousParent)
    {
        var destination = toSubTrack ? SubTracks : SubChannels;

        if (destination.Contains(info))
        {
            var currentIndex = destination.IndexOf(info);
            if (index > currentIndex)
            {
                index--;
            }
        }
        
        previousParent?.SubTracks.Remove(info);
        previousParent?.SubChannels.Remove(info);
        
        if (index > destination.Count)
        {
            destination.Add(info);
        }
        else
        {
            destination.Insert(index, info);
        }

        return index;
    }
    
    public bool HasChangesSince(DateTime time)
    {
        if (SubTracks.Any(x => x.HasChangesSince(time)))
            return true;
        if (SubChannels.Any(x => x.HasChangesSince(time)))
            return true;
        return LastModifiedDate > time;
    }
}