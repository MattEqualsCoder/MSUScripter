using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using Newtonsoft.Json;

namespace MSUScripter.Services;

public class MsuPcmService
{
    public static MsuPcmService Instance { get; private set; } = null!;

    private readonly ILogger<MsuPcmService> _logger;

    public MsuPcmService(ILogger<MsuPcmService> logger)
    {
        _logger = logger;
        Instance = this;
    }

    public bool CreatePcm(MsuProject project, MsuSongInfo song, out string? message)
    {
        try
        {
            if (string.IsNullOrEmpty(song.OutputPath))
            {
                message = $"Track #{song.TrackNumber} - Missing output PCM path";
                return false;
            }
            
            var msu = new FileInfo(project.MsuPath);
            var jsonPath = msu.FullName.Replace(msu.Extension, "-msupcm-temp.json");
            ExportMsuPcmTracksJson(project, song, jsonPath);
            
            var msuPath = new FileInfo(project.MsuPath).DirectoryName;
            var relativePath = Path.GetRelativePath(msuPath!, song.OutputPath);
        
            if (!File.Exists(jsonPath))
            {
                message = $"Track #{song.TrackNumber} - {relativePath} - Invalid MsuPcm++ json was not able to be created";
                return false;
            }

            if (!ValidateMsuPcmInfo(song.MsuPcmInfo, out message, out var numFiles))
            {
                message = $"Track #{song.TrackNumber} - {relativePath} - {message!.ReplaceLineEndings("")}";
                File.Delete(jsonPath);
                return false;
            }

            if (numFiles == 0)
            {
                message = $"Track #{song.TrackNumber} - {relativePath} - No input files specified";
                File.Delete(jsonPath);
                return false;
            }

            if (RunMsuPcm(jsonPath, out message))
            {
                if (!ValidatePcm(song.OutputPath, out message))
                {
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message?.ReplaceLineEndings("")}";
                    File.Delete(jsonPath);
                    return false;
                }
                message = $"Track #{song.TrackNumber} - {relativePath} - Success!";
                File.Delete(jsonPath);
                return true;
            }
        
            message = $"Track #{song.TrackNumber} - {relativePath} - {message.ReplaceLineEndings("")}";
            File.Delete(jsonPath);
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating PCM file for Track #{TrackNum} - {SongPath}", song.TrackNumber, song.OutputPath);
            message = $"Track #{song.TrackNumber} - {song.OutputPath} - Unknown error";
            return false;
        }
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
        if (string.IsNullOrEmpty(SettingsService.Settings.MsuPcmPath) ||
            !File.Exists(SettingsService.Settings.MsuPcmPath))
        {
            error = "MsuPcm++ path not specified or is invalid";
            return false;
        }

        IsGeneratingPcm = true;

        try
        {
            var msuPcmFile = new FileInfo(SettingsService.Settings.MsuPcmPath);
            var command = msuPcmFile.Name + " \"" + trackJson + "\"";
        
            var procStartInfo = new ProcessStartInfo("cmd", "/c " + command)
            {
                WorkingDirectory = msuPcmFile.DirectoryName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // wrap IDisposable into using (in order to release hProcess) 
            using var process = new Process();
            process.StartInfo = procStartInfo;
            process.Start();

            // Add this: wait until process does its work
            process.WaitForExit();

            // and only then read the result
            var result = process.StandardOutput.ReadToEnd().Replace("\0", "").Trim();
            error = process.StandardError.ReadToEnd().Replace("\0", "").Trim();

            IsGeneratingPcm = false;
            if (string.IsNullOrEmpty(error)) return true;
            _logger.LogError("Error running MsuPcm++: {Error}", error);
            return false;
        }
        catch (Exception e)
        {
            IsGeneratingPcm = false;
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
            if (ConverterService.ConvertMsuPcmTrackInfo(song.MsuPcmInfo, false, false) is not Track track) continue;
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
}