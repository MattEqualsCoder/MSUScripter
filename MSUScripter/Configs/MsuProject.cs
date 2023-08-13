using System;
using System.Collections.Generic;
using MSURandomizerLibrary.Configs;
using MSUScripter.Tools;
using YamlDotNet.Serialization;

namespace MSUScripter.Configs;

public class MsuProject
{
    public string ProjectFilePath { get; set; } = "";
    public string BackupFilePath { get; set; } = "";
    public string MsuPath { get; set; } = "";
    public string MsuTypeName { get; set; } = "";
    public DateTime LastSaveTime { get; set; }
    [YamlIgnore, SkipConvert]
    public MsuType MsuType { get; set; } = null!;
    [SkipConvert]
    public MsuBasicInfo BasicInfo { get; set; } = new();
    [SkipConvert]
    public List<MsuTrackInfo> Tracks { get; set; } = new();
}