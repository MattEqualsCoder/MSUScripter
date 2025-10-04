using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Models;
using Newtonsoft.Json;

namespace MSUScripter.Services;

public class MsuPcmInstallResponse
{
    public bool Success { get; init; }
    public bool MissingSharedLibraries { get; init; }
}

public class MsuPcmJsonInfo
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required string JsonFilePath { get; init; }
    public required string JsonText { get; init; }
}

public class MsuPcmResult
{
    public bool Successful { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class MsuPcmService(
    ILogger<MsuPcmService> logger,
    Settings settings,
    StatusBarService statusBarService,
    ConverterService converterService,
    YamlService yamlService,
    IAudioPlayerService audioPlayerService,
    DependencyInstallerService dependencyInstallerService)
{
    private string _cacheFolder2 = "";
    private string _msuPcmPath = string.Empty;

    public bool IsGeneratingPcm { get; private set; }

    public event EventHandler<bool>? GeneratingPcm; 

    private string CacheFolder
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

    public async Task<GeneratePcmFileResponse> CreateTempPcm(MsuProject project, string inputFile, int? loop = null, int? trimEnd = null, double? normalization = -25, int? trimStart = null, bool skipCleanup = false)
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
        var result = await CreatePcm(project, new MsuSongInfo()
            {
                Id = Guid.NewGuid().ToString("N"),
                TrackNumber = -1,
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
            }, false, false, false);

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

    public async Task<GeneratePcmFileResponse> CreatePcm(MsuProject project, MsuSongInfo song, bool asPrimary, bool isBulkGeneration, bool cacheResults)
    {
        if (!IsValid)
        {
            return new GeneratePcmFileResponse(false, false,
                "MsuPcm++ is not installed and configured. Please install MsuPcm++ and reverify in the MSU Scripter settings.",
                null);
        }

        GeneratingPcm?.Invoke(this, true);
        
        if (!audioPlayerService.IsStopped)
        {
            await audioPlayerService.StopSongAsync(song.OutputPath);
        }

        if (!song.HasAudioFiles())
        {
            WriteFailureToStatusBar();
            GeneratingPcm?.Invoke(this, false);
            return new GeneratePcmFileResponse(false, false, "No input files selected.", null);
        }
        
        if (song.MsuPcmInfo.HasBothSubTracksAndSubChannels)
        {
            var error = "Subtracks and subchannels can't be at the same level and be generated by msupcm++.";
            WriteFailureToStatusBar();
            GeneratingPcm?.Invoke(this, false);
            return new GeneratePcmFileResponse(false, false, error, null);
        }

        if (song.TrackNumber < 0)
        {
            logger.LogInformation("Generating PCM file for song {Song}", string.IsNullOrEmpty(song.SongName) ? song.TrackName : song.SongName);
        }
        else
        {
            logger.LogInformation("Generating temp PCM file for {SongFile}", song.MsuPcmInfo.File);
        }
        
        var tempPath = project.GetMsuGenerationTempFilePath(song);
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        var tempJsonPath = Path.Combine(tempPath, $"temp.json");
        var tempPcmPath = Path.Combine(tempPath, $"temp.pcm");
        var jsonResponse = ExportMsuPcmTracksJson(project, song, tempJsonPath, tempPcmPath);
        
        if (string.IsNullOrEmpty(jsonResponse.JsonText))
        {
            WriteFailureToStatusBar();
            GeneratingPcm?.Invoke(this, false);
            return new GeneratePcmFileResponse(false, false, "Failed to generate MsuPcm++ JSON file", null);
        }

        var outputPath = song.OutputPath;
        if (asPrimary && song.IsAlt)
        {
            outputPath = project.Tracks.First(x => x.TrackNumber == song.TrackNumber).Songs.First(x => !x.IsAlt).OutputPath;
        }
        else if (song.TrackNumber is >= 1000 or < 0)
        {
            song.OutputPath = tempPcmPath;
            outputPath = tempPcmPath;
        }
        
        if (string.IsNullOrEmpty(outputPath))
        {
            WriteFailureToStatusBar();
            GeneratingPcm?.Invoke(this, false);
            return new GeneratePcmFileResponse(false, false, $"Track #{song.TrackNumber} - Missing output PCM path", null);
        }
        
        project.GenerationCache.Songs.TryGetValue(song.Id, out var previousCache);
        var currentCache = cacheResults ? GetSongCacheData(jsonResponse.JsonText, outputPath) : null;

        if (MsuProjectSongCache.IsValid(previousCache, currentCache))
        {
            logger.LogInformation("Song {SongId} matches cached data", outputPath);
            statusBarService.UpdateStatusBar("PCM Generated");
            GeneratingPcm?.Invoke(this, false);
            return new GeneratePcmFileResponse(true, true, null, outputPath);
        }

        logger.LogInformation("Generating PCM at {Path}", tempPcmPath);
        statusBarService.UpdateStatusBar("Generating PCM");

        // Generate the file and make sure the file exists
        var msuPcmResult = await RunMsuPcmAsync(tempJsonPath, tempPcmPath);
        if (!msuPcmResult.Successful && !File.Exists(tempPcmPath))
        {
            logger.LogError("MsuPcm++ returned the following error: {Error}", msuPcmResult.Error);
            GeneratingPcm?.Invoke(this, false);
            WriteFailureToStatusBar();
            return new GeneratePcmFileResponse(false, false, msuPcmResult.Error, outputPath);
        }
        logger.LogInformation("MsuPcm++ generated PCM file {File}", tempPcmPath);
        
        // Clean up msupcm++ errors
        bool msuPcmSuccessful = msuPcmResult.Successful;
        var msuPcmMessage = msuPcmResult.Error;
        if (!msuPcmResult.Successful && project.IgnoreWarnings.Contains(msuPcmResult.Error))
        {
            logger.LogWarning("Ignoring MsuPcm++ warning: {Warning}", msuPcmResult.Error);
            msuPcmSuccessful = true;
            msuPcmMessage = "";
        }
        
        // Validate the MsuPcm++ header
        var validationSuccessful = ValidatePcm(tempPcmPath, out var validationResponse);
        if (!validationSuccessful)
        {
            logger.LogError("Generated PCM file has an error: {Error}", validationResponse);
            GeneratingPcm?.Invoke(this, false);
            WriteFailureToStatusBar();
            return new GeneratePcmFileResponse(false, false, validationResponse, outputPath);
        }

        // Move the file to the target location
        if (outputPath != tempPcmPath)
        {
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

                logger.LogInformation("Moving generated PCM to {Path}", outputPath);
                File.Move(tempPcmPath, outputPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while generating PCM file {Path}", outputPath);
                GeneratingPcm?.Invoke(this, false);
                WriteFailureToStatusBar();
                return new GeneratePcmFileResponse(false, false, "MsuPcm++ succeeded, but could not move generated file", outputPath);
            }
        }

        // Move to the cache
        if (cacheResults)
        {
            currentCache = GetSongCacheData(jsonResponse.JsonText, outputPath);

            if (currentCache != null)
            {
                project.GenerationCache.Songs[song.Id] = currentCache;

                if (!isBulkGeneration)
                {
                    SaveGenerationCache(project);
                }
            }
        }

        if (!asPrimary)
        {
            song.LastGeneratedDate = DateTime.Now;
        }
        
        var hasAlts = song.TrackNumber < 1000 &&
                      project.Tracks.FirstOrDefault(x => x.TrackNumber == song.TrackNumber)?.Songs.Count > 1;

        if (msuPcmSuccessful && validationSuccessful)
        {
            logger.LogInformation("Generated PCM file {File} successfully", outputPath);
        }
        else
        {
            logger.LogWarning("Generated PCM file {File} with warnings: {Warning}", outputPath, msuPcmMessage);
        }
        
        statusBarService.UpdateStatusBar(hasAlts ? "PCM Generated - YAML Regeneration Needed" : "PCM Generated");
        GeneratingPcm?.Invoke(this, false);
        return new GeneratePcmFileResponse(msuPcmSuccessful && validationSuccessful, true, msuPcmMessage, outputPath);
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
  
    public GeneratePcmFileResponse CreateEmptyPcm(MsuSongInfo song)
    {
        if (string.IsNullOrEmpty(song.OutputPath))
        {
            return new GeneratePcmFileResponse(false, false, "Missing song output path", null);
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
            return new GeneratePcmFileResponse(false, false, "Could not delete output file", null);
        }

        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MSUScripter.Assets.empty.pcm");
        if (stream == null)
        {
            return new GeneratePcmFileResponse(false, false, "Error Creating Empty PCM File", null);
        }

        try
        {
            using var fileStream = File.Create(song.OutputPath);
            stream.CopyTo(fileStream);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Could not copy empty pcm file");
            statusBarService.UpdateStatusBar("Error Creating Empty PCM File");
            return new GeneratePcmFileResponse(false, false, "Error Creating Empty PCM File", null);
        }

        statusBarService.UpdateStatusBar("Generated Empty PCM File");
        return new GeneratePcmFileResponse(true, true, "Error Creating Empty PCM File", song.OutputPath);

    }

    private bool ValidatePcm(string path, out string validationError)
    {
        var testBytes = new byte[8];
        using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            _ = reader.Read(testBytes, 0, 8);
        }

        if (Encoding.UTF8.GetString(testBytes, 0, 4) != "MSU1")
        {
            validationError = "MsuPcm++ generated the file with an invalid header.";
            return false;
        }

        var loop = BitConverter.ToInt32(testBytes, 4);
        var totalSamples = (new FileInfo(path).Length - 8) / 4;

        if (loop < totalSamples)
        {
            validationError = "";
            return true;
        }

        validationError = "Bad loop point specified. Continuing could cause some emulators to crash.";
        return false;
    }

    private async Task<MsuPcmResult> RunMsuPcmAsync(string trackJson, string expectedOutputPath)
    {
        IsGeneratingPcm = true;
        logger.LogInformation("Running MsuPcm++ for file {File}", trackJson);
        var toReturn = RunMsuPcmInternalAsync(trackJson, expectedOutputPath);
        IsGeneratingPcm = false;
        return await toReturn;
    }

    public async Task<MsuPcmResult> VerifyInstalledAsync()
    {
        var fileName = OperatingSystem.IsWindows() ? "msupcm.exe" : "msupcm.AppImage";
        _msuPcmPath = Path.Combine(Directories.Dependencies, fileName);
        var result = await RunMsuPcmInternalAsync("-v", "");
        IsValid = result.Successful && result.Result.StartsWith("msupcm v");
        
        if (!IsValid)
        {
            logger.LogError("msupcm++ could not be validated at path {Path}: {Error}", _msuPcmPath, result.Error);
        }
        else
        {
            logger.LogInformation("msupcm++ validated successfully at {Path}: {Result}", _msuPcmPath, result.Result);
        }

        return new MsuPcmResult()
        {
            Successful = IsValid,
            Result = result.Result,
            Error = result.Error
        };
    }

    public async Task<MsuPcmInstallResponse> InstallAsync(Action<string> progress)
    {
        var response = await dependencyInstallerService.InstallMsuPcm(progress);

        if (!response)
        {
            return new MsuPcmInstallResponse
            {
                Success = false,
            };
        }
        
        var verified = await VerifyInstalledAsync();

        if (verified.Error.Contains("error while loading shared libraries"))
        {
            return new MsuPcmInstallResponse
            {
                Success = false,
                MissingSharedLibraries = true
            };
        }
        
        return new MsuPcmInstallResponse
        {
            Success = verified.Successful,
            MissingSharedLibraries = false
        };
    }
    
    private async Task<MsuPcmResult> RunMsuPcmInternalAsync(string innerCommand, string expectedOutputPath)
    {
        if (string.IsNullOrEmpty(_msuPcmPath) ||
            !File.Exists(_msuPcmPath))
        {
            return new MsuPcmResult()
            {
                Successful = false,
                Result = "",
                Error = "MsuPcm++ path not specified or is invalid"
            };
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

            var workingDirectory = msuPcmFile.DirectoryName;
            var command = msuPcmFile.FullName;
            var arguments = innerCommand;

            if (innerCommand != "-v" && File.Exists(innerCommand))
            {
                workingDirectory = Path.GetDirectoryName(innerCommand);
                arguments = "\"" + Path.GetFileName(innerCommand) + "\"";
            }
            
            ProcessStartInfo procStartInfo;
            
            logger.LogInformation("Running msupcm++ command: {Command} {Arguments}", command, arguments);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                procStartInfo= new ProcessStartInfo(command)
                {
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                procStartInfo= new ProcessStartInfo(command)
                {
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
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
            await process.WaitForExitAsync();

            // and only then read the result
            var result = (await process.StandardOutput.ReadToEndAsync()).Replace("\0", "").Trim();
            var error = (await process.StandardError.ReadToEndAsync()).Replace("\0", "").Trim();

            if (!string.IsNullOrEmpty(error))
            {
                logger.LogError("Error running MsuPcm++: {Error}", error);
                if (error.Contains("decrease volume?"))
                {
                    error =
                        "MsuPcm settings for audio file caused audio clipping. Consider lowering the normalization value.";
                }
                return new MsuPcmResult()
                {
                    Successful = false,
                    Result = result,
                    Error = error
                };
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
                        return new MsuPcmResult()
                        {
                            Successful = false,
                            Result = result,
                            Error = error
                        };
                    }
                    else
                    {
                        error = "$MsuPcm++ ran but did not create the expected file or return an error message.";
                        logger.LogError("Error running MsuPcm++: {Error}", error);
                        return new MsuPcmResult()
                        {
                            Successful = false,
                            Result = result,
                            Error = error
                        };
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
                            return new MsuPcmResult()
                            {
                                Successful = false,
                                Result = result,
                                Error = error
                            };
                        }
                        else
                        {
                            error = "$MsuPcm++ ran but did not create the expected file or return an error message.";
                            logger.LogError("Error running MsuPcm++: {Error}", error);
                            return new MsuPcmResult()
                            {
                                Successful = false,
                                Result = result,
                                Error = error
                            };
                        }
                    }
                }
            }

            return new MsuPcmResult()
            {
                Successful = true,
                Result = result,
                Error = error
            };
            
        }
        catch (Exception e)
        {
            var result = "";
            var error = "Unknown error running MsuPcm++";
            logger.LogError(e, "Unknown error running MsuPcm++");
            return new MsuPcmResult()
            {
                Successful = false,
                Result = result,
                Error = error
            };
        }
    }

    public MsuPcmJsonInfo ExportMsuPcmTracksJson(MsuProject project, MsuSongInfo? singleSong = null, string? exportPath = null, string? pcmPath = null)
    {
        var msu = new FileInfo(project.MsuPath);
        
        if (string.IsNullOrEmpty(exportPath))
        {
            exportPath = project.GetTracksJsonPath();
        }
        
        bool? ditherValue = project.BasicInfo.DitherType switch
        {
            DitherType.Default => null,
            DitherType.All => true,
            DitherType.None => false,
            DitherType.DefaultOn => singleSong?.MsuPcmInfo.Dither ?? true,
            DitherType.DefaultOff => singleSong?.MsuPcmInfo.Dither ?? false,
            _ => null
        };
        
        var songs = singleSong == null 
            ? project.Tracks.Where(x => !x.IsScratchPad).SelectMany(x => x.Songs).ToList()
            : [singleSong];
        
        var output = new MsuPcmPlusPlusConfig()
        {
            Game = project.BasicInfo.Game,
            Pack = project.BasicInfo.PackName,
            Artist = project.BasicInfo.Artist,
            Output_prefix = msu.FullName.Replace(msu.Extension, ""),
            Normalization = project.BasicInfo.Normalization,
            Dither = ditherValue,
            Verbosity = 2,
            Keep_temps = settings.RunMsuPcmWithKeepTemps,
            First_track = songs.Min(x => x.TrackNumber),
            Last_track = songs.Max(x => x.TrackNumber)
        };
        
        var tracks = new List<Track>();
        foreach (var song in songs)
        {
            if (converterService.ConvertMsuPcmTrackInfo(song.MsuPcmInfo, false, false) is not Track track) continue;
            track.Output = pcmPath ?? song.OutputPath;
            track.Track_number = song.TrackNumber;
            track.Title = song.TrackName ?? "";
            tracks.Add(track);
        }

        output.Tracks = tracks;
        var json = JsonConvert.SerializeObject(output, Formatting.Indented);
        File.WriteAllText(exportPath, json);
        statusBarService.UpdateStatusBar("Json File Written");
        return new MsuPcmJsonInfo
        {
            JsonFilePath = exportPath,
            JsonText = json
        };
    }

    private void WriteFailureToStatusBar()
    {
        statusBarService.UpdateStatusBar("PCM Generation Failed");
    }
}