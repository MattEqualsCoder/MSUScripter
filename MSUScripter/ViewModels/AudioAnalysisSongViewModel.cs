using AvaloniaControls.Models;
using MSUScripter.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class AudioAnalysisSongViewModel : ViewModelBase
{
    public MsuSongInfoViewModel? OriginalViewModel { get; set; }
        
    [Reactive] public string SongName { get; set; } = "";
    
    [Reactive] public int TrackNumber { get; set; }

    [Reactive] public string TrackName { get; set; } = "";

    [Reactive] public string Path { get; set; } = "";
    
    [Reactive] public double? AvgDecibels { get; set; }
    
    [Reactive] public double? MaxDecibels { get; set; }

    [Reactive] public bool HasLoaded { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(HasWarning))]
    public string WarningMessage { get; set; } = "";

    public bool CanRefresh { get; set; } = true;
    public bool HasWarning => !string.IsNullOrEmpty(WarningMessage);

    public void ApplyAudioAnalysis(AnalysisDataOutput data)
    {
        AvgDecibels = data.AvgDecibels;
        MaxDecibels = data.MaxDecibels;
        HasLoaded = true;
    }

    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}