using System;
using MSUScripter.ViewModels;

namespace MSUScripter;

public class SongFileEventArgs : EventArgs
{
    public SongFileEventArgs(MsuSongInfoViewModel songViewModel, string filePath, bool force)
    {
        SongViewModel = songViewModel;
        FilePath = filePath;
        Force = force;
    }

    public MsuSongInfoViewModel SongViewModel { get; set; }
    public string FilePath { get; set; }
    public bool Force { get; set; }
    
}