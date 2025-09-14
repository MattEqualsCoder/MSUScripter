using System;
using System.Collections.Generic;
using System.IO;
using MSURandomizerLibrary.Configs;
using MSUScripter.Models;
using YamlDotNet.Serialization;

namespace MSUScripter.Configs;

public class MsuProject
{
    public string Id { get; set; } = "";
    public string ProjectFilePath { get; set; } = "";
    public string BackupFilePath { get; set; } = "";
    public string MsuPath { get; set; } = "";
    public string MsuTypeName { get; set; } = "";
    public DateTime LastSaveTime { get; set; }
    public List<string> IgnoreWarnings { get; set; } = [];
    [YamlIgnore, SkipConvert]
    public MsuType MsuType { get; set; } = null!;
    [SkipConvert]
    public MsuBasicInfo BasicInfo { get; init; } = new();
    [SkipConvert]
    public List<MsuTrackInfo> Tracks { get; set; } = [];
    public Dictionary<string, FileSampleInfo> SampleRates { get; set; } = [];
    [YamlIgnore, SkipConvert]
    public bool IsNewProject { get; set; }

    public string GetMsuGenerationCacheFilePath()
    {
        return Path.Combine(Directories.CacheFolder, "Generation", $"{Id}.yml");
    }
    
    public string GetMsuGenerationTempFilePath(MsuSongInfo? song = null)
    {
        return song == null
            ? Path.Combine(Directories.TempFolder, "Generation", Id, Guid.NewGuid().ToString("N"))
            : Path.Combine(Directories.TempFolder, "Generation", Id, song.Id);
    }
    
    public string GetYamlPath()
    {
        return Path.ChangeExtension(MsuPath, ".yml");
    }
    
    public string GetMetroidMsuPath()
    {
        return BasicInfo.MetroidMsuPath ?? "";
    }
    
    public string GetZeldaMsuPath()
    {
        return BasicInfo.ZeldaMsuPath ?? "";
    }
    
    public string GetMetroidMsuYamlPath()
    {
        return Path.ChangeExtension(GetMetroidMsuPath(), ".yml");
    }
    
    public string GetZeldaMsuYamlPath()
    {
        return Path.ChangeExtension(GetZeldaMsuPath(), ".yml");
    }

    public string GetTracksJsonPath()
    {
        return Path.ChangeExtension(MsuPath, "-tracks.json");
    }
    
    public string GetTracksTextPath()
    {
        var msuFileInfo = new FileInfo(MsuPath);
        return Path.Combine(msuFileInfo.DirectoryName!, "Track List.txt");
    }
    
    public string GetAltSwapperPath()
    {
        var fileInfo = new FileInfo(MsuPath);
        return Path.Combine(fileInfo.DirectoryName ?? "", "!Swap_Alt_Tracks.bat");
    }
    
    public string GetSmz3SwapperPath()
    {
        var fileInfo = new FileInfo(MsuPath);
        return Path.Combine(fileInfo.DirectoryName ?? "", "!Split_Or_Combine_SMZ3_ALttP_SM_MSUs.bat");
    }

    [YamlIgnore, SkipConvert] public MsuProjectGenerationCache GenerationCache { get; set; } = new();
}

public class FileSampleInfo
{
    public long FileLength { get; init; }
    public int SampleRate { get; init; }
}