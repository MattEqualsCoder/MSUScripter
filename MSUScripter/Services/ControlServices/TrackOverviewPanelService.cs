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
                _model.Rows.Add(new TrackOverviewPanelViewModel.TrackOverviewRow(track.TrackNumber, track.TrackName));
            }
            else
            {
                _model.Rows.AddRange(track.Songs.Select(x =>
                    new TrackOverviewPanelViewModel.TrackOverviewRow(track.TrackNumber,
                        track.TrackName + (x.IsAlt ? " (Alt)" : ""), x)));
            }
        }

        UpdateCompletedTrackDetails();
    }

    public void UpdateCompletedTrackDetails()
    {
        _model.UpdateCompletedTrackDetails();
    }
}