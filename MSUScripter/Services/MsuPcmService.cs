using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using Newtonsoft.Json;

namespace MSUScripter.Services;

public class MsuPcmService
{
    private readonly ILogger<MsuPcmService> _logger;
    private readonly ConverterService _converterService;
    private readonly Settings _settings;

    public MsuPcmService(ILogger<MsuPcmService> logger, ConverterService converterService, Settings settings)
    {
        _logger = logger;
        _converterService = converterService;
        _settings = settings;
    }

    public bool CreateTempPcm(MsuProject project, string inputFile, out string outputPath, out string? message, out bool generated)
    {
        outputPath = GetTempFilePath();
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
        return CreatePcm(project, new MsuSongInfo()
            {
                TrackNumber = project.MsuType.Tracks.First().Number,
                OutputPath = outputPath,
                MsuPcmInfo = new MsuSongMsuPcmInfo()
                {
                    Output = outputPath,
                    File = inputFile
                }
            }, out message, out generated);
    }

    public bool CreatePcm(MsuProject project, MsuSongInfo song, out string? message, out bool generated)
    {
        
        if (string.IsNullOrEmpty(song.OutputPath))
        {
            message = $"Track #{song.TrackNumber} - Missing output PCM path";
            generated = false;
            return false;
        }

        var jsonDirectory = Path.Combine(Program.GetBaseFolder(), "msupcmtemp");
        if (!Directory.Exists(jsonDirectory))
        {
            Directory.CreateDirectory(jsonDirectory);
        }
            
        var msu = new FileInfo(project.MsuPath);
        var guid = Guid.NewGuid().ToString("N");
        var jsonPath = Path.Combine(jsonDirectory, msu.Name.Replace(msu.Extension, $"-msupcm-temp-{guid}.json"));
        try
        {
            ExportMsuPcmTracksJson(project, song, jsonPath);
            
            var msuPath = new FileInfo(project.MsuPath).DirectoryName;
            var relativePath = Path.GetRelativePath(msuPath!, song.OutputPath);
        
            if (!File.Exists(jsonPath))
            {
                message = $"Track #{song.TrackNumber} - {relativePath} - Invalid MsuPcm++ json was not able to be created";
                generated = false;
                return false;
            }

            if (!ValidateMsuPcmInfo(song.MsuPcmInfo, out message, out var numFiles))
            {
                message = $"Track #{song.TrackNumber} - {relativePath} - {message!.ReplaceLineEndings("")}";
                File.Delete(jsonPath);
                generated = false;
                return false;
            }

            if (numFiles == 0)
            {
                message = $"Track #{song.TrackNumber} - {relativePath} - No input files specified";
                File.Delete(jsonPath);
                generated = false;
                return false;
            }

            var file = new FileInfo(song.OutputPath);
            var lastModifiedDate = file.Exists ? file.LastWriteTime : DateTime.MinValue;

            if (RunMsuPcm(jsonPath, out message))
            {
                if (!ValidatePcm(song.OutputPath, out message))
                {
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message?.ReplaceLineEndings("")}";
                    File.Delete(jsonPath);
                    generated = true;
                    return false;
                }
                message = $"Track #{song.TrackNumber} - {relativePath} - Success!";
                File.Delete(jsonPath);
                generated = true;
                return true;
            }

            file = new FileInfo(song.OutputPath);
            var newModifiedDate = file.Exists ? file.LastWriteTime : DateTime.MinValue;
            generated = newModifiedDate > lastModifiedDate;
            
            if (generated)
            {
                message = $"Track #{song.TrackNumber} - {relativePath} - PCM Generated with msupcm++ warning: {message.ReplaceLineEndings("")}";
            }
            else
            {
                message = $"Track #{song.TrackNumber} - {relativePath} - {message.ReplaceLineEndings("")}";
            }
           
            File.Delete(jsonPath);
            
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating PCM file for Track #{TrackNum} - {SongPath}", song.TrackNumber, song.OutputPath);
            message = $"Track #{song.TrackNumber} - {song.OutputPath} - Unknown error";
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }

