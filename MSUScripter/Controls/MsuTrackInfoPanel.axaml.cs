using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MSUScripter.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class MsuTrackInfoPanel : UserControl
{
    private MsuProjectViewModel? _project;
    private MsuTrackInfoViewModel? _trackInfo;

    public MsuTrackInfoPanel()
    {
        InitializeComponent();
    }

    public void SetTrackInfo(MsuProjectViewModel project, MsuTrackInfoViewModel trackInfo)
    {
        _project = project;
        trackInfo.Project = project;
        _trackInfo = trackInfo;
        DataContext = trackInfo;
    }
    
    public event EventHandler<PcmEventArgs>? PcmOptionSelected; 
    
    public event EventHandler<SongFileEventArgs>? FileUpdated;
    
    public event EventHandler<SongFileEventArgs>? MetaDataFileSelected;
    
    public event EventHandler<TrackEventArgs>? AddSongWindowButtonPressed;
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var songInfo = new MsuSongInfoViewModel()
        {
            TrackNumber = _trackInfo!.TrackNumber,
            TrackName = _trackInfo!.TrackName,
            IsAlt = _trackInfo.Songs.Count > 0,
        };
        
        var msu = new FileInfo(_project!.MsuPath);
        if (!songInfo.IsAlt)
        {
            songInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{_trackInfo.TrackNumber}.pcm");
        }
        else
        {
            var altSuffix = _trackInfo.Songs.Count == 1 ? "alt" : $"alt{_trackInfo.Songs.Count}";
            songInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{_trackInfo.TrackNumber}_{altSuffix}.pcm");
        }

        songInfo.Project = _project!;
        songInfo.MsuPcmInfo.Project = _project!;
        songInfo.MsuPcmInfo.Song = songInfo;
        songInfo.MsuPcmInfo.IsTopLevel = true;

        _trackInfo!.AddSong(songInfo);
    }

    private void AddSongWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        AddSongWindowButtonPressed?.Invoke(this, new TrackEventArgs(_trackInfo!.TrackNumber));
    }

    private void MsuSongInfoPanel_OnOnDelete(object? sender, EventArgs e)
    {
        if (sender is not MsuSongInfoPanel songPanel || _trackInfo == null) return;
        var songInfoViewModel = songPanel.DataContext as MsuSongInfoViewModel;
        _trackInfo.RemoveSong(songInfoViewModel);

        if (songInfoViewModel?.IsAlt != true && _trackInfo.Songs.Any())
        {
            var newPrimaryTrack = _trackInfo.Songs.First();
            newPrimaryTrack.IsAlt = false;
            var msu = new FileInfo(_project!.MsuPath);
            newPrimaryTrack.OutputPath = msu.FullName.Replace(msu.Extension, $"-{_trackInfo.TrackNumber}.pcm");
        }
    }

    private void MsuSongInfoPanel_OnPcmOptionSelected(object? sender, PcmEventArgs e)
    {
        PcmOptionSelected?.Invoke(sender, e);
    }

    private void MsuSongInfoPanel_OnMetaDataFileSelected(object? sender, SongFileEventArgs e)
    {
        MetaDataFileSelected?.Invoke(sender, e);
    }

    private void MsuSongInfoPanel_OnFileUpdated(object? sender, SongFileEventArgs e)
    {
        FileUpdated?.Invoke(sender, e);
    }
}