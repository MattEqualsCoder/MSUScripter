using System.Collections.Generic;
using System.Linq;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public partial class AudioAnalysisViewModel : TranslatedViewModelBase
{
    public MsuProject? Project { get; set; }

    public int SongsCompleted => Rows.Count(x => x.HasLoaded);

    [Reactive] public partial string BottomBar { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(TotalSongs))]
    public partial List<AudioAnalysisSongViewModel> Rows { get; set; }

    [Reactive] public partial bool CompareEnabled { get; set; }
    
    public double Duration { get; set; }
    public int TotalSongs => Rows.Count;
    public string? LoadError { get; set; }
    public bool ShowCompareButton { get; set; } = true;

    public AudioAnalysisViewModel()
    {
        Project = new MsuProject();
        BottomBar = string.Empty;
        Rows = [];
        CompareEnabled = true;
    }
    
    public void UpdateSongsCompleted()
    {
        this.RaisePropertyChanged(nameof(SongsCompleted));
    }
    
    public override ViewModelBase DesignerExample()
    {
        BottomBar = "Test Message";
        Rows =
        [
            new AudioAnalysisSongViewModel()
            {
                SongName = "Test Song Name",
                TrackNumber = 1,
                TrackName = "Track #1",
                AvgDecibels = -20,
                MaxDecibels = -10,
                HasLoaded = true
            },
            new AudioAnalysisSongViewModel()
            {
                SongName = "Test Song Name 2",
                TrackNumber = 2,
                TrackName = "Track #2",
                AvgDecibels = -21.25,
                MaxDecibels = -22,
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