            generated = false;
            return false;
        }
    }

    public bool CreateEmptyPcm(MsuSongInfo song)
    {
        if (string.IsNullOrEmpty(song.OutputPath))
        {
            return false;
        }

        try
        {
            if (File.Exists(song.OutputPath))
            {
                File.Delete(song.OutputPath);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not delete output file");
            return false;
        }

        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MSUScripter.empty.pcm");
        if (stream == null)
            return false;

        try
        {
            using var fileStream = File.Create(song.OutputPath);
            stream.CopyTo(fileStream);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not copy empty pcm file");
            return false;
        }

        return true;

    }

    public bool ValidateMsuPcmInfo(MsuSongMsuPcmInfo info, out string? error, out int numFiles)
    {
        numFiles = 0;
        
        if (!string.IsNullOrEmpty(info.File))
        {
            if (!File.Exists(info.File))
            {
                error = $"{info.File} not found";
                return false;    
            }

            numFiles = 1;
        }

        foreach (var subTrack in info.SubTracks)
        {
            if (!ValidateMsuPcmInfo(subTrack, out error, out var numSubFiles))
                return false;
            numFiles += numSubFiles;
        }
        
        foreach (var subChannel in info.SubChannels)
        {
            if (!ValidateMsuPcmInfo(subChannel, out error, out var numSubFiles))
                return false;
            numFiles += numSubFiles;
        }

        error = null;
        return true;
    }

    public bool ValidatePcm(string path, out string? error)
    {
        var testBytes = new byte[8];
        using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            reader.Read(testBytes, 0, 8);
        }

        if (Encoding.UTF8.GetString(testBytes, 0, 4) != "MSU1")
        {
            error = "Bad Header";
            return false;
        }

        var loop = BitConverter.ToInt32(testBytes, 4);
        var totalSamples = (new FileInfo(path).Length - 8) / 4;

        if (loop < totalSamples)
        {
            error = null;
            return true;
        }
        else
        {
            error = "Bad loop point specified";
            return false;
        }
    }

    public bool RunMsuPcm(string trackJson, out string error)
    {
        IsGeneratingPcm = true;
        var toReturn = RunMsuPcmInternal("\"" + trackJson + "\"", out _, out error);
        IsGeneratingPcm = false;
        return toReturn;
    }

    public bool ValidateMsuPcmPath(string msuPcmPath, out string error)
    {
        var successful = RunMsuPcmInternal("-v", out var result, out error, msuPcmPath);
        return successful && result.StartsWith("msupcm v");
    }

    private bool RunMsuPcmInternal(string innerCommand, out string result, out string error, string? msuPcmPath = null)
    {
        msuPcmPath ??= _settings.MsuPcmPath;
        if (string.IsNullOrEmpty(msuPcmPath) ||
            !File.Exists(msuPcmPath))
        {
            result = "";
            error = "MsuPcm++ path not specified or is invalid";
            return false;
        }
        
        try
        {
            var msuPcmFile = new FileInfo(msuPcmPath);
            
            ProcessStartInfo procStartInfo;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var command = msuPcmFile.Name + " " + innerCommand;
                procStartInfo= new ProcessStartInfo("cmd", "/c " + command)
                {
                    WorkingDirectory = msuPcmFile.DirectoryName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                procStartInfo= new ProcessStartInfo(msuPcmFile.FullName)
                {
                    Arguments = innerCommand,
                    WorkingDirectory = msuPcmFile.DirectoryName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            // wrap IDisposable into using (in order to release hProcess) 
            using var process = new Process();
            process.StartInfo = procStartInfo;
            process.Start();

            // Add this: wait until process does its work
            process.WaitForExit();

            // and only then read the result
            result = process.StandardOutput.ReadToEnd().Replace("\0", "").Trim();
            error = process.StandardError.ReadToEnd().Replace("\0", "").Trim();
            
            if (string.IsNullOrEmpty(error)) return true;
            _logger.LogError("Error running MsuPcm++: {Error}", error);
            return false;
        }
        catch (Exception e)
        {
            result = "";
            error = "Unknown error running MsuPcm++";
            _logger.LogError(e, "Unknown error running MsuPcm++");
            return false;
        }
    }
    
    public string? ExportMsuPcmTracksJson(MsuProject project, MsuSongInfo? singleSong = null, string? exportPath = null)
    {
        var msu = new FileInfo(project.MsuPath);
        
        if (string.IsNullOrEmpty(exportPath))
        {
            exportPath = msu.FullName.Replace(msu.Extension, "-tracks.json");
        }
        
        var output = new MsuPcmPlusPlusConfig()
        {
            Game = project.BasicInfo.Game,
            Pack = project.BasicInfo.PackName,
            Artist = project.BasicInfo.Artist,
            Url = string.IsNullOrEmpty(project.BasicInfo.Url) ? null : new Uri(project.BasicInfo.Url),
            Output_prefix = msu.FullName.Replace(msu.Extension, ""),
            Normalization = project.BasicInfo.Normalization,
            Dither = project.BasicInfo.Dither,
            Verbosity = 2,
            Keep_temps = false,
            First_track = project.Tracks.Min(x => x.TrackNumber),
            Last_track = project.Tracks.Max(x => x.TrackNumber)
        };
        var tracks = new List<Track>();

        var songs = singleSong == null 
            ? project.Tracks.SelectMany(x => x.Songs).ToList()
            : new List<MsuSongInfo>() { singleSong };
        
        foreach (var song in songs)
        {
            if (_converterService.ConvertMsuPcmTrackInfo(song.MsuPcmInfo, false, false) is not Track track) continue;
            track.Output = song.OutputPath;
            track.Track_number = song.TrackNumber;
            track.Title = song.TrackName;
            tracks.Add(track);
        }

        output.Tracks = tracks;
        var json = JsonConvert.SerializeObject(output, Formatting.Indented);
        File.WriteAllText(exportPath, json);
        return exportPath;
    }
    
    public bool IsGeneratingPcm { get; private set; }

    private string GetTempFilePath()
    {
        var basePath = Directory.GetCurrentDirectory();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            basePath = Environment.ExpandEnvironmentVariables($"%LocalAppData%{Path.DirectorySeparatorChar}MSUScripter");
        }

        return Path.Combine(basePath, "tmp-pcm.pcm");
    }
}