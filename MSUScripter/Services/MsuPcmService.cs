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

    private ILogger<MsuPcmService> _logger;

    public MsuPcmService(ILogger<MsuPcmService> logger)
    {
        _logger = logger;
        Instance = this;
    }

    public void CreateMsu(MsuProject project)
    {
        var jsonPath = ExportMsuPcmTracksJson(project);
        if (jsonPath == null)
            return;
        //RunMsuPcm(jsonPath);
    }

    public bool CreatePcm(MsuProject project, MsuSongInfo song, out string? message)
    {
        var jsonPath = ExportMsuPcmTracksJson(project, song);

        var msuPath = new FileInfo(project.MsuPath).DirectoryName;
        var relativePath = Path.GetRelativePath(msuPath!, song.OutputPath);
        
        if (jsonPath == null)
        {
            message = $"Track #{song.TrackNumber} - {relativePath} - Invalid MsuPcm++ tracks json path";
            return true;
        }

        if (!ValidateMsuPcmInfo(song.MsuPcmInfo, out message, out var numFiles))
        {
            message = $"Track #{song.TrackNumber} - {relativePath} - {message!.ReplaceLineEndings("")}";
            return false;
        }

        if (numFiles == 0)
        {
            message = $"Track #{song.TrackNumber} - {relativePath} - No input files specified";
            return false;
        }

        if (RunMsuPcm(jsonPath, out message))
        {
            if (!ValidatePcm(song.OutputPath, out message))
            {
                message = $"Track #{song.TrackNumber} - {relativePath} - {message?.ReplaceLineEndings("")}";
                return false;
            }
            message = $"Track #{song.TrackNumber} - {relativePath} - Success!";
            return true;
        }
        
        message = $"Track #{song.TrackNumber} - {relativePath} - {message.ReplaceLineEndings("")}";
        return false;
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
        var command = SettingsService.Settings.MsuPcmPath + " \"" + trackJson + "\"";
        ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", "/c " + command);

        procStartInfo.RedirectStandardOutput = true;
        procStartInfo.RedirectStandardError = true;
        procStartInfo.UseShellExecute = false;
        procStartInfo.CreateNoWindow = true;

        // wrap IDisposable into using (in order to release hProcess) 
        using(Process process = new Process()) {
            process.StartInfo = procStartInfo;
            process.Start();

            // Add this: wait until process does its work
            process.WaitForExit();

            // and only then read the result
            string result = process.StandardOutput.ReadToEnd().Replace("\0", "").Trim();
            error = process.StandardError.ReadToEnd().Replace("\0", "").Trim();

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError("Error running MsuPcm++: {Error}", error);
                return false;
            }

            return true;
        }
    }
    
    public string? ExportMsuPcmTracksJson(MsuProject project, MsuSongInfo? singleSong = null, string? exportPath = null)
    {
        if (string.IsNullOrEmpty(project.MsuPcmTracksJsonPath) && string.IsNullOrEmpty(exportPath)) return null;
        
        var msu = new FileInfo(project.MsuPath);
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
        var json = JsonConvert.SerializeObject(output);
        exportPath ??= project.MsuPcmTracksJsonPath!.Replace(".json", "-2.json");
        File.WriteAllText(exportPath, json);
        return exportPath;
    }
}