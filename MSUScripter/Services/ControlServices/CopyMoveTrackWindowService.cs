using System.Collections.Generic;
using System.IO;
using System.Linq;
using AvaloniaControls.ControlServices;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class CopyMoveTrackWindowService (ConverterService converterService) : ControlService
{
    private readonly CopyMoveTrackWindowViewModel _model = new();

    public CopyMoveTrackWindowViewModel InitializeModel(MsuProjectViewModel msuProjectViewModel, MsuTrackInfoViewModel trackViewModel,
        MsuSongInfoViewModel msuSongInfoViewModel, CopyMoveType type)
    {
        _model.Project = msuProjectViewModel;
        _model.PreviousTrack = trackViewModel;
        _model.PreviousSong = msuSongInfoViewModel;
        _model.Type = type;
        _model.Tracks = msuProjectViewModel.Tracks.OrderBy(x => x.TrackNumber).ToList();
        _model.TargetTrack = _model.PreviousTrack;
        _model.OriginalLocation = trackViewModel.Songs.IndexOf(msuSongInfoViewModel);
        
        return _model;
    }

    public void RunCopyMove()
    {
        if (_model.PreviousTrack == null || _model.PreviousSong == null || _model.Project == null)
        {
            return;
        }

        var songInfo = _model.PreviousSong;
        var previousTrack = _model.PreviousTrack;
        var destinationTrack = _model.TargetTrack;
        
        if (_model.Type == CopyMoveType.Move)
        {
            var targetLocation = _model.TargetLocation;
            if (previousTrack == destinationTrack && targetLocation > _model.OriginalLocation)
            {
                targetLocation--;
            }
            previousTrack.Songs.Remove(songInfo);
            destinationTrack.Songs.Insert(targetLocation, songInfo);
        }
        else if (_model.Type == CopyMoveType.Copy)
        {
            var msuSongInfo = new MsuSongInfo(); 
            converterService.ConvertViewModel(_model.PreviousSong, msuSongInfo);
            converterService.ConvertViewModel(_model.PreviousSong.MsuPcmInfo, msuSongInfo.MsuPcmInfo);
            
            var msuSongInfoCloned = new MsuSongInfoViewModel(); 
            converterService.ConvertViewModel(msuSongInfo, msuSongInfoCloned);
            converterService.ConvertViewModel(msuSongInfo.MsuPcmInfo, msuSongInfoCloned.MsuPcmInfo);
            
            destinationTrack.Songs.Insert(_model.TargetLocation, msuSongInfoCloned);
        }
        else if (_model.Type == CopyMoveType.Swap)
        {
            var originalIndex = previousTrack.Songs.IndexOf(songInfo);
            var swapSong = destinationTrack.Songs[_model.TargetLocation];
            previousTrack.Songs.Remove(songInfo);
            destinationTrack.Songs.Remove(swapSong);
            previousTrack.Songs.Insert(originalIndex, swapSong);
            destinationTrack.Songs.Insert(_model.TargetLocation, songInfo);
        }

        previousTrack.FixTrackSuffixes(songInfo.CanPlaySongs);
        if (previousTrack != destinationTrack)
        {
            destinationTrack.FixTrackSuffixes(songInfo.CanPlaySongs);
        }
    }
    
    public void UpdateTrackLocations()
    {
        List<string> locationOptions = [];

        var prefix = _model.Type == CopyMoveType.Swap ? "Song " : "Before song ";
        
        for (var i = 0; i < _model.TargetTrack.Songs.Count; i++)
        {
            if (string.IsNullOrEmpty(_model.TargetTrack.Songs[0].SongName))
            {
                locationOptions.Add($"{prefix}{i+1}");
            }
            else
            {
                locationOptions.Add($"{prefix}{i+1}: {_model.TargetTrack.Songs[i].SongName}");
            }
        }

        if (_model.Type != CopyMoveType.Swap)
        {
            locationOptions.Add("At the end of the list");
        }
        
        _model.TargetLocationOptions = locationOptions;
        _model.TargetLocation = locationOptions.Count - 1;
        _model.IsTargetLocationEnabled = locationOptions.Count > 1;
    }
}