using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MsuSongInfoPanelService(SharedPcmService sharedPcmService, Settings settings, AudioMetadataService audioMetadataService) : ControlService
{
    private MsuSongInfoViewModel _model = new();

    public void InitializeModel(MsuSongInfoViewModel model)
    {
        _model = model;
        _model.Track = _model.Project.Tracks.First(x => x.TrackNumber == model.TrackNumber);
        _model.CanPlaySongs = sharedPcmService.CanPlaySongs;
    }

    public async Task<string?> PlaySong(bool testLoop)
    {
        return await sharedPcmService.PlaySong(_model, testLoop);
    }
    
    public void DeleteSong()
    {
        _model.Track.Songs.Remove(_model);

        if (!_model.IsAlt && _model.Track.Songs.Any())
        {
            var newPrimaryTrack = _model.Track.Songs.First();
            newPrimaryTrack.IsAlt = false;
            var msu = new FileInfo(_model.Project.MsuPath);
            newPrimaryTrack.OutputPath = msu.FullName.Replace(msu.Extension, $"-{_model.TrackNumber}.pcm");
        }
    }
    
    public async Task StopSong()
    {
        await sharedPcmService.StopSong();
    }

    public string? GetOpenMusicFilePath()
    {
        if (!string.IsNullOrEmpty(_model.MsuPcmInfo.File) && File.Exists(_model.MsuPcmInfo.File))
        {
            var file = new FileInfo(_model.MsuPcmInfo.File);
            if (file.Directory?.Exists == true)
            {
                return file.Directory.FullName;
            }
        }
        else if (!string.IsNullOrEmpty(settings.PreviousPath) && Directory.Exists(settings.PreviousPath))
        {
            return settings.PreviousPath;
        }

        return null;
    }
    
    public void ImportAudioMetadata(string file)
    {
        if (string.IsNullOrEmpty(file) || !File.Exists(file))
        {
            return;
        }
        
        var metadata =  audioMetadataService.GetAudioMetadata(file);
        _model.ApplyAudioMetadata(metadata, true);
    }
}