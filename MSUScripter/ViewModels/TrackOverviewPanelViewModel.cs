using System.Collections.Generic;
using System.Linq;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class TrackOverviewPanelViewModel : ViewModelBase
{
    public MsuProjectViewModel MsuProjectViewModel { get; set; } = new();
    
    [Reactive] public List<TrackOverviewRow> Rows { get; set; } = new();

    [Reactive] public string CompletedSongDetails { get; set; } = "";

    [Reactive] public string CompletedTrackDetails { get; set; } = "";

    [Reactive] public int SelectedIndex { get; set; } = 0;

    public int TotalTrackCount => MsuProjectViewModel.Tracks.Count;
    
    public int CompletedTrackCount => MsuProjectViewModel.Tracks.Count(x => x.Songs.Any(y => y.HasFiles()));
    
    public int TotalSongCount => Rows.Count(x => x.HasSong);
    
    public int CompletedSongCount => Rows.Count(x => x is { HasSong: true, SongInfo.IsComplete: true });

    public void UpdateCompletedTrackDetails()
    {
        CompletedSongDetails = $"{CompletedSongCount} out of {TotalSongCount} songs are marked as finished";
        CompletedTrackDetails = $"{CompletedTrackCount} out of {TotalTrackCount} tracks have songs with audio files";
    }
    
    public class TrackOverviewRow(int trackNumber, string trackName, MsuSongInfoViewModel? song = null)
    {
        public int TrackNumber => trackNumber;
        public string TrackName => trackName;
        public MsuSongInfoViewModel? SongInfo => song;
        
        public bool HasSong => SongInfo != null;
        public string Name => SongInfo?.SongName ?? "";
        public string Artist => SongInfo?.Artist ?? "";
        public string Album => SongInfo?.Album ?? "";

        public string File =>
            SongInfo == null
                ? ""
                : !SongInfo.MsuPcmInfo.HasFiles()
                    ? ""
                    : SongInfo.MsuPcmInfo.GetFileCount() == 1
                        ? SongInfo.MsuPcmInfo.File!
                        : $"{SongInfo.MsuPcmInfo.GetFileCount()} files";
    }
    
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}