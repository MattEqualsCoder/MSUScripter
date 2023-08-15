using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services;

public class AudioAnalysisService
{
    private AudioService _audioService;
    private MsuPcmService _msuPcmService;
    private ConverterService _converterService;
    private ILogger<AudioAnalysisService> _logger;

    public AudioAnalysisService(AudioService audioService, MsuPcmService msuPcmService, ConverterService converterService, ILogger<AudioAnalysisService> logger)
    {
        _audioService = audioService;
        _msuPcmService = msuPcmService;
        _converterService = converterService;
        _logger = logger;
    }

    public void AnalyzePcmFiles(MsuProjectViewModel projectViewModel, AudioAnalysisViewModel audioAnalysis, CancellationToken ct = new())
    {
        var project = _converterService.ConvertProject(projectViewModel);
        
        Parallel.ForEach(audioAnalysis.Rows,
            new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct },
            song =>
            {
                AnalyzePcmFile(project, song);
                audioAnalysis.SongsCompleted++;
            }); 
    }

    public void AnalyzePcmFile(MsuProjectViewModel projectViewModel, AudioAnalysisSongViewModel song)
    {
        var project = _converterService.ConvertProject(projectViewModel);
        AnalyzePcmFile(project, song);
    }
    
    public void AnalyzePcmFile(MsuProject project, AudioAnalysisSongViewModel song)
    {
        // Regenerate the pcm file if it has updates that have been made to it
        if (project.BasicInfo.IsMsuPcmProject && song.OriginalViewModel != null && song.OriginalViewModel.HasChangesSince(song.OriginalViewModel.LastGeneratedDate) && song.OriginalViewModel.HasFiles())
        {
            _logger.LogInformation("PCM file {File} out of date, regenerating", song.Path);
            GeneratePcmFile(project, song.OriginalViewModel);
            
        }
        
        var data = _audioService.AnalyzeAudio(song.Path);
        song.ApplyAudioAnalysis(data);
        _logger.LogInformation("Analysis for pcm file {File} complete", song.Path);
    }
    
    private void GeneratePcmFile(MsuProject project, MsuSongInfoViewModel songModel)
    {
        var song = new MsuSongInfo();
        _converterService.ConvertViewModel(songModel, song);
        _converterService.ConvertViewModel(songModel.MsuPcmInfo, song.MsuPcmInfo);
        if (!_msuPcmService.CreatePcm(project, song, out var message))
        {
            _logger.LogInformation("PCM file {File} failed to regenerate: {Error}", song.OutputPath, message);
        }
        else
        {
            songModel.LastGeneratedDate = DateTime.Now;
            _logger.LogInformation("PCM file {File} regenerated successfully", song.OutputPath);
        }
    }
}