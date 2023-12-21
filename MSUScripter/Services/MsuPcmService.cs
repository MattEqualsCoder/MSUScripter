using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Models;
using Newtonsoft.Json;

namespace MSUScripter.Services;

public class MsuPcmService
{
    private readonly ILogger<MsuPcmService> _logger;
    private readonly ConverterService _converterService;
    private readonly Settings _settings;
    private readonly string _cacheFolder;

    public MsuPcmService(ILogger<MsuPcmService> logger, ConverterService converterService, Settings settings)
    {
        _logger = logger;
        _converterService = converterService;
        _settings = settings;
        _cacheFolder = Path.Combine(Directories.CacheFolder, "msupcm");
        if (!Directory.Exists(_cacheFolder))
        {
            Directory.CreateDirectory(_cacheFolder);
        }
    }

    public void DeleteTempPcms(int capPcms = -1)
    {
        var tempDirectory = new DirectoryInfo(Directories.TempFolder);

        if (capPcms <= 0)
        {
            foreach (var tempPcm in tempDirectory.EnumerateFiles("*.pcm", SearchOption.AllDirectories))
            {
                try
                {
                    tempPcm.Delete();
                }
                catch (Exception)
                {
                    _logger.LogWarning("Could not delete {File}", tempPcm.FullName);
                }
            }
        }
        else
        {
            var pcmFiles = tempDirectory.EnumerateFiles("*.pcm", SearchOption.AllDirectories)
                .OrderBy(x => x.CreationTime).ToList();
            if (pcmFiles.Count < capPcms) return;
            foreach (var tempPcm in pcmFiles.Take(pcmFiles.Count-capPcms+1))
            {
                try
                {
                    tempPcm.Delete();
                }
                catch (Exception)
                {
                    _logger.LogWarning("Could not delete {File}", tempPcm.FullName);
                }
            }
        }
    }

    public bool CreateTempPcm(MsuProject project, string inputFile, out string outputPath, out string? message, out bool generated, int? loop = null, int? trimEnd = null, double? normalization = -25, int? trimStart = null, bool skipCleanup = false)
    {
        if (!skipCleanup)
        {
            DeleteTempPcms(10);    
        }
        outputPath = Path.Combine(Directories.TempFolder, $"{Guid.NewGuid():N}.pcm");
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
        var result = CreatePcm(project, new MsuSongInfo()
            {
                TrackNumber = project.MsuType.Tracks.First().Number,
                OutputPath = outputPath,
                MsuPcmInfo = new MsuSongMsuPcmInfo()
                {
                    Output = outputPath,
                    File = inputFile,
                    Loop = loop,
                    TrimStart = trimStart,
                    TrimEnd = trimEnd,
                    Normalization = normalization
                }
            }, out message, out generated, false);

        if (result && generated)
        {
            _logger.LogInformation("Temp PCM {Path} created successfully", outputPath);
        }
        else if (generated)
        {
            _logger.LogInformation("Temp PCM {Path} created with warning: {Warning}", outputPath, message);
        }
        else
        {
            _logger.LogInformation("Temp PCM {Path} had an error: {Error}", outputPath, message);
        }

        return result;
    }

