using System;

namespace MSUScripter.Configs;

public class MsuBasicInfo
{
    public string MsuType { get; set; } = "";
    public string Game { get; set; } = "";
    public string? PackName { get; set; } = "";
    public string? PackCreator { get; set; }
    public string? PackVersion { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Url { get; set; }
    public double? Normalization { get; set; }
    public bool? Dither { get; set; }
    public bool IsMsuPcmProject { get; set; } = true;
    public bool CreateAltSwapperScript { get; set; } = true;
    public bool CreateSplitSmz3Script { get; set; }
    public bool WriteTrackList { get; set; } = true;
    public bool WriteYamlFile { get; set; } = true;
    public string? ZeldaMsuPath { get; set; }
    public string? MetroidMsuPath { get; set; }
    public bool IsSmz3Project { get; set; }
    public DateTime LastModifiedDate { get; set; }
}