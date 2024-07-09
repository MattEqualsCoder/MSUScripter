using Material.Icons;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class AudioControlViewModel : ViewModelBase
{
    [Reactive] public MaterialIconKind Icon { get; set; } = MaterialIconKind.Stop;
    [Reactive] public double Position { get; set; }
    [Reactive] public double Volume { get; set; }
    [Reactive] public string Timestamp { get; set; } = "0:00/0:00";
    [Reactive] public bool CanPlayMusic { get; set; }
    [Reactive] public bool CanSetMusicPosition { get; set; }
    [Reactive] public bool CanPlayPause { get; set; }
    [Reactive] public bool CanChangeVolume { get; set; }
    
    public override ViewModelBase DesignerExample()
    {
        CanPlayMusic = true;
        CanSetMusicPosition = true;
        CanPlayPause = true;
        CanChangeVolume = true;
        return this;
    }
}