using Material.Icons;
using MSUScripter.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

[SkipLastModified]
public class AudioControlViewModel : ViewModelBase
{
    [Reactive] public MaterialIconKind Icon { get; set; } = MaterialIconKind.Stop;
    [Reactive] public double Position { get; set; }
    [Reactive] public double Volume { get; set; }
    [Reactive] public string Timestamp { get; set; } = "0:00/0:00";
    [Reactive] public bool CanPlayMusic { get; set; }
    [Reactive] public bool CanChangePosition { get; set; }
    [Reactive] public bool CanPlayPause { get; set; }
    [Reactive] public bool CanChangeVolume { get; set; }
    [Reactive] public int? JumpToSeconds { get; set; }
    [Reactive] public bool CanPopout { get; set; }
    [Reactive] public bool CanSetTimeSeconds { get; set; }
    [Reactive] public bool CanPressPopoutButton { get; set; } = true;
    public double PreviousPosition { get; set; }
    public int IconSize => 16;
    
    public override ViewModelBase DesignerExample()
    {
        CanPlayMusic = true;
        CanChangePosition = true;
        CanPlayPause = true;
        CanChangeVolume = true;
        CanPopout = true;
        CanSetTimeSeconds = true;
        return this;
    }
}