    public void ClearCache()
    {
        var cacheDirectory = new DirectoryInfo(_cacheFolder);
        foreach (var file in cacheDirectory.EnumerateFiles())
        {
            if (file.CreationTime < DateTime.Now.AddMonths(-1))
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    _logger.LogWarning("Could not delete {File}", file.FullName);
                }
            }
        }
    }

    public void DeleteTempJsonFiles()
    {
        var jsonDirectory = Path.Combine(Directories.TempFolder, "msupcm");
        var tempDirectory = new DirectoryInfo(jsonDirectory);
        if (tempDirectory.Exists)
        {
            foreach (var tempPcm in tempDirectory.EnumerateFiles("*.json"))
            {
                try
                {
                    tempPcm.Delete();
                }
                catch
                {
                    _logger.LogWarning("Could not delete {File}", tempPcm.FullName);
                }
            }
        }
    }

    public bool CreatePcm(MsuProject project, MsuSongInfo song, out string? message, out bool generated, bool addTrackDetailsToMessage = true)
    {
        if (string.IsNullOrEmpty(song.OutputPath))
        {
            message = $"Track #{song.TrackNumber} - Missing output PCM path";
            generated = false;
            return false;
        }

        var jsonDirectory = Path.Combine(Directories.TempFolder, "msupcm");
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
                message = "Valid MsuPcm++ json was not able to be created";
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                generated = false;
                return false;
            }

            if (!ValidateMsuPcmInfo(song.MsuPcmInfo, out message, out var numFiles))
            {
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                File.Delete(jsonPath);
                generated = false;
                return false;
            }

            if (numFiles == 0)
            {
                message = "No input files specified";
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                File.Delete(jsonPath);
                generated = false;
                return false;
            }

            var file = new FileInfo(song.OutputPath);
            
            if (IsCached(song.MsuPcmInfo.GetFiles(), file.FullName, jsonPath))
            {
                message = "";
                generated = true;
                return true;
            }
            
            var lastModifiedDate = file.Exists ? file.LastWriteTime : DateTime.MinValue;

            if (RunMsuPcm(jsonPath, out message))
            {
                if (!ValidatePcm(song.OutputPath, out message))
                {
                    if (addTrackDetailsToMessage)
                        message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                    File.Delete(jsonPath);
                    generated = false;
                    return false;
                }
                Cache(song.MsuPcmInfo.GetFiles(), file.FullName, jsonPath);
                message = "Success!";
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                File.Delete(jsonPath);
                generated = true;
                return true;
            }

            file = new FileInfo(song.OutputPath);
            var newModifiedDate = file.Exists ? file.LastWriteTime : DateTime.MinValue;
            generated = newModifiedDate > lastModifiedDate;
            
            if (generated)
            {
                message = $"PCM Generated with msupcm++ warning: {CleanMsuPcmResponse(message)}";
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
            }
            else
            {
                message = CleanMsuPcmResponse(message);
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
            }
           
            File.Delete(jsonPath);
            
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating PCM file for Track #{TrackNum} - {SongPath}", song.TrackNumber, song.OutputPath);
            message = "Unknown error";
            if (addTrackDetailsToMessage)
                message = $"Track #{song.TrackNumber} - {song.OutputPath} - {message}";
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

    private bool IsCached(ICollection<string> inputPaths, string outputPath, string jsonPath)
    {
        if (!File.Exists(outputPath))
        {
            return false;
        }

        var expectedCache = GetCacheDetails(inputPaths, outputPath, jsonPath);

        if (!File.Exists(expectedCache.CachePath))
        {
            return false;
        }

        var currentCache = File.ReadAllText(expectedCache.CachePath);

        return currentCache == expectedCache.CacheValue;
    }

    private void Cache(ICollection<string> inputPaths, string outputPath, string jsonPath)
    {
        var cache = GetCacheDetails(inputPaths, outputPath, jsonPath);
        File.WriteAllText(cache.CachePath, cache.CacheValue);
    }

    private (string CachePath, string CacheValue) GetCacheDetails(ICollection<string> inputPaths, string outputPath,
        string jsonPath)
    {
        using var sha1 = SHA1.Create();

        var key = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(outputPath))).Replace("-", "");
        
        var paths = new List<string>();
        paths.Add(jsonPath);
        paths.Add(outputPath);
        paths.AddRange(inputPaths);
        
        var hashes = new List<string>();
        foreach (var inputPath in paths)
        {
            using var stream = File.OpenRead(inputPath);
            hashes.Add(BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", ""));
        }
        
        var value = string.Join("|", hashes);

        var filePath = Path.Combine(_cacheFolder, key);
            
        return (filePath, value);
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

    private string TempFilePath => Path.Combine(Directories.BaseFolder, "tmp-pcm.pcm");

    private string CleanMsuPcmResponse(string input)
    {
        return Regex.Replace(input.ReplaceLineEndings(""), @"\s[`'][^`']+\.pcm[`']\s", " ");
    }
}