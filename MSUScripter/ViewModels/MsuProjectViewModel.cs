using System;
using System.Collections.Generic;
using System.Linq;
using MSUScripter.Tools;

namespace MSUScripter.ViewModels;

public class MsuProjectViewModel
{
    public string ProjectFilePath { get; set; } = "";
    public string BackupFilePath { get; set; } = "";
    public string MsuPath { get; set; } = "";
    public string MsuTypeName { get; set; } = "";
    public DateTime LastSaveTime { get; set; }
    [SkipConvert]
    public MsuBasicInfoViewModel BasicInfo { get; set; } = new();
    [SkipConvert]
    public List<MsuTrackInfoViewModel> Tracks { get; set; } = new();
    
    public bool HasPendingChanges()
    {
        return HasChangesSince(LastSaveTime);
    }
    
    public bool HasChangesSince(DateTime time)
    {
        return BasicInfo.HasChangesSince(time) || Tracks.Any(x => x.HasChangesSince(time));
    }
}