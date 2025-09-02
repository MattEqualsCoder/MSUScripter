using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaControls.Controls;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MsuSongPanelService(ConverterService converterService, YamlService yamlService, AudioMetadataService audioMetadataService, SharedPcmService sharedPcmService, AudioAnalysisService audioAnalysisService) : ControlService
{
    public string? GetMsuPcmInfoCopyText(MsuSongMsuPcmInfo msuPcmInfo)
    {
        MsuSongMsuPcmInfo output = new();
        if (!converterService.ConvertViewModel(msuPcmInfo, output))
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
        yamlService.FromYaml<MsuSongMsuPcmInfo>(yaml, YamlType.PascalIgnoreDefaults, out var data, out var error);
        return data;
    }
    
    public MsuSongMsuPcmInfo? DuplicateMsuPcmInfo(MsuSongMsuPcmInfo info)
    {
        MsuSongMsuPcmInfo output = new();
        return !converterService.ConvertViewModel(info, output) ? null : info;
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
        
        ITaskService.Run(() =>
        {
            model.SetSampleRate(44100);
            var sampleRate = GetSampleRate(model.Project, path);
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
        
        List<string> files = [];
        foreach (var treeItem in model.TreeItems.Where(x => x.MsuPcmInfo != null))
        {
            if (!string.IsNullOrEmpty(treeItem.MsuPcmInfo!.File) && File.Exists(treeItem.MsuPcmInfo!.File))
            {
                files.Add(treeItem.MsuPcmInfo.File);
            }

            var subTracks = treeItem.ChildrenTreeData.FirstOrDefault();
            var subChannels = treeItem.ChildrenTreeData.LastOrDefault();

            if (subChannels != null && subTracks != null && subTracks != subChannels && subTracks.ChildrenTreeData.Count > 0 && subChannels.ChildrenTreeData.Count > 0)
            {
                hasBothSubTracksAndSubChannels = true;
            }
        }

        if (files.Count == 0)
        {
            model.UpdateTrackWarnings(false, false, false);
            return;
        }

        files = files.Distinct().ToList();

        if (files.Count > 1)
        {
            model.UpdateTrackWarnings(false, true, hasBothSubTracksAndSubChannels);
            return;
        }
        
        ITaskService.Run(() =>
        {
            var path = files.First();
            var sampleRate = GetSavedSampleInfo(project, path) ?? GetSampleRate(project, path);
            if (model.CurrentSongInfo == song)
            {
                model.UpdateTrackWarnings(sampleRate != 44100, false, hasBothSubTracksAndSubChannels);
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

    private int GetSampleRate(MsuProject project, string path)
    {
        var sampleRate = audioAnalysisService.GetAudioSampleRate(path);
        var fileInfo = new FileInfo(path);
        project.SampleRates[path] = new FileSampleInfo
        {
            FileLength = fileInfo.Length,
            SampleRate = sampleRate
        };
        return sampleRate;
    }

    public Task<string?> PlaySong(MsuProject project, MsuSongInfo song, bool testLoop)
    {
        return sharedPcmService.PlaySong(project, song, testLoop);
    }
    
    public Task<GeneratePcmFileResponse> GeneratePcm(MsuProject project, MsuSongInfo song, bool asPrimary, bool asEmpty)
    {
        return sharedPcmService.GeneratePcmFile(project, song, asPrimary, asEmpty, false);
    }
    
    public async Task<AnalysisDataOutput?> AnalyzeAudio(MsuProject project, MsuSongInfo song)
    {
        if (string.IsNullOrEmpty(song.OutputPath))
        {
            return null;
        }
        
        await sharedPcmService.PauseSong();
        var response = await sharedPcmService.GeneratePcmFile(project, song, false, false, false);
        if (!response.Successful)
        {
            return null;
        }
        
        var output = await audioAnalysisService.AnalyzeAudio(song.OutputPath);
        return output;
    }
}