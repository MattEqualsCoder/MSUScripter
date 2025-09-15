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
using SoundFlow.Interfaces;
using SoundFlow.Providers;
using File = System.IO.File;

namespace MSUScripter.Services;

public class AudioSampleRateResponse
{
    public int SampleRate { get; set; } = 44100;
    public bool Successful { get; set; }
}

public class AudioAnalysisService(
    IAudioPlayerService audioPlayerService,
    MsuPcmService msuPcmService,
    StatusBarService statusBarService,
    PythonCompanionService pythonCompanionService,
    ILogger<AudioAnalysisService> logger)
{
    public async Task AnalyzePcmFiles(AudioAnalysisViewModel audioAnalysis, CancellationToken ct = new())
    {
        var project = audioAnalysis.Project;
        
        await Parallel.ForEachAsync(audioAnalysis.Rows,
            new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct },
            async (song, _) =>
            {
                await AnalyzePcmFile(project, song);
                audioAnalysis.SongsCompleted++;
            });

        if (project?.BasicInfo.IsMsuPcmProject == true)
        {
            msuPcmService.SaveGenerationCache(project);    
        }
    }
    
    public async Task AnalyzePcmFile(MsuProject? project, AudioAnalysisSongViewModel song)
    {
        if (string.IsNullOrEmpty(song.Path))
        {
            song.WarningMessage = "No output path for the song";
            return;
        }
        else if (project?.BasicInfo.IsMsuPcmProject != true && !File.Exists(song.Path))
        {
            song.WarningMessage = "PCM file missing";
            return;
        }
        else if (project?.BasicInfo.IsMsuPcmProject == true && song.MsuSongInfo == null)
        {
            song.WarningMessage = "Song information missing";
            return;
        }
        
        // Regenerate the pcm file if it has updates that have been made to it
        if (project?.BasicInfo.IsMsuPcmProject == true && song.MsuSongInfo != null)
        {
            var response = await msuPcmService.CreatePcm(project, song.MsuSongInfo, false, true, true);
            if (!response.Successful)
            {
                song.WarningMessage = File.Exists(song.Path)
                    ? "Could not generate new PCM file"
                    : "PCM file missing and could not be generated";
            }
        }
            
        var data = await AnalyzeAudio(song.Path);
        song.ApplyAudioAnalysis(data);
        logger.LogInformation("Analysis for pcm file {File} complete", song.Path);
    }

    public async Task<AudioSampleRateResponse> GetAudioSampleRateAsync(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return new AudioSampleRateResponse
            {
                Successful = false,
            };
        }

        List<string> incompatibleFileTypes = [".ogg"];

        if (incompatibleFileTypes.Contains(new FileInfo(path).Extension.ToLower()))
        {
            logger.LogInformation("AudioSampleRate Incompatible file {File}. Assuming 44100.", path);
            return new AudioSampleRateResponse
            {
                Successful = false,
            };
        }

        if (OperatingSystem.IsLinux())
        {
            var response = await pythonCompanionService.GetSampleRateAsync(new GetSampleRateRequest()
            {
                File = path
            });
            
            if (response.Successful)
            {
                logger.LogInformation("Successfully retrieved sample rate of {Rate} for {File}", response.SampleRate, path);
            }
            else
            {
                logger.LogError("Failed to retrieve sample rate for {File}. Assuming 44100.", path);
            }
            
            return new AudioSampleRateResponse
            {
                Successful = response.Successful,
                SampleRate = response.Successful ? response.SampleRate : 44100,
            };
        }
        
        try
        {
            var mp3 = new AudioFileReader(path);
            logger.LogInformation("Successfully retrieved sample rate of {Rate} for {File}", mp3.WaveFormat.SampleRate, path);
            return new AudioSampleRateResponse
            {
                Successful = true,
                SampleRate = mp3.WaveFormat.SampleRate
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to retrieve sample rate for {File}. Assuming 44100.", path);
            return new AudioSampleRateResponse
            {
                Successful = false,
            };
        }
        
    }

    public int GetAudioStartingSample(string path)
    {
        logger.LogInformation("GetAudioStartingSample");
        try
        {
            using var reader = new AudioReader(path);
            
            var totalSampleCount = 0;
            int samples;
            var readBuffer = new float[10000];
            var quit = false;

            var threshold = .0003;
            do
            {
                samples = reader.Read(readBuffer);
            
                for (var i = 0; i < samples; i++)
                {
                    if (Math.Abs(readBuffer[i]) > threshold)
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
            return totalSampleCount / reader.Divider;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to get audio samples for file");
            return -1;
        }
    }

    public int GetAudioEndingSample(string path)
    {
        logger.LogInformation("GetAudioEndingSample");
        try
        {
            using var reader = new AudioReader(path);
            
            int samples;
            var readBuffer = new float[10000];
            var lastLoudSample = 0;
            
            var threshold = .0003;
            do
            {
                samples = reader.Read(readBuffer);

                for (var i = 0; i < samples; i++)
                {
                    if (Math.Abs(readBuffer[i]) > threshold)
                    {
                        lastLoudSample++;
                    }
                }
                
            } while (samples == readBuffer.Length);

            statusBarService.UpdateStatusBar("Retrieved Ending Samples");
            return lastLoudSample / reader.Divider;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to get audio samples for file");
            return -1;
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

        int samples;
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
            AvgDecibels = ConvertToDecibel(average),
            MaxDecibels = ConvertToDecibel(maxPeak)
        };
    }

    private double ConvertToDecibel(float value)
    {
        return Math.Round(20 * Math.Log10(Math.Abs(value)), 4);
    }

    private double ConvertToDecibel(double value)
    {
        return Math.Round(20 * Math.Log10(Math.Abs(value)), 4);
    }
}

public class AudioReader : IDisposable
{
    private readonly AudioFileReader? _windowsReader;
    private readonly ISoundDataProvider? _linuxReader;

    public AudioReader(string file)
    {
        if (OperatingSystem.IsWindows())
        {
            _windowsReader = new AudioFileReader(file);
            Channels = _windowsReader.WaveFormat.Channels;
            BytesPerSample = _windowsReader.WaveFormat.BitsPerSample / 8;
            Divider = Channels;
        }
        else
        {
            _linuxReader = new StreamDataProvider(File.OpenRead(file));
            Divider = 1;
        }
    }

    public int Read(float[] buffer)
    {
        if (OperatingSystem.IsWindows())
        {
            return _windowsReader!.Read(buffer, 0, buffer.Length);
        }
        else
        {
            return _linuxReader!.ReadBytes(buffer);
        }
    }
    
    public int Channels { get; init; }
    public int BytesPerSample { get; init; }
    public int BytesPerFrame => BytesPerSample * Channels;
    public int Divider { get; init; }

    public void Dispose()
    {
        _windowsReader?.Dispose();
        _linuxReader?.Dispose();
    }
}