using System.Collections.Generic;
using System.Linq;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class CopyMoveTrackWindowViewModel : ViewModelBase
{
    [Reactive] public MsuProjectViewModel? Project { get; set; }
    [Reactive] public MsuTrackInfoViewModel? PreviousTrack { get; set; }
    [Reactive] public MsuSongInfoViewModel? PreviousSong { get; set; }
    [Reactive] public bool IsMove { get; set; }
    [Reactive] public List<MsuTrackInfoViewModel> Tracks { get; set; } = [];
    [Reactive] public MsuTrackInfoViewModel TargetTrack { get; set; } = new();

    public string MainText =>
        IsMove ? "Select a track to move song to" : "Select a track to copy song to"; 
    
    public override ViewModelBase DesignerExample()
    {
        Tracks =
        [
            new MsuTrackInfoViewModel
            {
                TrackNumber = 5,
                TrackName = "Fifth Track"
            }
        ];
        TargetTrack = Tracks.First();
        return this;
    }
}