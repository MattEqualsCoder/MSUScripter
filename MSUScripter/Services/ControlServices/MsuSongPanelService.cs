using System;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using MSUScripter.Configs;
using MSUScripter.Models;

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

    public void PlaySong(MsuProject project, MsuSongInfo song, bool testLoop)
    {
        var response = sharedPcmService.PlaySong(project, song, testLoop);
    }
    
    public void GeneratePcm(MsuProject project, MsuSongInfo song, bool asPrimary, bool asEmpty)
    {
        var response = sharedPcmService.GeneratePcmFile(project, song, asPrimary, asEmpty);
    }
    
    public async Task<AnalysisDataOutput?> AnalyzeAudio(MsuProject project, MsuSongInfo song)
    {
        if (string.IsNullOrEmpty(song.OutputPath))
        {
            return null;
        }
        
        await sharedPcmService.PauseSong();
        var response = await sharedPcmService.GeneratePcmFile(project, song, false, false);
        if (!response.Successful)
        {
            return null;
        }
        
        var output = await audioAnalysisService.AnalyzeAudio(song.OutputPath);
        return output;
    }
}