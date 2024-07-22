using System.IO;
using System.Linq;
using AvaloniaControls.ControlServices;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class CopyMoveTrackWindowService (ConverterService converterService) : ControlService
{
    private readonly CopyMoveTrackWindowViewModel _model = new();

    public CopyMoveTrackWindowViewModel InitializeModel(MsuProjectViewModel msuProjectViewModel, MsuTrackInfoViewModel trackViewModel,
        MsuSongInfoViewModel msuSongInfoViewModel, bool isMove)
    {
        _model.Project = msuProjectViewModel;
        _model.PreviousTrack = trackViewModel;
        _model.PreviousSong = msuSongInfoViewModel;
        _model.IsMove = isMove;
        _model.Tracks = msuProjectViewModel.Tracks.OrderBy(x => x.TrackNumber).ToList();
        _model.TargetTrack = _model.PreviousTrack;
        
        return _model;
    }

    public void RunCopyMove()
    {
        if (_model.PreviousTrack == null || _model.PreviousSong == null || _model.Project == null)
        {
            return;
        }

        var destinationTrack = _model.TargetTrack;
        
        if (_model.IsMove)
        {
            if (destinationTrack.TrackNumber == _model.PreviousTrack.TrackNumber)
            {
                return;
            }

            var songInfo = _model.PreviousSong;

            songInfo.TrackNumber = destinationTrack.TrackNumber;
            songInfo.TrackName = destinationTrack.TrackName;
            songInfo.IsAlt = destinationTrack.Songs.Count > 0;
            songInfo.MsuPcmInfo.IsAlt = songInfo.IsAlt;

            var msu = new FileInfo(_model.Project.MsuPath);
            if (!songInfo.MsuPcmInfo.IsAlt)
            {
                songInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{destinationTrack.TrackNumber}.pcm");
            }
            else
            {
                var altSuffix = destinationTrack.Songs.Count == 1 ? "alt" : $"alt{destinationTrack.Songs.Count}";
                songInfo.OutputPath =
                    msu.FullName.Replace(msu.Extension, $"-{destinationTrack.TrackNumber}_{altSuffix}.pcm");
            }

            _model.PreviousTrack.Songs.Remove(_model.PreviousSong);
            destinationTrack.Songs.Add(_model.PreviousSong);
        }
        else
        {
            var msuSongInfo = new MsuSongInfo(); 
            converterService.ConvertViewModel(_model.PreviousSong, msuSongInfo);
            converterService.ConvertViewModel(_model.PreviousSong.MsuPcmInfo, msuSongInfo.MsuPcmInfo);
            
            msuSongInfo.TrackNumber = destinationTrack.TrackNumber;
            msuSongInfo.TrackName = destinationTrack.TrackName;
            msuSongInfo.IsAlt = destinationTrack.Songs.Count > 0;

            var msu = new FileInfo(_model.Project.MsuPath);
            if (!msuSongInfo.IsAlt)
            {
                msuSongInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{destinationTrack.TrackNumber}.pcm");
            }
            else
            {
                var altSuffix = destinationTrack.Songs.Count == 1 ? "alt" : $"alt{destinationTrack.Songs.Count}";
                msuSongInfo.OutputPath =
                    msu.FullName.Replace(msu.Extension, $"-{destinationTrack.TrackNumber}_{altSuffix}.pcm");
            }
            
            var msuSongInfoCloned = new MsuSongInfoViewModel(); 
            converterService.ConvertViewModel(msuSongInfo, msuSongInfoCloned);
            converterService.ConvertViewModel(msuSongInfo.MsuPcmInfo, msuSongInfoCloned.MsuPcmInfo);
            msuSongInfoCloned.Project = _model.Project;
            destinationTrack.Songs.Add(msuSongInfoCloned);
        }
    }
}