using System.IO;
using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MsuTrackInfoPanelService(SharedPcmService sharedPcmService) : ControlService
{
    private MsuTrackInfoViewModel _model = new();
    
    public void InitializeModel(MsuTrackInfoViewModel model)
    {
        _model = model;
        _model.CanPlaySongs = sharedPcmService.CanPlaySongs;
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

        songInfo.ApplyCascadingSettings(_model.Project, _model, isAlt, sharedPcmService.CanPlaySongs, true, true);
        _model.Songs.Add(songInfo);
    }

    public void SetScrolledSong(MsuSongInfoViewModel? song, int index)
    {
        if (song == null || index == -1)
        {
            _model.FloatingSongBannerSongInfo = null;
        }
        
        _model.FloatingSongBannerSongInfo = song;
    }

    public async Task<string?> PlayCurrent()
    {
        if (_model.FloatingSongBannerSongInfo == null)
        {
            return null;
        }
        
        return await sharedPcmService.PlaySong(_model.FloatingSongBannerSongInfo, false);
    }

    public async Task<string?> LoopCurrent()
    {
        if (_model.FloatingSongBannerSongInfo == null)
        {
            return null;
        }
        
        return await sharedPcmService.PlaySong(_model.FloatingSongBannerSongInfo, true);
    }

    public async Task PauseCurrent()
    {
        if (_model.FloatingSongBannerSongInfo == null)
        {
            return;
        }

        await sharedPcmService.PauseSong();
    }
}