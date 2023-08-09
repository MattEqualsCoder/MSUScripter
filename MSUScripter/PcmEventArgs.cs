using System;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter;

public class PcmEventArgs : EventArgs
{
    public MsuSongInfoViewModel Song { get; set; }
    public PcmEventType Type { get; set; }

    public PcmEventArgs(MsuSongInfoViewModel song, PcmEventType type = PcmEventType.Play)
    {
        Song = song;
        Type = type;
    }
}