using System;
using System.Collections.Generic;
using AvaloniaControls.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class AudioAnalysisViewModel : ViewModelBase
{
    public MsuProjectViewModel Project { get; set; } = new();
    
    [Reactive] public int SongsCompleted { get; set; }

    [Reactive] public string BottomBar { get; set; } = "";
    
    [Reactive, ReactiveLinkedProperties(nameof(TotalSongs))]
    public List<AudioAnalysisSongViewModel> Rows { get; set; } = [];
    
    public double Duration { get; set; }
    public int TotalSongs => Rows.Count;
    
    public override ViewModelBase DesignerExample()
    {
        SongsCompleted = 2;
        BottomBar = "Test Message";
        Rows =
        [
            new AudioAnalysisSongViewModel()
            {
                SongName = "Test Song Name",
                TrackNumber = 1,
                TrackName = "Track #1",
                AvgDecibals = -20,
                MaxDecibals = -10,
                HasLoaded = true
            },
            new AudioAnalysisSongViewModel()
            {
                SongName = "Test Song Name 2",
                TrackNumber = 2,
                TrackName = "Track #2",
                AvgDecibals = -21.25,
                MaxDecibals = -22,
                WarningMessage = "Could not fully load",
                HasLoaded = true
            },
            new AudioAnalysisSongViewModel()
            {
                SongName = "Test Song Name 3",
                TrackNumber = 3,
                TrackName = "Track #3",
                HasLoaded = false
            }
        ];
        return this;
    }
}