using System;
using System.ComponentModel;
using NJsonSchema.Annotations;

namespace MSUScripter.Configs;

[Description("Details about a song for YAML and PCM generation")]
public class MsuSongInfo
{
    [JsonSchemaIgnore]
    public int TrackNumber { get; set; }
    
    [JsonSchemaIgnore]
    public string? TrackName { get; set; }
    
    [Description("The title of the song")]
    public string? SongName { get; set; }
    
    [Description("The artist(s) that created the song")]
    public string? Artist { get; set; } 
    
    [Description("The album in which the song was released on or the game the song is from")]
    public string? Album { get; set; }
    
    [Description("A url in which the user can purchase the song/album")]
    public string? Url { get; set; }
    
    [Description("If the song should be added to the video to upload to YouTube to check for copyright strikes")]
    public bool? CheckCopyright { get; set; } = true;
    
    [Description("If the song has been tested and shown to be safe from copyright strikes in VODs")]
    public bool? IsCopyrightSafe { get; set; }
    
    [Description("Details that are passed to msupcm++ for generation")]
    public MsuSongMsuPcmInfo MsuPcmInfo { get; set; } = new();
    
    [JsonSchemaIgnore]
    public string? OutputPath { get; set; } = "";
    
    [JsonSchemaIgnore]
    public bool IsAlt { get; set; }
    
    [JsonSchemaIgnore]
    public bool IsComplete { get; set; }
    
    [JsonSchemaIgnore]
    public bool ShowPanel { get; set; } = true;
    
    [JsonSchemaIgnore]
    public DateTime LastModifiedDate { get; set; }
    
    [JsonSchemaIgnore]
    public DateTime LastGeneratedDate { get; set; }

    
}