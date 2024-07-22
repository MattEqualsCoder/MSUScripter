using System;
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

        _model.Rows.Clear();
        
        foreach (var track in tracks.OrderBy(x => x.TrackNumber))
        {
            if (!track.Songs.Any())
            {
                _model.Rows.Add(new TrackOverviewPanelViewModel.TrackOverviewRow(track.TrackNumber, track.TrackName));
            }
            else
            {
                foreach (var song in track.Songs)
                {
                    _model.Rows.Add(new TrackOverviewPanelViewModel.TrackOverviewRow(track.TrackNumber,
                       track.TrackName + (song.IsAlt ? " (Alt)" : ""), song));
                }
                
            }
        }

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

    internal void AddSong(TrackOverviewPanelViewModel.TrackOverviewRow row, MsuSongInfoViewModel newSongInfo)
    {
        if (row.HasSong)
        {
            var oldRowIndex = _model.Rows.IndexOf(row);
            _model.Rows.Insert(oldRowIndex + 1, new TrackOverviewPanelViewModel.TrackOverviewRow(row.TrackNumber, row.TrackName, newSongInfo));
        }
        else
        {
            row.SongInfo = newSongInfo;
        }
    }
}