using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Models;
using Newtonsoft.Json;

namespace MSUScripter.Services;

public class MsuPcmInstallResponse
{
    public bool Success { get; set; }
    public bool MissingSharedLibraries { get; set; }
}

public partial class MsuPcmService(
    ILogger<MsuPcmService> logger,
    Settings settings,
    StatusBarService statusBarService,
    ConverterService converterService,
    YamlService yamlService,
    IAudioPlayerService audioPlayerService,
    DependencyInstallerService dependencyInstallerService)
{
    private const string WindowsDownloadUrl = "https://github.com/qwertymodo/msupcmplusplus/releases/download/v1.0RC3/msupcm.exe";
    private const string LinuxDownloadUrl = "https://github.com/qwertymodo/msupcmplusplus/releases/download/v1.0RC3/msupcm";

    private string _cacheFolder2 = "";
    private string _msuPcmPath = string.Empty;

    protected string CacheFolder
    {
        get
        {
            if (string.IsNullOrEmpty(_cacheFolder2))
            {
                _cacheFolder2 = Path.Combine(Directories.CacheFolder, "msupcm");
                if (!Directory.Exists(_cacheFolder2))
                {
                    Directory.CreateDirectory(_cacheFolder2);
                }
            }

            return _cacheFolder2;
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
                    logger.LogWarning("Could not delete {File}", tempPcm.FullName);
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
                    logger.LogWarning("Could not delete {File}", tempPcm.FullName);
                }
            }
        }
    }
    
    public bool IsValid { get; private set; }

    public async Task<GeneratePcmFileResponse> CreateTempPcm(bool standAlone, MsuProject project, string inputFile, int? loop = null, int? trimEnd = null, double? normalization = -25, int? trimStart = null, bool skipCleanup = false)
    {
        if (!skipCleanup)
        {
            DeleteTempPcms(10);    
        }
        var outputPath = Path.Combine(Directories.TempFolder, $"{Guid.NewGuid():N}.pcm");
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
        var result = await CreatePcm(standAlone, project, new MsuSongInfo()
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
            }, false);

        if (result is { Successful: true, GeneratedPcmFile: true })
        {
            logger.LogInformation("Temp PCM {Path} created successfully", outputPath);
        }
        else if (result.GeneratedPcmFile)
        {
            logger.LogInformation("Temp PCM {Path} created with warning: {Warning}", outputPath, result.Message);
        }
        else
        {
            logger.LogInformation("Temp PCM {Path} had an error: {Error}", outputPath, result.Message);
        }

        return result;
    }

    public void ClearCache()
    {
        var cacheDirectory = new DirectoryInfo(CacheFolder);
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
                    logger.LogWarning("Could not delete {File}", file.FullName);
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
                    logger.LogWarning("Could not delete {File}", tempPcm.FullName);
                }
            }
        }
    }

    private string GetSongCacheFilePath(MsuProject project, MsuSongInfo song)
    {
        var projectFolter = Path.Combine(Directories.TempFolder, project.Id);
        if (!Directory.Exists(projectFolter))
        {
            Directory.CreateDirectory(projectFolter);
        }
        return Path.Combine(Directories.CacheFolder, project.Id, song.Id + ".yml");
    }

    private string GetSongGenerationDirectory(MsuProject project, MsuSongInfo song)
    {
        var path = Path.Combine(Directories.TempFolder, project.Id, song.Id);
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }

    public async Task<GeneratePcmFileResponse> CreatePcm(MsuProject project, MsuSongInfo song, bool asPrimary, bool isBulkGeneration)
    {
        if (!audioPlayerService.IsStopped)
        {
            await audioPlayerService.StopSongAsync(song.OutputPath);
        }

        if (!song.HasAudioFiles())
        {
            WriteFailureToStatusBar();
            return new GeneratePcmFileResponse(false, false, "No audio files", null);
        }
        
        if (song.MsuPcmInfo.HasBothSubTracksAndSubChannels)
        {
            var error = "Subtracks and subchannels can't be at the same level and be generated by msupcm++.";
            WriteFailureToStatusBar();
            return new GeneratePcmFileResponse(false, false, error, null);
        }
        
        statusBarService.UpdateStatusBar("Generating PCM");
        
        var tempPath = GetSongGenerationDirectory(project, song);
        var tempJsonPath = Path.Combine(tempPath, "temp.json");
        var tempPcmPath = Path.Combine(tempPath, "temp.pcm");
        var json = GetMsuPcmSongJson(project, song, tempJsonPath, tempPcmPath);
        
        if (string.IsNullOrEmpty(json))
        {
            WriteFailureToStatusBar();
            return new GeneratePcmFileResponse(false, false, "Failed to generate MsuPcm++ JSON file", null);
        }

        var outputPath = song.OutputPath;
        if (asPrimary && song.IsAlt)
        {
            outputPath = project.Tracks.First(x => x.TrackNumber == song.TrackNumber).Songs.First(x => !x.IsAlt).OutputPath;
        }
        else if (song.TrackNumber >= 9999)
        {
            song.OutputPath = tempPcmPath;
            outputPath = tempPcmPath;
        }
        
        if (string.IsNullOrEmpty(outputPath))
        {
            WriteFailureToStatusBar();
            return new GeneratePcmFileResponse(false, false, $"Track #{song.TrackNumber} - Missing output PCM path", null);
        }
        
        project.GenerationCache.Songs.TryGetValue(song.Id, out var previousCache);
        var currentCache = GetSongCacheData(json, outputPath);

        if (MsuProjectSongCache.IsValid(previousCache, currentCache))
        {
            logger.LogInformation("Song {SongId} matches cached data", outputPath);
            return new GeneratePcmFileResponse(true, false, null, outputPath);
        }

        if (!RunMsuPcm(tempJsonPath, tempPcmPath, out var message))
        {
            return new GeneratePcmFileResponse(false, false, message, null);
        }

        if (!ValidatePcm(tempPcmPath, out message))
        {
            return new GeneratePcmFileResponse(false, false, message, null);
        }

        // Move the file
        try
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Move(tempPcmPath, outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while generating PCM file {Path}", outputPath);
            return new GeneratePcmFileResponse(false, false, "MsuPcm++ succeeded, but could not move generated file", null);
        }
        
        currentCache = GetSongCacheData(json, outputPath);

        if (currentCache != null)
        {
            project.GenerationCache.Songs[song.Id] = currentCache;

            if (!isBulkGeneration)
            {
                SaveGenerationCache(project);
            }
        }
        
        message = "Success!";
        
        logger.LogInformation("Generated PCM file {File} successfully", outputPath);
        // _statusBarService.UpdateStatusBar(hasAlts ? "PCM Generated - YAML Regeneration Needed" : "PCM Generated");
        return new GeneratePcmFileResponse(true, true, message, outputPath);
    }
    
    public void SaveGenerationCache(MsuProject project)
    {
        var generationCacheFile = project.GetMsuGenerationCacheFilePath();
        var cacheYaml = yamlService.ToYaml(project.GenerationCache, YamlType.Pascal);
        try
        {
            File.WriteAllText(generationCacheFile, cacheYaml);
            logger.LogInformation("Saved project msupcm++ generation cache to {Path}", generationCacheFile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while saving generation cache");
        }
    }

    private MsuProjectSongCache? GetSongCacheData(string json, string outputPath)
    {
        if (!File.Exists(outputPath))
        {
            return null;
        }
        
        var data = Encoding.UTF8.GetBytes(json);
        var hash = XXH64.DigestOf(data);
        var fileInfo = new FileInfo(outputPath); 
        return new MsuProjectSongCache
        {
            JsonHash = hash,
            JsonLength = json.Length,
            FileGenerationTime = fileInfo.LastWriteTime,
            FileLength = fileInfo.Length,
        };
    }

    public async Task<GeneratePcmFileResponse> CreatePcm(bool standAlone, MsuProject project, MsuSongInfo song, bool addTrackDetailsToMessage = true)
    {
        if (!audioPlayerService.IsStopped)
        {
            await audioPlayerService.StopSongAsync(song.OutputPath);
        }

        var message = "";
        statusBarService.UpdateStatusBar("Generating PCM");

        var hasAlts = project.Tracks.First(x => x.TrackNumber == song.TrackNumber).Songs.Count > 1;
        
        if (string.IsNullOrEmpty(song.OutputPath))
        {
            WriteFailureToStatusBar();
            return new GeneratePcmFileResponse(false, false, $"Track #{song.TrackNumber} - Missing output PCM path", null);
        }

        if (song.MsuPcmInfo.HasBothSubTracksAndSubChannels)
        {
            message = $"Track #{song.TrackNumber} - Subtracks and subchannels can't be at the same level and be generated by msupcm++.";
            WriteFailureToStatusBar();
            return new GeneratePcmFileResponse(false, false, message, null);
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
            ExportMsuPcmTracksJson(standAlone, project, song, jsonPath);
            
            var msuPath = new FileInfo(project.MsuPath).DirectoryName;
            var relativePath = Path.GetRelativePath(msuPath!, song.OutputPath);
        
            if (!File.Exists(jsonPath))
            {
                message = "Valid MsuPcm++ json was not able to be created";
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                WriteFailureToStatusBar();
                return new GeneratePcmFileResponse(false, false, message, null);
            }

            if (!ValidateMsuPcmInfo(song.MsuPcmInfo, out message, out var numFiles))
            {
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                File.Delete(jsonPath);
                WriteFailureToStatusBar();
                return new GeneratePcmFileResponse(false, false, message, null);
            }

            if (numFiles == 0)
            {
                message = "No input files specified";
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                WriteFailureToStatusBar();
                File.Delete(jsonPath);
                return new GeneratePcmFileResponse(false, false, message, null);
            }

            var file = new FileInfo(song.OutputPath);
            
            if (IsCached(song.MsuPcmInfo.GetFiles(), file.FullName, jsonPath))
            {
                message = "Success!";
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                statusBarService.UpdateStatusBar(hasAlts ? "PCM Generated - YAML Regeneration Needed" : "PCM Generated");
                return new GeneratePcmFileResponse(true, true, message, song.OutputPath);
            }
            
            var lastModifiedDate = file.Exists ? file.LastWriteTime : DateTime.MinValue;

            if (RunMsuPcm(jsonPath, song.OutputPath, out message))
            {
                if (!ValidatePcm(song.OutputPath, out message))
                {
                    if (addTrackDetailsToMessage)
                        message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                    File.Delete(jsonPath);
                    return new GeneratePcmFileResponse(false, false, message, null);
                }
                Cache(song.MsuPcmInfo.GetFiles(), file.FullName, jsonPath);
                message = "Success!";
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                File.Delete(jsonPath);
                
                logger.LogInformation("Generated PCM file {File} successfully", song.OutputPath);
                statusBarService.UpdateStatusBar(hasAlts ? "PCM Generated - YAML Regeneration Needed" : "PCM Generated");
                return new GeneratePcmFileResponse(true, true, message, song.OutputPath);
            }

            file = new FileInfo(song.OutputPath);
            var newModifiedDate = file.Exists ? file.LastWriteTime : DateTime.MinValue;
            var generated = newModifiedDate > lastModifiedDate;
            
            if (generated)
            {
                message = $"PCM Generated with msupcm++ warning: {CleanMsuPcmResponse(message)}";
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                statusBarService.UpdateStatusBar("PCM Generated with Warning");
            }
            else
            {
                message = CleanMsuPcmResponse(message);
                if (addTrackDetailsToMessage)
                    message = $"Track #{song.TrackNumber} - {relativePath} - {message}";
                WriteFailureToStatusBar();
            }

            logger.LogInformation(message);
           
            File.Delete(jsonPath);
            
            return new GeneratePcmFileResponse(false, generated, message, generated ? song.OutputPath : null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error creating PCM file for Track #{TrackNum} - {SongPath}", song.TrackNumber, song.OutputPath);
            message = "Unknown error";
            if (addTrackDetailsToMessage)
                message = $"Track #{song.TrackNumber} - {song.OutputPath} - {message}";
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }
            
            WriteFailureToStatusBar();
            return new GeneratePcmFileResponse(false, false, message, null);
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
            logger.LogError(e, "Could not delete output file");
            statusBarService.UpdateStatusBar("Error Creating Empty PCM File");
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
            logger.LogError(e, "Could not copy empty pcm file");
            statusBarService.UpdateStatusBar("Error Creating Empty PCM File");
            return false;
        }

        statusBarService.UpdateStatusBar("Generated Empty PCM File");
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
        if (!File.Exists(path))
        {
            error = "msupcm++ did not create the file, but did not return an error.";
            return false;
        }

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

    public bool RunMsuPcm(string trackJson, string expectedOutputPath, out string error)
    {
        IsGeneratingPcm = true;
        var toReturn = RunMsuPcmInternal("\"" + trackJson + "\"", expectedOutputPath, out _, out error);
        IsGeneratingPcm = false;
        return toReturn;
    }

    public bool VerifyInstalled(out string error)
    {
        var fileName = OperatingSystem.IsWindows() ? "msupcm.exe" : "msupcm";
        _msuPcmPath = Path.Combine(Directories.Dependencies, fileName);
        IsValid = RunMsuPcmInternal("-v", "", out var result, out error) && result.StartsWith("msupcm v");
        
        if (!IsValid)
        {
            logger.LogError("msupcm++ could not be validated at path {Path}: {Error}", _msuPcmPath, error);
        }

        return IsValid;
    }

    public async Task<MsuPcmInstallResponse> Install(Action<string> progress)
    {
        var response = await dependencyInstallerService.InstallMsuPcm(progress);

        if (!response)
        {
            return new MsuPcmInstallResponse
            {
                Success = false,
            };
        }
        
        var verified = VerifyInstalled(out var error);

        if (error.Contains("error while loading shared libraries"))
        {
            return new MsuPcmInstallResponse
            {
                Success = false,
                MissingSharedLibraries = true
            };
        }
        
        return new MsuPcmInstallResponse
        {
            Success = verified,
            MissingSharedLibraries = false
        };
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

        var filePath = Path.Combine(CacheFolder, key);
            
        return (filePath, value);
    }

    private bool RunMsuPcmInternal(string innerCommand, string expectedOutputPath, out string result, out string error)
    {
        if (string.IsNullOrEmpty(_msuPcmPath) ||
            !File.Exists(_msuPcmPath))
        {
            result = "";
            error = "MsuPcm++ path not specified or is invalid";
            return false;
        }
        
        try
        {
            var modifiedTime = DateTime.MinValue;
            if (!string.IsNullOrEmpty(expectedOutputPath) && File.Exists(expectedOutputPath))
            {
                var fileInfo = new FileInfo(expectedOutputPath);
                modifiedTime = fileInfo.LastWriteTime;
            }

            var msuPcmFile = new FileInfo(_msuPcmPath);
            
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

            if (!string.IsNullOrEmpty(error))
            {
                logger.LogError("Error running MsuPcm++: {Error}", error);
                return false;
            }

            // Validate that the file generated if applicable
            if (!string.IsNullOrEmpty(expectedOutputPath))
            {
                if (!File.Exists(expectedOutputPath))
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        error = $"MsuPcm++ did not create the expected file and returned with the following Message: {result}";
                        logger.LogError("Error running MsuPcm++: {Error}", error);
                        return false;
                    }
                    else
                    {
                        error = "$MsuPcm++ ran but did not create the expected file or return an error message.";
                        logger.LogError("Error running MsuPcm++: {Error}", error);
                        return false;
                    }
                }
                else
                {
                    var fileInfo = new FileInfo(expectedOutputPath);
                    if (fileInfo.LastWriteTime <= modifiedTime)
                    {
                        if (!string.IsNullOrEmpty(result))
                        {
                            error = $"MsuPcm++ did not create the expected file and returned with the following Message: {result}";
                            logger.LogError("Error running MsuPcm++: {Error}", error);
                            return false;
                        }
                        else
                        {
                            error = "$MsuPcm++ ran but did not create the expected file or return an error message.";
                            logger.LogError("Error running MsuPcm++: {Error}", error);
                            return false;
                        }
                    }
                }
            }

            return true;
            
        }
        catch (Exception e)
        {
            result = "";
            error = "Unknown error running MsuPcm++";
            logger.LogError(e, "Unknown error running MsuPcm++");
            return false;
        }
    }

    public string? GetMsuPcmSongJson(MsuProject project, MsuSongInfo songInfo, string jsonPath, string pcmPath)
    {
        if (converterService.ConvertMsuPcmTrackInfo(songInfo.MsuPcmInfo, false, false) is not Track track)
        {
            return null;
        }

        track.Output = pcmPath;
        
        
        var output = new MsuPcmPlusPlusConfig()
        {
            Game = project.BasicInfo.Game,
            Pack = project.BasicInfo.PackName,
            Artist = project.BasicInfo.Artist,
            Output_prefix = jsonPath.Replace(".json", ""),
            Normalization = project.BasicInfo.Normalization,
            Dither = project.BasicInfo.Dither,
            Verbosity = 2,
            Keep_temps = settings.RunMsuPcmWithKeepTemps,
            Tracks = [track]
        };
        
        var json = JsonConvert.SerializeObject(output, Formatting.Indented);
        File.WriteAllText(jsonPath, json);
        logger.LogInformation("Wrote {Input} MsuPcm++ json file to {Output}", songInfo.OutputPath, jsonPath);
        return json;
    }
    
    public string? ExportMsuPcmTracksJson(MsuProject project)
    {
        var msu = new FileInfo(project.MsuPath);
        var exportPath = project.GetTracksJsonPath();
        
        var output = new MsuPcmPlusPlusConfig()
        {
            Game = project.BasicInfo.Game,
            Pack = project.BasicInfo.PackName,
            Artist = project.BasicInfo.Artist,
            Output_prefix = msu.FullName.Replace(msu.Extension, ""),
            Normalization = project.BasicInfo.Normalization,
            Dither = project.BasicInfo.Dither,
            Verbosity = 2,
            Keep_temps = settings.RunMsuPcmWithKeepTemps,
            First_track = project.Tracks.Min(x => x.TrackNumber),
            Last_track = project.Tracks.Where(x => !x.IsScratchPad).Max(x => x.TrackNumber)
        };
        var tracks = new List<Track>();

        var songs = project.Tracks.Where(x => !x.IsScratchPad).SelectMany(x => x.Songs).ToList();
        
        foreach (var song in songs)
        {
            if (converterService.ConvertMsuPcmTrackInfo(song.MsuPcmInfo, false, false) is not Track track) continue;
            track.Output = song.OutputPath;
            track.Track_number = song.TrackNumber;
            track.Title = song.TrackName ?? "";
            tracks.Add(track);
        }

        output.Tracks = tracks;
        var json = JsonConvert.SerializeObject(output, Formatting.Indented);
        File.WriteAllText(exportPath, json);
        statusBarService.UpdateStatusBar("Json File Written");
        return exportPath;
    }
    
    public string? ExportMsuPcmTracksJson(bool standAlone, MsuProject project, MsuSongInfo? singleSong = null, string? exportPath = null)
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
            Output_prefix = msu.FullName.Replace(msu.Extension, ""),
            Normalization = project.BasicInfo.Normalization,
            Dither = project.BasicInfo.Dither,
            Verbosity = 2,
            Keep_temps = standAlone && settings.RunMsuPcmWithKeepTemps,
            First_track = singleSong?.TrackNumber ?? project.Tracks.Min(x => x.TrackNumber),
            Last_track = singleSong?.TrackNumber ?? project.Tracks.Where(x => !x.IsScratchPad).Max(x => x.TrackNumber)
        };
        var tracks = new List<Track>();

        var songs = singleSong == null 
            ? project.Tracks.Where(x => !x.IsScratchPad).SelectMany(x => x.Songs).ToList()
            : new List<MsuSongInfo>() { singleSong };
        
        foreach (var song in songs)
        {
            if (converterService.ConvertMsuPcmTrackInfo(song.MsuPcmInfo, false, false) is not Track track) continue;
            track.Output = song.OutputPath;
            track.Track_number = song.TrackNumber;
            track.Title = song.TrackName ?? "";
            tracks.Add(track);
        }

        output.Tracks = tracks;
        var json = JsonConvert.SerializeObject(output, Formatting.Indented);
        File.WriteAllText(exportPath, json);
        statusBarService.UpdateStatusBar("Json File Written");
        return exportPath;
    }

    private void WriteFailureToStatusBar()
    {
        statusBarService.UpdateStatusBar("PCM Generation Failed");
    }
    
    public bool IsGeneratingPcm { get; private set; }

    private string TempFilePath => Path.Combine(Directories.BaseFolder, "tmp-pcm.pcm");

    private string CleanMsuPcmResponse(string input)
    {
        return Regex.Replace(input.ReplaceLineEndings(""), @"\s[`'][^`']+\.pcm[`']\s", " ");
    }

    [GeneratedRegex("[^a-zA-Z0-9 -]")]
    private static partial Regex NonAlphanumericRegex();
}