using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MsuSongInfoPanelService(SharedPcmService sharedPcmService, Settings settings, AudioMetadataService audioMetadataService, ConverterService converterService, YamlService yamlService, AudioAnalysisService audioAnalysisService) : ControlService
{
    private MsuSongInfoViewModel _model = new();

    public void InitializeModel(MsuSongInfoViewModel model)
    {
        _model = model;
        _model.Track = _model.Project.Tracks.First(x => x.TrackNumber == model.TrackNumber);
        _model.CanPlaySongs = sharedPcmService.CanPlaySongs;
        _model.PauseStopIcon = sharedPcmService.CanPauseSongs ? Material.Icons.MaterialIconKind.Pause : Material.Icons.MaterialIconKind.Stop;
        _model.PauseStopText = sharedPcmService.CanPauseSongs ? "Pause Music" : "Stop Music";
    }

    public async Task<string?> PlaySong(bool testLoop)
    {
        return await sharedPcmService.PlaySong(_model, testLoop);
    }
    
    public void DeleteSong()
    {
        _model.Track.Songs.Remove(_model);
        _model.Track.FixTrackSuffixes(_model.CanPlaySongs);
    }
    
    public async Task PauseSong()
    {
        await sharedPcmService.PauseSong();
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
    
    public string? GetCopyDetailsString()
    {
        MsuSongInfo output = new();
        if (!converterService.ConvertViewModel(_model, output) || !converterService.ConvertViewModel(_model.MsuPcmInfo, output.MsuPcmInfo))
        {
            return null;
        }
        
        output.TrackNumber = 0;
        output.TrackName = null;
        output.OutputPath = null;
        output.LastGeneratedDate = new DateTime();
        output.LastModifiedDate = new DateTime();
        output.IsComplete = false;
        output.ShowPanel = false;
        output.MsuPcmInfo.ClearFieldsForYaml();
        var yamlText = yamlService.ToYaml(output, YamlType.PascalIgnoreDefaults);
        
        return
            """
            # yaml-language-server: $schema=https://raw.githubusercontent.com/MattEqualsCoder/MSUScripter/main/Schemas/MsuSongInfo.json
            # Use Visual Studio Code with the YAML plugin from redhat for schema support (make sure the language is set to YAML)
            
            """ + yamlText;
    }

    public string? CopyDetailsFromString(string yamlText)
    {
        if (!yamlService.FromYaml<MsuSongInfo>(yamlText, YamlType.PascalIgnoreDefaults, out var yamlSongDetails, out _) || yamlSongDetails == null)
        {
            return "Invalid song details";
        }

        var originalProject = _model.Project;
        var originalTrack = _model.Track;
        var originalTrackName = _model.TrackName;
        var originalTrackNumber = _model.TrackNumber;
        var originalIsAlt = _model.IsAlt;
        var originalCanPlaySongs = _model.CanPlaySongs;
        var originalOutputPath = _model.OutputPath;
        _model.MsuPcmInfo.SubChannels.Clear();
        _model.MsuPcmInfo.SubTracks.Clear();
            
        if (!converterService.ConvertViewModel(yamlSongDetails, _model) || !converterService.ConvertViewModel(yamlSongDetails.MsuPcmInfo, _model.MsuPcmInfo))
        {
            return "Invalid song details";
        }

        _model.OutputPath = originalOutputPath;
            
        _model.ApplyCascadingSettings(originalProject, originalTrack, originalIsAlt, originalCanPlaySongs, true, true);
        return null;
    }
    
    public void IgnoreMsuPcmError()
    {
        if (string.IsNullOrEmpty(_model.OutputPath)) return;
        _model.Project.IgnoreWarnings.Add(_model.OutputPath);
    }
    
    public Task<GeneratePcmFileResponse> GeneratePcmFile(bool asPrimary, bool asEmpty)
    {
        return sharedPcmService.GeneratePcmFile(_model, asPrimary, asEmpty);
    }
    
    public void AnalyzeAudio()
    {
        _model.AverageAudio = "Running";
        _model.PeakAudio = null;
        
        ITaskService.Run(async () =>
        {
            await PauseSong();

            var generateResponse = await GeneratePcmFile(false, false);
            if (!generateResponse.Successful)
            {
                _model.AverageAudio = "Error generating PCM";
                _model.PeakAudio = null;
            }

            if (!string.IsNullOrEmpty(_model.OutputPath))
            {
                var output = await audioAnalysisService.AnalyzeAudio(_model.OutputPath);

                if (output is { AvgDecibels: not null, MaxDecibels: not null })
                {
                    _model.AverageAudio = $"Average: {Math.Round(output.AvgDecibels.Value, 2)}db";
                    _model.PeakAudio = $"Peak: {Math.Round(output.MaxDecibels.Value, 2)}db";
                }
                else
                {
                    _model.AverageAudio = "Error analyzing audio";
                    _model.PeakAudio = null;
                }
            }
            else
            {
                _model.AverageAudio = "Error generating PCM";
                _model.PeakAudio = null;
            }
        });
    }
}