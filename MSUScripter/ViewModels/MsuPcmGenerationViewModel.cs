using System.Collections.Generic;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MsuPcmGenerationViewModel : ViewModelBase
{
    [Reactive] public MsuProjectViewModel MsuProjectViewModel { get; set; } = new();
    [Reactive] public MsuProject MsuProject { get; set; } = new();
    [Reactive] public List<MsuPcmGenerationSongViewModel> Rows { get; set; } = [];
    [Reactive] public int TotalSongs { get; set; }
    [Reactive] public int SongsCompleted { get; set; }
    [Reactive] public string ButtonText { get; set; } = "Cancel";
    [Reactive] public bool IsFinished { get; set; }
    [Reactive] public int NumErrors { get; set; }
    [Reactive] public List<string> GenerationErrors { get; set; } = [];
    public bool ExportYaml { get; set; }
    public bool SplitSmz3 { get; set; }
    public double GenerationSeconds { get; set; }
    
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}

public class MsuPcmGenerationSongViewModel : ViewModelBase
{
    [Reactive] public string SongName { get; set; } = "";
    
    [Reactive] public MsuSongInfoViewModel? OriginalViewModel { get; set; }
    
    [Reactive] public int TrackNumber { get; set; }

    [Reactive] public string TrackName { get; set; } = "";

    [Reactive] public string Path { get; set; } = "";
    
    [Reactive] public bool HasLoaded { get; set; }
    
    [Reactive] public bool HasWarning { get; set; }

    [Reactive] public string Message { get; set; } = "";
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}