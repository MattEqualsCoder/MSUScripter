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
using File = System.IO.File;

namespace MSUScripter.Services;

public class AudioAnalysisService(
    IAudioPlayerService audioPlayerService,
    MsuPcmService msuPcmService,
    StatusBarService statusBarService,
    ConverterService converterService,
    ILogger<AudioAnalysisService> logger)
{
    public async Task AnalyzePcmFiles(MsuProjectViewModel projectViewModel, AudioAnalysisViewModel audioAnalysis, CancellationToken ct = new())
    {
        var project = converterService.ConvertProject(projectViewModel);
        
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
        var project = converterService.ConvertProject(projectViewModel);
        await AnalyzePcmFile(project, song);
    }
    
    public async Task AnalyzePcmFile(MsuProject project, AudioAnalysisSongViewModel song)
    {
        if (string.IsNullOrEmpty(song.Path))
        {
            song.WarningMessage = "No output path for the song";
            return;
        }
        else if (!project.BasicInfo.IsMsuPcmProject && !File.Exists(song.Path))
        {
            song.WarningMessage = "PCM file missing";
            return;
        }
        else if (project.BasicInfo.IsMsuPcmProject && song.OriginalViewModel?.HasFiles() != true && !File.Exists(song.Path))
        {
            song.WarningMessage = "No input files specified for PCM file";
            return;
        }
        
        // Regenerate the pcm file if it has updates that have been made to it
        if (project.BasicInfo.IsMsuPcmProject && song.OriginalViewModel != null && song.OriginalViewModel.HasFiles() && (song.OriginalViewModel.HasChangesSince(song.OriginalViewModel.LastGeneratedDate) || !File.Exists(song.Path)))
        {
            logger.LogInformation("PCM file {File} out of date, regenerating", song.Path);
            if (!GeneratePcmFile(project, song.OriginalViewModel))
            {
                song.WarningMessage = "Could not generate new PCM file";
            }
        }
            
        var data = await AnalyzeAudio(song.Path);
        song.ApplyAudioAnalysis(data);
        logger.LogInformation("Analysis for pcm file {File} complete", song.Path);
    }
    
    private bool GeneratePcmFile(MsuProject project, MsuSongInfoViewModel songModel)
    {
        var song = new MsuSongInfo();
        converterService.ConvertViewModel(songModel, song);
        converterService.ConvertViewModel(songModel.MsuPcmInfo, song.MsuPcmInfo);
        msuPcmService.CreatePcm(false, project, song, out var message, out var generated);
        if (!generated)
        {
            logger.LogInformation("PCM file {File} failed to regenerate: {Error}", song.OutputPath, message);
        }
        else
        {
            songModel.LastGeneratedDate = DateTime.Now;
            logger.LogInformation("PCM file {File} regenerated successfully", song.OutputPath);
        }

        return generated;
    }

    public int GetAudioSampleRate(string? path)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return 44100;
        }

        List<string> incompatibleFileTypes = [".ogg"];

        if (incompatibleFileTypes.Contains(new FileInfo(path).Extension.ToLower()))
        {
            logger.LogInformation("AudioSampleRate Incompatible file {File}", path);
            return 44100;
        }
        
        try
        {
            var mp3 = new AudioFileReader(path);
            return mp3.WaveFormat.SampleRate;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to retrieve audio sample rate");
            return 44100;
        }
        
    }

    public int GetAudioStartingSample(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("This is only supported on Windows");
        }

        logger.LogInformation("GetAudioStartingSample");
        try
        {
            var totalSampleCount = 0;
            var samples = 0;
            var readBuffer = new float[10000];
            var quit = false;
            var mp3 = new AudioFileReader(path);
            do
            {
                samples = mp3.Read(readBuffer, 0, readBuffer.Length);
            
                for (var i = 0; i < samples; i++)
                {
                    if (Math.Abs(readBuffer[i]) > .0003)
                    {
                        totalSampleCount += i;
                        quit = true;
                        break;
                    }
                }

                if (!quit)
                {
                    totalSampleCount += samples;    
                }
            
            } while (!quit && samples == readBuffer.Length);

            statusBarService.UpdateStatusBar("Retrieved Starting Samples");
            return totalSampleCount / mp3.WaveFormat.Channels;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to get audio samples for file");
            throw;
        }
        
    }

    public int GetAudioEndingSample(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("This is only supported on Windows");
        }

        logger.LogInformation("GetAudioEndingSample");
        try
        {
            var samples = 0;
            var readBuffer = new float[10000];
            var mp3 = new AudioFileReader(path);
            int lastLoudSample = 0;
            do
            {
                samples = mp3.Read(readBuffer, 0, readBuffer.Length);

                for (var i = 0; i < samples; i++)
                {
                    if (Math.Abs(readBuffer[i]) > .0003)
                    {
                        lastLoudSample++;
                    }
                }

            } while (samples == readBuffer.Length);

            statusBarService.UpdateStatusBar("Retrieved Ending Samples");
            return lastLoudSample / mp3.WaveFormat.Channels;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to get audio samples for file");
            throw;
        }

    }

    public async Task<AnalysisDataOutput> AnalyzeAudio(string path)
    {
        await audioPlayerService.StopSongAsync(path, true);
        
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
        await using var fs = File.OpenRead(path);

        var duration = fs.Length;

        if (duration < 50000)
        {
            return new AnalysisDataOutput();
        }
        
        await using var rs = new RawSourceWaveStream(fs, new WaveFormat(44100, 2));
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