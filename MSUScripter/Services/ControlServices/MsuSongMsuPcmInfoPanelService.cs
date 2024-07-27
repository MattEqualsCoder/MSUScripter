using System;
using System.IO;
using AvaloniaControls.ControlServices;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MsuSongMsuPcmInfoPanelService(
    Settings settings,
    SettingsService settingsService,
    IAudioPlayerService audioPlayerService,
    ConverterService converterService,
    AudioAnalysisService audioAnalysisService,
    AudioMetadataService audioMetadataService,
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

        var topLevelPcmInfo = _model.TopLevel;
        topLevelPcmInfo.UpdateSubTrackSubChannelWarning();
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

        var topLevelPcmInfo = _model.TopLevel;
        topLevelPcmInfo.UpdateSubTrackSubChannelWarning();
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

        var topLevelPcmInfo = _model.TopLevel;
        topLevelPcmInfo.UpdateSubTrackSubChannelWarning();
    }

    public bool ShouldShowSubTracksSubChannelsWarningPopup(bool newSubTrack, bool newSubChannel)
    {
        var numSubTracks = _model.SubTracks.Count + (newSubTrack ? 1 : 0);
        var numSubChannels = _model.SubChannels.Count + (newSubChannel ? 1 : 0);
        return !settings.HideSubTracksSubChannelsWarning && numSubTracks > 0 && numSubChannels > 0;
    }

    public void HideSubTracksSubChannelsWarning()
    {
        settings.HideSubTracksSubChannelsWarning = true;
        settingsService.SaveSettings();
    }

    public string? GetCopyDetailsString()
    {
        MsuSongMsuPcmInfo output = new();
        if (!converterService.ConvertViewModel(_model, output))
        {
            return null;
        }
        output.ClearFieldsForYaml();
        var yamlText = yamlService.ToYaml(output, YamlType.PascalIgnoreDefaults);
        
        return
            """
            # yaml-language-server: $schema=https://raw.githubusercontent.com/MattEqualsCoder/MSUScripter/main/Schemas/MsuSongMsuPcmInfo.json
            # Use Visual Studio Code with the YAML plugin from redhat for schema support (make sure the language is set to YAML)
            
            """ + yamlText;
    }

    public string? CopyDetailsFromString(string yamlText)
    {
        if (!yamlService.FromYaml<MsuSongMsuPcmInfo>(yamlText, YamlType.PascalIgnoreDefaults, out var yamlMsuPcmDetails, out _) || yamlMsuPcmDetails == null)
        {
            return "Invalid msupcm++ track details";
        }

        var originalProject = _model.Project;
        var originalSong = _model.Song;
        var originalIsAlt = _model.IsAlt;
        var originalParent = _model.ParentMsuPcmInfo;
        var originalCanPlaySongs = _model.CanPlaySongs;

        _model.SubTracks.Clear();
        _model.SubChannels.Clear();
            
        if (!converterService.ConvertViewModel(yamlMsuPcmDetails, _model))
        {
            return "Invalid msupcm++ track details";
        }
            
        _model.ApplyCascadingSettings(originalProject, originalSong, originalIsAlt, originalParent, originalCanPlaySongs, true, true);
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
        }
        catch
        {
            return "Unable to get starting samples for file";
        }
    }

    public string? GetEndingSamples()
    {
        if (string.IsNullOrEmpty(_model.File) || !File.Exists(_model.File))
        {
            return "No input file selected";
        }

        try
        {
            var samples = audioAnalysisService.GetAudioEndingSample(_model.File);
            _model.TrimEnd = samples;
            return null;
        }
        catch
        {
            return "Unable to get ending samples for file";
        }
    }

    public void ImportAudioMetadata()
    {
        var topLevelPcmInfo = _model.TopLevel;

        if (string.IsNullOrEmpty(_model.File) || !File.Exists(_model.File))
        {
            topLevelPcmInfo.UpdateHertzWarning(audioAnalysisService.GetAudioSampleRate(_model.File));
            topLevelPcmInfo.UpdateMultiWarning();
            return;
        }
        
        var metadata =  audioMetadataService.GetAudioMetadata(_model.File);
        _model.Song.ApplyAudioMetadata(metadata, false);
   
        topLevelPcmInfo.UpdateHertzWarning(audioAnalysisService.GetAudioSampleRate(_model.File));
        topLevelPcmInfo.UpdateMultiWarning();
    }
}