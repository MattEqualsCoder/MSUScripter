using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class TrackOverviewPanel : UserControl
{

    public TrackOverviewPanel()
    {
        InitializeComponent();
    }
    
    public TrackOverviewPanel(List<MsuTrackInfoViewModel> tracks)
    {
        InitializeComponent();
        
        foreach (var track in tracks.OrderBy(x => x.TrackNumber))
        {
            if (!track.Songs.Any())
            {
                Model.Rows.Add(new TrackOverviewViewModel.TrackOverviewRow()
                {
                    TrackNumber = track.TrackNumber,
                    TrackName = track.TrackName
                });
            }
            else
            {
                Model.Rows.AddRange(track.Songs.Select(x => new TrackOverviewViewModel.TrackOverviewRow()
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

        var numTracks = tracks.Count;
        var numCompletedTracks = tracks.Count(x => x.Songs.Any(y => y.HasFiles()));
        Model.CompletedTrackDetails = $"{numCompletedTracks} out of {numTracks} tracks have songs with audio files";
        
        DataContext = Model;
    }

    public TrackOverviewViewModel Model { get; set; } = new();

    public event EventHandler<TrackEventArgs>? OnSelectedTrack;

    private void AudioDataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not TextBlock && e.Source is not Border { Name: "CellBorder"})
        {
            return;
        }

        var selectedItems = this.Find<DataGrid>(nameof(TrackDataGrid))!.SelectedItems;
        if (selectedItems.Count <= 0)
        {
            return;
        }
        
        var row = selectedItems[0] as TrackOverviewViewModel.TrackOverviewRow;

        if (row == null)
        {
            return;
        }
        OnSelectedTrack?.Invoke(this, new TrackEventArgs(row.TrackNumber));
    }

    private void IsCompleteCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        Task.Run(() =>
        {
            Task.Delay(TimeSpan.FromSeconds(0.2));
            Dispatcher.UIThread.Invoke(() =>
            {
                GetFinishedSongText();
            });
        });

    }

    private void GetFinishedSongText()
    {
        var numSongs = Model.Rows.Count(x => x.HasSong);
        var numFinishedSongs = Model.Rows.Count(x => x.HasSong && x.SongInfo?.IsComplete == true);
        Model.CompletedSongDetails = $"{numFinishedSongs} out of {numSongs} songs are marked as finished";
    }
}