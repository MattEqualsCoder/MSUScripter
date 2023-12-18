using System;

namespace MSUScripter.Models;

public class TrackEventArgs : EventArgs
{
    public int TrackNumber { get; set; }

    public TrackEventArgs(int trackNumber)
    {
        TrackNumber = trackNumber;
    }
}