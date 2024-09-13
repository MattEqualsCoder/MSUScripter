using System.Collections.Generic;
using System.Linq;
using AvaloniaControls.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public enum CopyMoveType
{
    Copy,
    Move,
    Swap
}

public class CopyMoveTrackWindowViewModel : ViewModelBase
{
    [Reactive] public MsuProjectViewModel? Project { get; set; }
    [Reactive] public MsuTrackInfoViewModel? PreviousTrack { get; set; }
    [Reactive] public MsuSongInfoViewModel? PreviousSong { get; set; }
    [Reactive] public CopyMoveType Type { get; set; }
    [Reactive] public List<MsuTrackInfoViewModel> Tracks { get; set; } = [];
    [Reactive] public bool IsTargetLocationEnabled { get; set; }
    [Reactive] public int OriginalLocation { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CanPressButton))]
    public MsuTrackInfoViewModel TargetTrack { get; set; } = new();
    
    [Reactive, ReactiveLinkedProperties(nameof(CanPressButton))] 
    public List<string> TargetLocationOptions { get; set; } = [];
    
    [Reactive, ReactiveLinkedProperties(nameof(CanPressButton))]
    public int TargetLocation { get; set; }
    
    public bool CanPressButton => Type != CopyMoveType.Swap || (TargetLocationOptions.Count > 0 &&
                                                                (PreviousTrack != TargetTrack ||
                                                                 TargetLocation != OriginalLocation));
    
    public string MainText =>
        Type switch
        {
            CopyMoveType.Move => "Select a track to move song to",
            CopyMoveType.Copy => "Select a track to copy song to",
            _ => "Select a track to swap with"
        };

    public string LocationText =>
        Type switch
        {
            CopyMoveType.Swap => "Song to swap with",
            _ => "Song placement"
        };
    
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