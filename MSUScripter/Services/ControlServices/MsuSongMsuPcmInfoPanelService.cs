using System;
using System.IO;
using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MsuSongMsuPcmInfoPanelService(
    ILogger<MsuSongMsuPcmInfoPanelService> logger,
    Settings settings,
    SettingsService settingsService,
    IAudioPlayerService audioPlayerService,
    MsuPcmService msuPcmService,
    ConverterService converterService,
    AudioAnalysisService audioAnalysisService,
    StatusBarService statusBarService,
    YamlService yamlService) : ControlService
{
    private MsuSongMsuPcmInfoViewModel _model = new();

    public void InitializeModel(MsuSongMsuPcmInfoViewModel model)
    {
        _model = model;
        _model.CanPlaySongs = audioPlayerService.CanPlayMusic;
    }

    public void AddSubTrack(int? index = null, bool addToParent = false)
    {
        if (addToParent)
        {
            if (index is null or -1)
            {
                _model.ParentMsuPcmInfo?.SubTracks.Add(new MsuSongMsuPcmInfoViewModel
                    { Project = _model.Project, Song = _model.Song, IsAlt = _model.IsAlt, ParentMsuPcmInfo = _model.ParentMsuPcmInfo });
            }
            else
            {
                _model.ParentMsuPcmInfo?.SubTracks.Insert(index.Value, new MsuSongMsuPcmInfoViewModel
                    { Project = _model.Project, Song = _model.Song, IsAlt = _model.IsAlt, ParentMsuPcmInfo = _model.ParentMsuPcmInfo });
            }
        }
        else
        {
            if (index is null or -1)
            {
                _model.SubTracks.Add(new MsuSongMsuPcmInfoViewModel
                    { Project = _model.Project, Song = _model.Song, IsAlt = _model.IsAlt, ParentMsuPcmInfo = _model });
            }
            else
            {
                _model.SubTracks.Insert(index.Value, new MsuSongMsuPcmInfoViewModel
                    { Project = _model.Project, Song = _model.Song, IsAlt = _model.IsAlt, ParentMsuPcmInfo = _model });
            }
        }
        
    }
    
    public void AddSubChannel(int? index = null, bool addToParent = false)
    {
        if (addToParent)
        {
            if (index is null or -1)
            {
                _model.ParentMsuPcmInfo?.SubChannels.Add(new MsuSongMsuPcmInfoViewModel
                    { Project = _model.Project, Song = _model.Song, IsAlt = _model.IsAlt, ParentMsuPcmInfo = _model.ParentMsuPcmInfo });
            }
            else
            {
                _model.ParentMsuPcmInfo?.SubChannels.Insert(index.Value, new MsuSongMsuPcmInfoViewModel
                    { Project = _model.Project, Song = _model.Song, IsAlt = _model.IsAlt, ParentMsuPcmInfo = _model.ParentMsuPcmInfo });
            }
        }
        else
        {
            if (index is null or -1)
            {
                _model.SubChannels.Add(new MsuSongMsuPcmInfoViewModel
                    { Project = _model.Project, Song = _model.Song, IsAlt = _model.IsAlt, ParentMsuPcmInfo = _model });
            }
            else
            {
                _model.SubChannels.Insert(index.Value, new MsuSongMsuPcmInfoViewModel
                    { Project = _model.Project, Song = _model.Song, IsAlt = _model.IsAlt, ParentMsuPcmInfo = _model });
            }
        }
    }

    public void Delete()
    {
        if (_model.ParentMsuPcmInfo == null)
        {
            return;
        }

        if (_model.IsSubChannel)
        {
            _model.ParentMsuPcmInfo.SubChannels.Remove(_model);
        }
        else if (_model.IsSubTrack)
        {
            _model.ParentMsuPcmInfo.SubTracks.Remove(_model);
        }
    }

    public bool ShouldShowSubTracksSubChannelsWarningPopup(bool newSubTrack, bool newSubChannel)
    {
        var numSubTracks = _model.SubTracks.Count + (newSubTrack ? 1 : 0);
        var numSubChannels = _model.SubChannels.Count + (newSubChannel ? 1 : 0);
        return !settings.HideSubTracksSubChannelsWarning && numSubTracks > 1 && numSubChannels > 1;
    }

    public void HideSubTracksSubChannelsWarning()
    {
        settings.HideSubTracksSubChannelsWarning = true;
        settingsService.SaveSettings();
    }

    public async Task<string?> PlaySong(bool testLoop)
    {
        await audioPlayerService.StopSongAsync(null, true);
        
        if (!GeneratePcmFile(false, false, out var error, out _))
            return error;
        
        if (string.IsNullOrEmpty(_model.Song.OutputPath) || !File.Exists(_model.Song.OutputPath))
        {
            return "No pcm file detected";
        }
        
        // UpdateStatusBarText("Playing Song");
        await audioPlayerService.PlaySongAsync(_model.Song.OutputPath, testLoop);
        return null;
    }

    public void IgnoreMsuPcmError()
    {
        if (string.IsNullOrEmpty(_model.Song.OutputPath)) return;
        _model.Project.IgnoreWarnings.Add(_model.Song.OutputPath);
    }
    
    public bool GeneratePcmFile(bool asPrimary, bool asEmpty, out string error, out bool msuPcmError)
    {
        error = "";
        msuPcmError = false;
        
        if (msuPcmService.IsGeneratingPcm) return false;

        if (asEmpty)
        {
            var emptySong = new MsuSongInfo();
            converterService.ConvertViewModel(_model.Song, emptySong);
            var successful = msuPcmService.CreateEmptyPcm(emptySong);
            if (!successful)
            {
                error = "Could not generate empty pcm file";
                return false;
            }
            //UpdateStatusBarText("PCM Generated");
            return true;
        }
        
        if (!_model.HasFiles())
        {
            error = "No files specified to generate into a pcm file";
            return false;
        }
        
        //UpdateStatusBarText("Generating PCM");
        var song = new MsuSongInfo();
        converterService.ConvertViewModel(_model.Song, song);
        converterService.ConvertViewModel(_model, song.MsuPcmInfo);
        var tempProject = converterService.ConvertProject(_model.Project);

        if (asPrimary)
        {
            var msu = new FileInfo(_model.Project.MsuPath);
            var path = msu.FullName.Replace(msu.Extension, $"-{song.TrackNumber}.pcm");
            song.OutputPath = path;
        }
        
        if (!msuPcmService.CreatePcm(tempProject, song, out var msuPcmMessage, out var generated, false))
        {
            if (generated)
            {
                //UpdateStatusBarText("PCM Generated with Warning");

                if (!_model.Project.IgnoreWarnings.Contains(song.OutputPath))
                {
                    msuPcmError = true;
                    error = msuPcmMessage ?? "Unknown error with msupcm++";
                }
                
                _model.Song.LastGeneratedDate = DateTime.Now;
                return true;
            }
            else
            {
                //UpdateStatusBarText("msupcm++ Error");
                error = msuPcmMessage ?? "Unknown error with msupcm++";
                return false;
            }
        }
        
        _model.Song.LastGeneratedDate = DateTime.Now;

        //var hasAlts = tempProject.Tracks.First(x => x.TrackNumber == songModel.TrackNumber).Songs.Count > 1;
        
        //UpdateStatusBarText(hasAlts ? "PCM Generated - YAML Regeneration Needed" : "PCM Generated");
        return true;
    }

    public string? GetCopyDetailsString()
    {
        MsuSongMsuPcmInfo output = new();
        if (!converterService.ConvertViewModel(_model, output))
        {
            return null;
        }
        output.ClearLastModifiedDate();
        return yamlService.ToYaml(output, false);
    }

    public string? CopyDetailsFromString(string yamlText)
    {
        if (!yamlService.FromYaml<MsuSongMsuPcmInfo>(yamlText, out var yamlMsuPcmDetails, out _, false) || yamlMsuPcmDetails == null)
        {
            return "Invalid msupcm++ track details";
        }

        var originalProject = _model.Project;
        var originalSong = _model.Song;
        var originalIsAlt = _model.IsAlt;
        var originalParent = _model.ParentMsuPcmInfo;
            
        if (!converterService.ConvertViewModel(yamlMsuPcmDetails, _model))
        {
            return "Invalid msupcm++ track details";
        }
            
        _model.ApplyCascadingSettings(originalProject, originalSong, originalIsAlt, originalParent, true);
        _model.LastModifiedDate = DateTime.Now;
        return null;
    }
    
    public void UpdateLoopSettings(PyMusicLooperResultViewModel loopResult)
    {
        _model.Loop = loopResult.LoopStart;
        _model.TrimEnd = loopResult.LoopEnd;
    }

    public bool HasLoopDetails()
    {
        return _model.Loop > 0 || _model.TrimEnd > 0;
    }

    public async Task StopSong()
    {
        await audioPlayerService.StopSongAsync(null, true);
    }
    
    public string? GetStartingSamples()
    {
        if (string.IsNullOrEmpty(_model.File) || !File.Exists(_model.File))
        {
            return "No input file selected";
        }

        try
        {
            var samples = audioAnalysisService.GetAudioStartingSample(_model.File);
            _model.TrimStart = samples;
            return null;
            //UpdateStatusBarText("Starting samples retrieved");
        }
        catch
        {
            return "Unable to get starting samples for file";
        }
    }
}