using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.ViewModels;
using NAudio.Wave;

namespace MSUScripter.Services;

public class AudioAnalysisService
{
    private IAudioPlayerService _audioPlayerService;
    private MsuPcmService _msuPcmService;
    private ConverterService _converterService;
    private ILogger<AudioAnalysisService> _logger;

    public AudioAnalysisService(IAudioPlayerService audioPlayerService, MsuPcmService msuPcmService, ConverterService converterService, ILogger<AudioAnalysisService> logger)
    {
        _audioPlayerService = audioPlayerService;
        _msuPcmService = msuPcmService;
        _converterService = converterService;
        _logger = logger;
    }

    public async Task AnalyzePcmFiles(MsuProjectViewModel projectViewModel, AudioAnalysisViewModel audioAnalysis, CancellationToken ct = new())
    {
        var project = _converterService.ConvertProject(projectViewModel);
        
        await Parallel.ForEachAsync(audioAnalysis.Rows,
            new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct },
            async (song, token) =>
            {
                await AnalyzePcmFile(project, song);
                audioAnalysis.SongsCompleted++;
            }); 
    }

    public async Task AnalyzePcmFile(MsuProjectViewModel projectViewModel, AudioAnalysisSongViewModel song)
    {
        var project = _converterService.ConvertProject(projectViewModel);
        await AnalyzePcmFile(project, song);
    }
    
    public async Task AnalyzePcmFile(MsuProject project, AudioAnalysisSongViewModel song)
    {
        if (string.IsNullOrEmpty(song.Path))
        {
            song.HasWarning = true;
            song.WarningMessage = "No output path for the song";
            return;
        }
        else if (!project.BasicInfo.IsMsuPcmProject && !File.Exists(song.Path))
        {
            song.HasWarning = true;
            song.WarningMessage = "PCM file missing";
            return;
        }
        else if (project.BasicInfo.IsMsuPcmProject && song.OriginalViewModel?.HasFiles() != true && !File.Exists(song.Path))
        {
            song.HasWarning = true;
            song.WarningMessage = "No input files specified for PCM file";
            return;
        }
        
        // Regenerate the pcm file if it has updates that have been made to it
        if (project.BasicInfo.IsMsuPcmProject && song.OriginalViewModel != null && song.OriginalViewModel.HasFiles() && (song.OriginalViewModel.HasChangesSince(song.OriginalViewModel.LastGeneratedDate) || !File.Exists(song.Path)))
        {
            _logger.LogInformation("PCM file {File} out of date, regenerating", song.Path);
            if (!GeneratePcmFile(project, song.OriginalViewModel))
            {
                song.HasWarning = true;
                song.WarningMessage = "Could not generate new PCM file";
            }
        }
            
        var data = await AnalyzeAudio(song.Path);
        song.ApplyAudioAnalysis(data);
        _logger.LogInformation("Analysis for pcm file {File} complete", song.Path);
    }
    
    private bool GeneratePcmFile(MsuProject project, MsuSongInfoViewModel songModel)
    {
        var song = new MsuSongInfo();
        _converterService.ConvertViewModel(songModel, song);
        _converterService.ConvertViewModel(songModel.MsuPcmInfo, song.MsuPcmInfo);
        _msuPcmService.CreatePcm(project, song, out var message, out var generated);
        if (!generated)
        {
            _logger.LogInformation("PCM file {File} failed to regenerate: {Error}", song.OutputPath, message);
        }
        else
        {
            songModel.LastGeneratedDate = DateTime.Now;
            _logger.LogInformation("PCM file {File} regenerated successfully", song.OutputPath);
        }

        return generated;
    }

    public void GetMp3StartingSample(string path)
    {
        var totalSampleCount = 0;
        var samples = 0;
        var readBuffer = new float[2000];
        var mp3 = new Mp3FileReader(path);
        var sampleProvider = mp3.ToSampleProvider();
        do
        {
            samples = sampleProvider.Read(readBuffer, 0, readBuffer.Length);
            for (var i = 0; i < samples; i++)
            {
                var decibel = ConvertToDecibel(Math.Pow(readBuffer[i], 2));
                _logger.LogInformation("{Index}: {Amp}", i + totalSampleCount, decibel);
            }
            totalSampleCount += samples;
        } while (samples == readBuffer.Length);

    }
    
    public async Task<AnalysisDataOutput> AnalyzeAudio(string path)
    {
        await _audioPlayerService.StopSongAsync(path, true);
        
        try
        {
            using var reader = new BinaryReader(new FileStream(path, FileMode.Open));
        }
        catch
        {
            return new AnalysisDataOutput();
        }

        // Initialize the sample reader
        var readBuffer = new float[2000];
        using var fs = File.OpenRead(path);
        using var rs = new RawSourceWaveStream(fs, new WaveFormat(44100, 2));
        var sampleProvider = rs.ToSampleProvider();
        sampleProvider.Read(readBuffer, 0, 8);
        
        float maxPeak = 0;
        double sum = 0;
        var totalSampleCount = 0;

        var samples = 0;
        do
        {
            samples = sampleProvider.Read(readBuffer, 0, readBuffer.Length);
            sum += readBuffer.Select(x => Math.Pow(x, 2)).Sum();
            totalSampleCount += samples;
            maxPeak = Math.Max(maxPeak, readBuffer.Max());
        } while (samples == readBuffer.Length);

        var average = Math.Sqrt(sum / totalSampleCount);
        
        return new AnalysisDataOutput()
        {
            AvgDecibals = ConvertToDecibel(average),
            MaxDecibals = ConvertToDecibel(maxPeak)
        };
    }

    public double ConvertToDecibel(float value)
    {
        return Math.Round(20 * Math.Log10(Math.Abs(value)), 4);
    }
    
    public double ConvertToDecibel(double value)
    {
        return Math.Round(20 * Math.Log10(Math.Abs(value)), 4);
    }
}