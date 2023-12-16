using System;

namespace MSUScripter.Tools;

public class TrackEventArgs : EventArgs
{
    public int TrackNumber { get; set; }

    public TrackEventArgs(int trackNumber)
    {
        TrackNumber = trackNumber;
    }
}