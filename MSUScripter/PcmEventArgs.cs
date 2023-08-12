﻿using System;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter;

public class PcmEventArgs : EventArgs
{
    public MsuSongInfoViewModel Song { get; set; }
    public PcmEventType Type { get; set; }
    public MsuSongMsuPcmInfoViewModel? PcmInfo { get; set; }

    public PcmEventArgs(MsuSongInfoViewModel song, PcmEventType type = PcmEventType.Play, MsuSongMsuPcmInfoViewModel? ppmInfo = null)
    {
        Song = song;
        Type = type;
        PcmInfo = ppmInfo;
    }
}