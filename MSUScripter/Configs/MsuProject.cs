using System;
using System.Collections.Generic;
using MSURandomizerLibrary.Configs;
using YamlDotNet.Serialization;

namespace MSUScripter.Configs;

public class MsuProject
{
    public string ProjectFilePath { get; set; } = "";
    public string MsuPath { get; set; } = "";
    public string MsuTypeName { get; set; } = "";
    public DateTime LastSaveTime { get; set; }
    [YamlIgnore]
    public MsuType MsuType { get; set; } = null!;
    public MsuBasicInfo BasicInfo { get; set; } = new();
    public List<MsuTrackInfo> Tracks { get; set; } = new();
}