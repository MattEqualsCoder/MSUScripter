using AvaloniaControls.Models;
using MSUScripter.Configs;
using MSUScripter.Models;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public partial class AudioAnalysisSongViewModel : ViewModelBase
{
    public MsuSongInfo? MsuSongInfo { get; init; }
        
    [Reactive] public partial string SongName { get; set; }
    
    [Reactive] public partial int TrackNumber { get; set; }

    [Reactive] public partial string TrackName { get; set; }

    [Reactive] public partial string Path { get; set; }
    
    [Reactive] public partial double? AvgDecibels { get; set; }
    
    [Reactive] public partial double? MaxDecibels { get; set; }

    [Reactive] public partial bool HasLoaded { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(HasWarning))]
    public partial string WarningMessage { get; set; }

    public bool CanRefresh { get; set; } = true;
    public bool HasWarning => !string.IsNullOrEmpty(WarningMessage);

    public AudioAnalysisSongViewModel()
    {
        MsuSongInfo = new();
        SongName = string.Empty;
        TrackName = string.Empty;
        Path = string.Empty;
        WarningMessage = string.Empty;
    }

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