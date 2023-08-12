using System;
using System.Collections.Generic;
using MSUScripter.Tools;

namespace MSUScripter.Configs;

public class MsuTrackInfo
{
    public int TrackNumber { get; set; }
    public string TrackName { get; set; } = "";
    public DateTime LastModifiedDate { get; set; }
    
    [SkipConvert]
    public List<MsuSongInfo> Songs { get; set; } = new List<MsuSongInfo>();
}