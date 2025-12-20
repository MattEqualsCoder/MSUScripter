using Material.Icons;
using MSUScripter.Models;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

[SkipLastModified]
public partial class AudioControlViewModel : ViewModelBase
{
    [Reactive] public partial MaterialIconKind Icon { get; set; }
    [Reactive] public partial double Position { get; set; }
    [Reactive] public partial double Volume { get; set; }
    [Reactive] public partial string Timestamp { get; set; }
    [Reactive] public partial bool CanPlayMusic { get; set; }
    [Reactive] public partial bool CanChangePosition { get; set; }
    [Reactive] public partial bool CanPlayPause { get; set; }
    [Reactive] public partial bool CanChangeVolume { get; set; }
    [Reactive] public partial int? JumpToSeconds { get; set; }
    [Reactive] public partial bool CanSetTimeSeconds { get; set; }
    public double PreviousPosition { get; set; }
    public int IconSize => 16;

    public AudioControlViewModel()
    {
        Icon = MaterialIconKind.Stop;
        Timestamp = "0:00/0:00";
    }
    
    public override ViewModelBase DesignerExample()
    {
        CanPlayMusic = true;
        CanChangePosition = true;
        CanPlayPause = true;
        CanChangeVolume = true;
        CanSetTimeSeconds = true;
        return this;
    }
}