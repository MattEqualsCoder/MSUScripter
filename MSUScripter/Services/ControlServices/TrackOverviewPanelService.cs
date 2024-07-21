using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AvaloniaControls.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class TrackOverviewPanelService : ControlService
{
    private readonly TrackOverviewPanelViewModel _model = new();

    public TrackOverviewPanelViewModel InitializeModel(EditProjectPanelViewModel editProjectPanelViewModel)
    {
        _model.MsuProjectViewModel = editProjectPanelViewModel.MsuProjectViewModel ?? new MsuProjectViewModel();
        RefreshTracks();
        return _model;
    }

    public void RefreshTracks()
    {
        var tracks = _model.MsuProjectViewModel.Tracks;

        var rows = new List<TrackOverviewPanelViewModel.TrackOverviewRow>();
        
        foreach (var track in tracks.OrderBy(x => x.TrackNumber))
        {
            if (!track.Songs.Any())
            {
                rows.Add(new TrackOverviewPanelViewModel.TrackOverviewRow(track.TrackNumber, track.TrackName));
            }
            else
            {
                rows.AddRange(track.Songs.Select(x =>
                    new TrackOverviewPanelViewModel.TrackOverviewRow(track.TrackNumber,
                        track.TrackName + (x.IsAlt ? " (Alt)" : ""), x)));
            }
        }

        _model.Rows = rows;

        UpdateCompletedTrackDetails();
    }

    public void UpdateCompletedTrackDetails()
    {
        _model.UpdateCompletedTrackDetails();
    }

    public MsuProjectViewModel GetProject()
    {
        return _model.MsuProjectViewModel;
    }
}