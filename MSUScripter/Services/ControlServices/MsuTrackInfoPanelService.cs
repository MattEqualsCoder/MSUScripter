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
        var songInfo = new MsuSongInfoViewModel();

        var isAlt = _model.Songs.Count > 0;
        
        var msu = new FileInfo(_model.Project.MsuPath);
        if (!isAlt)
        {
            songInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{_model.TrackNumber}.pcm");
        }
        else
        {
            var altSuffix = _model.Songs.Count == 1 ? "alt" : $"alt{_model.Songs.Count}";
            songInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{_model.TrackNumber}_{altSuffix}.pcm");
        }

        songInfo.ApplyCascadingSettings(_model.Project, _model, isAlt, true, true, true);
        _model.Songs.Add(songInfo);
    }
}