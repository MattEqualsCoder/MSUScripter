using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

// ReSharper disable once ClassNeverInstantiated.Global
public class MsuSongPanelService(
    ConverterService converterService,
    YamlService yamlService,
    AudioMetadataService audioMetadataService,
    MsuPcmService msuPcmService,
    IAudioPlayerService audioPlayerService,
    AudioAnalysisService audioAnalysisService,
    PythonCompanionService pythonCompanionService,
    SettingsService settingsService,
    ILogger<MsuSongPanelService> logger) : ControlService
{
    public string? GetMsuPcmInfoCopyText(MsuSongMsuPcmInfo msuPcmInfo)
    {
        MsuSongMsuPcmInfo output = new();
        if (!converterService.CloneModel(msuPcmInfo, output))
        {
            return null;
        }
        
        Sanitize(output);
        
        var yamlText = yamlService.ToYaml(output, YamlType.PascalIgnoreDefaults);
    
        return
            """
            # yaml-language-server: $schema=https://raw.githubusercontent.com/MattEqualsCoder/MSUScripter/main/Schemas/MsuSongMsuPcmInfo.json
            # Use Visual Studio Code with the YAML plugin from redhat for schema support (make sure the language is set to YAML)

            """ + yamlText;

        void Sanitize(MsuSongMsuPcmInfo subInfo)
        {
            subInfo.ShowPanel = false;
            subInfo.LastModifiedDate = DateTime.MinValue;
            subInfo.Output = null;
            foreach (var info in subInfo.SubChannels.Concat(subInfo.SubTracks))
            {
                Sanitize(info);
            }
        }
    }

    public MsuSongMsuPcmInfo? GetMsuPcmInfoFromText(string yaml)
    {
        yamlService.FromYaml<MsuSongMsuPcmInfo>(yaml, YamlType.PascalIgnoreDefaults, out var data, out _);
        return data;
    }
    
    public MsuSongMsuPcmInfo? DuplicateMsuPcmInfo(MsuSongMsuPcmInfo info)
    {
        MsuSongMsuPcmInfo output = new();
        return !converterService.CloneModel(info, output) ? null : output;
    }

    public AudioMetadata GetAudioMetadata(string filename)
    {
        return audioMetadataService.GetAudioMetadata(filename);
    }

    public void CheckSampleRate(MsuSongBasicPanelViewModel model)
    {
        var path = model.InputFilePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path) || model.Project == null)
        {
            model.SetSampleRate(44100);
            return;
        }

        var savedSampleRate = GetSavedSampleInfo(model.Project, path);
        if (savedSampleRate != null)
        {
            model.SetSampleRate(savedSampleRate.Value);
            return;
        }
        
        ITaskService.Run(async () =>
        {
            model.SetSampleRate(44100);
            var sampleRate = await GetSampleRateAsync(model.Project, path);
            if (path == model.InputFilePath)
            {
                model.SetSampleRate(sampleRate);
            }
        });
    }

    public void CheckFileErrors(MsuSongAdvancedPanelViewModel model)
    {
        var project = model.Project;
        var song = model.CurrentSongInfo;

        var hasBothSubTracksAndSubChannels = false;
        var hasSoloSubChannel = false;
        var hasSameParentChildType = false;
        var hasIgnoredFile = false;
        
        List<string> files = [];
        foreach (var treeItem in model.TreeItems.Where(x => x.MsuPcmInfo != null))
        {
            if (!string.IsNullOrEmpty(treeItem.MsuPcmInfo!.File) && File.Exists(treeItem.MsuPcmInfo!.File))
            {
                files.Add(treeItem.MsuPcmInfo.File);
            }

            var subTracks = treeItem.ChildrenTreeData.FirstOrDefault();
            var subChannels = treeItem.ChildrenTreeData.LastOrDefault();

            if (subChannels != null && subTracks != null && subTracks != subChannels &&
                subTracks.ChildrenTreeData.Count > 0 && subChannels.ChildrenTreeData.Count > 0)
            {
                hasBothSubTracksAndSubChannels = true;
                
                if (subChannels.ChildrenTreeData.Count == 1)
                {
                    hasSoloSubChannel = true;
                }
            }

            if (treeItem is { IsSubChannel: true, ParentTreeData.ParentTreeData.IsSubChannel: true } or { IsSubTrack: true, ParentTreeData.ParentTreeData.IsSubTrack: true })
            {
                hasSameParentChildType = true;
            }

            if (!string.IsNullOrEmpty(treeItem.MsuPcmInfo.File) &&
                (subTracks?.ChildrenTreeData.Count > 0 || subChannels?.ChildrenTreeData.Count > 0))
            {
                hasIgnoredFile = true;
            }
        }

        if (files.Count == 0)
        {
            model.UpdateTrackWarnings(false, false, false, false, false, false);
            return;
        }

        files = files.Distinct().ToList();

        if (files.Count > 1)
        {
            model.UpdateTrackWarnings(false, true, hasBothSubTracksAndSubChannels, hasSoloSubChannel, hasSameParentChildType, hasIgnoredFile);
            return;
        }
        
        ITaskService.Run(async() =>
        {
            var path = files.First();
            var sampleRate = GetSavedSampleInfo(project, path) ?? await GetSampleRateAsync(project, path);
            if (model.CurrentSongInfo == song)
            {
                model.UpdateTrackWarnings(sampleRate != 44100, false, hasBothSubTracksAndSubChannels, hasSoloSubChannel, hasSameParentChildType, hasIgnoredFile);
            }
        });
    }

    private int? GetSavedSampleInfo(MsuProject project, string path)
    {
        if (project.SampleRates.TryGetValue(path, out var sampleRate))
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length == sampleRate.FileLength)
            {
                return sampleRate.SampleRate;
            }
        }

        return null;
    }

    private async Task<int> GetSampleRateAsync(MsuProject project, string path)
    {
        var response = await audioAnalysisService.GetAudioSampleRateAsync(path);
        var fileInfo = new FileInfo(path);
        if (response.Successful)
        {
            project.SampleRates[path] = new FileSampleInfo
            {
                FileLength = fileInfo.Length,
                SampleRate = response.SampleRate
            };
        }
        return response.SampleRate;
    }

    public async Task<GeneratePcmFileResponse> PlaySong(MsuProject project, MsuSongInfo song, bool testLoop)
    {
        GeneratePcmFileResponse? generateResponse = null;
        
        if (song.HasAudioFiles())
        {
            generateResponse = await GeneratePcm(project, song, false, false);
            if (!generateResponse.GeneratedPcmFile)
            {
                return generateResponse;
            }
            
            if (string.IsNullOrEmpty(song.OutputPath) || !File.Exists(song.OutputPath))
            {
                return generateResponse;
            }
        }
        else if (!File.Exists(song.OutputPath))
        {
            return new GeneratePcmFileResponse(false, false, "PCM file not found", song.OutputPath);
        }

        var msuTypeTrackInfo = project.MsuType.Tracks.FirstOrDefault(x => x.Number == song.TrackNumber);

        if (await audioPlayerService.PlaySongAsync(song.OutputPath, testLoop, msuTypeTrackInfo?.NonLooping != true))
        {
            return new GeneratePcmFileResponse(generateResponse?.Successful ?? true, generateResponse?.GeneratedPcmFile ?? false, generateResponse?.Message ?? string.Empty, song.OutputPath);
        }

        return new GeneratePcmFileResponse(false, false, "Error playing PCM file", song.OutputPath);
    }
    
    public async Task<GeneratePcmFileResponse> GeneratePcm(MsuProject project, MsuSongInfo song, bool asPrimary, bool asEmpty)
    {
        await audioPlayerService.StopSongAsync(song.OutputPath);
        if (asEmpty)
        {
            return msuPcmService.CreateEmptyPcm(song);
        }
        return await msuPcmService.CreatePcm(project, song, asPrimary, false, true);
    }

    public MsuPcmJsonInfo GenerateSongTracksFile(MsuProject project, MsuSongInfo song, string path)
    {
        return msuPcmService.ExportMsuPcmTracksJson(project, song, path, song.OutputPath);
    }
    
    public async Task<AnalysisDataOutput?> AnalyzeAudio(MsuProject project, MsuSongInfo song)
    {
        if (string.IsNullOrEmpty(song.OutputPath))
        {
            return null;
        }
        
        await audioPlayerService.StopSongAsync(null, true);
        var response = await msuPcmService.CreatePcm(project, song, false, false, true);
        if (!response.Successful)
        {
            return null;
        }
        
        var output = await audioAnalysisService.AnalyzeAudio(song.OutputPath);
        return output;
    }

    public int DetectStartingSamples(string file)
    {
        return audioAnalysisService.GetAudioStartingSample(file);
    }
    
    public int DetectEndingSamples(string file)
    {
        return audioAnalysisService.GetAudioEndingSample(file);
    }

    public void LogError(Exception ex, string message)
    {
        logger.LogError(ex, "{Message}", message);
    }

    public bool CanGenerateMsuPcmFiles()
    {
        return msuPcmService.IsValid;
    }

    public bool CanGenerateVideos()
    {
        return pythonCompanionService.IsValid;
    }

    public bool CanRunPyMusicLooper()
    {
        return pythonCompanionService.IsValid;
    }

    public bool HasShownVolumeModifierWarning()
    {
        return settingsService.Settings.HasShownVolumeModifierWarning;
    }

    public void UpdateShownVolumeModifierWarning()
    {
        settingsService.Settings.HasShownVolumeModifierWarning = true;
        settingsService.TrySaveSettings();
    }
}