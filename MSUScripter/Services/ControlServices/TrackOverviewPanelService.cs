using System.Linq;
using AvaloniaControls.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class TrackOverviewPanelService : ControlService
{
    private readonly TrackOverviewPanelViewModel _model = new();

    public TrackOverviewPanelViewModel InitializeModel(MsuProjectViewModel project)
    {
        _model.MsuProjectViewModel = project;
        RefreshTracks();
        return _model;
    }

    public void RefreshTracks()
    {
        var tracks = _model.MsuProjectViewModel.Tracks;
        
        foreach (var track in tracks.OrderBy(x => x.TrackNumber))
        {
            if (!track.Songs.Any())
            {
                _model.Rows.Add(new TrackOverviewPanelViewModel.TrackOverviewRow()
                {
                    TrackNumber = track.TrackNumber,
                    TrackName = track.TrackName
                });
            }
            else
            {
                _model.Rows.AddRange(track.Songs.Select(x => new TrackOverviewPanelViewModel.TrackOverviewRow()
                {
                    HasSong = true,
                    SongInfo = x,
                    TrackNumber = track.TrackNumber,
                    TrackName = track.TrackName + (x.IsAlt ? " (Alt)" : ""),
                    Name = x.SongName ?? "",
                    Artist = x.Artist ?? "",
                    Album = x.Album ?? "",
                    File = !x.MsuPcmInfo.HasFiles() ? ""
                        : x.MsuPcmInfo.GetFileCount() == 1
                            ? x.MsuPcmInfo.File!
                            : $"{x.MsuPcmInfo.GetFileCount()} files"
                }));
            }
        }

        UpdateCompletedTrackDetails();
    }

    public void UpdateCompletedTrackDetails()
    {
        _model.UpdateCompletedTrackDetails();
    }
}