using System.Collections.Generic;

namespace MSUScripter.Configs;

public class MsuTrackInfo
{
    public required int TrackNumber { get; set; }
    public required string TrackName { get; set; }
    public ICollection<MsuSongInfo> Songs { get; set; } = new List<MsuSongInfo>();
}