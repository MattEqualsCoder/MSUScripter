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
    
    [Description("Details that are passed to msupcm++ for generation")]
    public MsuSongMsuPcmInfo MsuPcmInfo { get; set; } = new();
    
    [JsonSchemaIgnore]
    public string? OutputPath { get; set; } = "";
    
    [JsonSchemaIgnore]
    public bool IsAlt { get; set; }
    
    [JsonSchemaIgnore]
    public bool IsComplete { get; set; }
    
    [JsonSchemaIgnore]
    public bool CheckCopyright { get; set; } = true;
    
    [JsonSchemaIgnore]
    public bool ShowPanel { get; set; } = true;
    
    [JsonSchemaIgnore]
    public DateTime LastModifiedDate { get; set; }
    
    [JsonSchemaIgnore]
    public DateTime LastGeneratedDate { get; set; }

    
}