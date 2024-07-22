using System.IO;
using AvaloniaControls.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MsuTrackInfoPanelService : ControlService
{
    private MsuTrackInfoViewModel _model = new();
    
    public void InitializeModel(MsuTrackInfoViewModel model)
    {
        _model = model;
    }

    public void AddSong()
    {
        var songInfo = new MsuSongInfoViewModel()
        {
            TrackNumber = _model.TrackNumber,
            TrackName = _model.TrackName,
            IsAlt = _model.Songs.Count > 0,
        };
        
        var msu = new FileInfo(_model.Project.MsuPath);
        if (!songInfo.IsAlt)
        {
            songInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{_model.TrackNumber}.pcm");
        }
        else
        {
            var altSuffix = _model.Songs.Count == 1 ? "alt" : $"alt{_model.Songs.Count}";
            songInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{_model.TrackNumber}_{altSuffix}.pcm");
        }

        songInfo.Project = _model.Project;
        songInfo.Track = _model;
        songInfo.MsuPcmInfo.ApplyCascadingSettings(songInfo.Project, songInfo, songInfo.IsAlt, null, songInfo.CanPlaySongs, true, true);
        _model.Songs.Add(songInfo);
    }
}