using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Models;

namespace MSUScripter.Services;

public class PythonCompanionService(ILogger<PythonCompanionService> logger, YamlService yamlService, DependencyInstallerService dependencyInstallerService)
{
    private const string BaseCommand = "py_msu_scripter_app";
    private const string MinVersion = "v0.1.5";
    private RunMethod _runMethod = RunMethod.Unknown;
    private string? _pythonExecutablePath;
    private string? _ffmpegPath;
    
    public bool IsValid { get; private set; }
    public bool IsFfMpegValid { get; private set; }
    
    public async Task<bool> VerifyInstalledAsync()
    {
        if (!await VerifyFfMpegAsync())
        {
            IsValid = false;
            return false;
        }
        
        _runMethod = RunMethod.Unknown;
        _pythonExecutablePath = null;
        var response = await RunCommandAsync("--version");

        IsValid = response.Success && response.Result.EndsWith(MinVersion) &&
                  !response.Error.Contains("Couldn't find ffmpeg");

        if (IsValid)
        {
            logger.LogInformation("Companion PyMsuScripterApp validated successfully: {Result}", response.Result);
        }
        else
        {
            logger.LogError("Failed to validate companion PyMsuScripterApp: {Result} | {Error}", response.Result, response.Error);
        }
        
        return IsValid;
    }

    public async Task<bool> VerifyFfMpegAsync()
    {
        var ffmpegFolder = Path.Combine(Directories.Dependencies, "ffmpeg", "bin");
        var ffmpegAppName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var ffmpegPath = Path.Combine(ffmpegFolder, ffmpegAppName);
        
        logger.LogInformation("Checking for FFmpeg at {Path}", ffmpegPath);

        if (File.Exists(ffmpegPath))
        {
            var installedResult = await ValidateInstalledFfmpegAsync(ffmpegPath, 3);
            if (installedResult.Success && installedResult.Result.StartsWith("ffmpeg version "))
            {
                var parts = installedResult.Result.Split(" ", 4);
                logger.LogInformation("FFmpeg validated successfully at {Path}: {Result}", ffmpegPath, $"{parts[0]} {parts[1]} {parts[2]}");
                IsFfMpegValid = true;
                _ffmpegPath = ffmpegFolder;
                return true;
            }
        }
        
        var pathResult = await RunInternalAsync(ffmpegAppName, "-version");
        IsFfMpegValid = pathResult.Success && pathResult.Result.StartsWith("ffmpeg version ");
        
        if (IsFfMpegValid)
        {
            var parts = pathResult.Result.Split(" ", 4);
            logger.LogInformation("FFmpeg validated successfully from environment: {Version}", $"{parts[0]} {parts[1]} {parts[2]}");
        }
        else
        {
            logger.LogError("Failed to validate companion FFmpeg: {Result} | {Error}", pathResult.Result, pathResult.Error);
        }
        
        return IsFfMpegValid;
    }

    private async Task<RunPyResult> ValidateInstalledFfmpegAsync(string ffmpegPath, int attempts)
    {
        for (var i = 0; i < attempts; i++)
        {
            var currentResult = await RunInternalAsync(ffmpegPath, "-version");
            if (currentResult.Success && currentResult.Result.StartsWith("ffmpeg version"))
            {
                return currentResult;
            }

            if (currentResult.IsBlankSuccess) continue;
            currentResult.Success = false;
            return currentResult;
        }

        return new RunPyResult
        {
            Success = false,
            Error = "Unable to run ffmpeg"
        };
    }

    public async Task<bool> InstallPyApp(Action<string> response)
    {
        var result = await dependencyInstallerService.InstallPyApp(response,
            async (application, arguments) => await RunInternalAsync(application, arguments));
        return result && await VerifyInstalledAsync();
    }
    
    public async Task<bool> InstallFfmpegAsync(Action<string> response)
    {
        var result = await dependencyInstallerService.InstallFfmpeg(response);
        return result && await VerifyFfMpegAsync();
    }

    public async Task<GetSampleRateResponse> GetSampleRateAsync(GetSampleRateRequest request, CancellationToken? cancellationToken = null)
    {
        if (!IsFfMpegValid)
        {
            return new GetSampleRateResponse()
            {
                Successful = false,
                Error = "Companion PyMsuScripterApp not validated"
            };
        }

        var ffprobeResponse = await GetSampleRateViaFfprobeAsync(request.File, 3);
        if (ffprobeResponse.Successful)
        {
            return ffprobeResponse;
        }
        
        return await RunCommandAsync<GetSampleRateRequest, GetSampleRateResponse>(request, cancellationToken);
    }

    private async Task<GetSampleRateResponse> GetSampleRateViaFfprobeAsync(string file, int numAttempts)
    {
        for (var i = 0; i < numAttempts; i++)
        {
            var response = await GetSampleRateViaFfprobeAsync(file);
            if (!response.IsBlankSuccess)
            {
                return response;
            }
        }

        return new GetSampleRateResponse()
        {
            Successful = false,
            Error = "Unable to get results from FFprobe"
        };
    }
    
    private async Task<GetSampleRateResponse> GetSampleRateViaFfprobeAsync(string file)
    {
        var ffprobePath = string.IsNullOrEmpty(_ffmpegPath) ? "ffprobe" : Path.Combine(_ffmpegPath, "ffprobe");
        var ffprobeResponse = await RunInternalAsync(ffprobePath, $"-v quiet -show_streams \"{file}\"");
        if (ffprobeResponse.Success && ffprobeResponse.Result.StartsWith("[STREAM]"))
        {
            try
            {
                var sampleRate = -1;
                var duration = -1d;
                var channels = 2;
                int bitsPerSample = 2;
                var lines = ffprobeResponse.Result.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                foreach (var line in lines.Where(x => x.Contains('=')))
                {
                    var parts = line.Split('=');
                    if (parts[0] == "sample_rate")
                    {
                        sampleRate = int.Parse(parts[1]);
                    }
                    else if (parts[0] == "duration")
                    {
                        duration = double.Parse(parts[1]);
                    }
                    else if (parts[0] == "channels")
                    {
                        channels = int.Parse(parts[1]);
                    }
                    else if (parts[0] == "bits_per_sample")
                    {
                        bitsPerSample = int.Parse(parts[1]);
                    }
                }

                if (sampleRate > 0 && duration > 0)
                {
                    return new GetSampleRateResponse
                    {
                        Successful = true,
                        SampleRate = sampleRate,
                        Duration = duration,
                        Channels = channels,
                        BitsPerSample = bitsPerSample
                    };
                }
                else
                {
                    logger.LogError("Failed getting valid sample rate and duration: {SampleRate} | {Duration}", sampleRate, duration);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, "Error running FFprobe");
            }
        }
        else
        {
            logger.LogError("Invalid response from FFprobe: Result: {Result} | Error: {Error}", ffprobeResponse.Result, ffprobeResponse.Error);
        }

        return new GetSampleRateResponse
        {
            Successful = false,
            Error = "Unable to get sample rate via ffprobe",
            IsBlankSuccess = ffprobeResponse.Success && string.IsNullOrEmpty(ffprobeResponse.Result) && string.IsNullOrEmpty(ffprobeResponse.Error)
        };
    }

    public async Task<RunPyMusicLooperResponse> RunPyMusicLooperAsync(RunPyMusicLooperRequest request, CancellationToken? cancellationToken = null)
    {
        if (!IsValid)
        {
            return new RunPyMusicLooperResponse()
            {
                Successful = false,
                Error = "Companion PyMsuScripterApp not validated"
            };
        }

        var cachePath = GetPyMusicLooperCachePath(request);
        
        logger.LogInformation("PyMusicLooper checking cache at {Path}", cachePath);
        
        var savedCacheData = GetCacheFromFile(cachePath);

        if (savedCacheData?.Successful == true)
        {
            logger.LogInformation("PyMusicLooper cache found at {Path}", cachePath);
            return savedCacheData;
        }
        
        var result = await RunCommandAsync<RunPyMusicLooperRequest, RunPyMusicLooperResponse>(request, cancellationToken);

        if (result.Successful)
        {
            logger.LogInformation("PyMusicLooper completed for {File} with {PairCount} results", request.File, result.Pairs.Count);
            SaveCacheToFile(result, cachePath);
        }

        return result;
    }

    private string GetPyMusicLooperCachePath(RunPyMusicLooperRequest request)
    {
        var directory =  Path.Combine(Directories.CacheFolder, "PyMusicLooper");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var cacheKey = new PyMusicLooperCacheKey(request).ToString();
        return Path.Combine(directory, $"{cacheKey}.yml");
    }
    
    private RunPyMusicLooperResponse? GetCacheFromFile(string cachePath)
    {
        if (!File.Exists(cachePath))
        {
            return null;
        }
        
        var text = File.ReadAllText(cachePath);
        if (yamlService.FromYaml<RunPyMusicLooperResponse>(text, YamlType.Pascal, out var cacheObject, out _) && cacheObject != null)
        {
            return cacheObject;
        }

        return null;
    }

    private void SaveCacheToFile(RunPyMusicLooperResponse cache, string cachePath)
    {
        var text = yamlService.ToYaml(cache, YamlType.Pascal);
        File.WriteAllText(cachePath, text);
        logger.LogInformation("Saved PyMusicLooper results to {CachePath}", cachePath);
    }

    public async Task<CreateVideoResponse> CreateVideoAsync(CreateVideoRequest request, Action<double>? updateCallback = null, CancellationToken? cancellationToken = null)
    {
        if (!IsValid)
        {
            return new CreateVideoResponse()
            {
                Successful = false,
                Error = "Companion PyMsuScripterApp not validated"
            };
        }
        
        if (!string.IsNullOrEmpty(request.ProgressFile) && File.Exists(request.ProgressFile) && updateCallback != null)
        {
            File.Delete(request.ProgressFile);
        }
        else if (string.IsNullOrEmpty(request.ProgressFile) && updateCallback != null)
        {
            request.ProgressFile = Path.Combine(Directories.TempFolder, $"{Guid.NewGuid().ToString()}_progress.txt");
        }

        CancellationTokenSource cts = new();
        
        if (updateCallback != null && !string.IsNullOrEmpty(request.ProgressFile))
        {
            _ = ITaskService.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        await using var fs = new FileStream(request.ProgressFile, FileMode.Open, FileAccess.Read,
                            FileShare.ReadWrite);
                        using var reader = new StreamReader(fs);
        
                        var text = await reader.ReadToEndAsync(cts.Token);
                        if (text.Contains('|'))
                        {
                            var parts = text.Split("|");
                            var section = int.Parse(parts[0]);
                            var percentage = float.Parse(parts[1]);
                            var totalPercentage = (section * 0.33f) + (percentage / 100 * 0.33);
                            updateCallback(totalPercentage);
                        }
                        // Do nothing
                    }
                    catch
                    {
                        // Do nothing
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                }
            }, cts.Token);
        }
        
        var response = await RunCommandAsync<CreateVideoRequest, CreateVideoResponse>(request, cancellationToken);

        var successful = response.Successful && !cts.IsCancellationRequested;
        
        await cts.CancelAsync();

        if (updateCallback != null)
        {
            updateCallback(1);
        }

        if (successful)
        {
            logger.LogInformation("Video creation successful");
        }
        else
        {
            logger.LogError("Error generating video: {Response}", response.Error);
        }
        
        return response;
    }

    private async Task<TResponse> RunCommandAsync<TRequest, TResponse>(TRequest request, CancellationToken? cancellationToken) where TResponse : PythonCompanionModeResponse
    {
        var guid = Guid.NewGuid().ToString();
        var inputFile = Path.Combine(Directories.TempFolder, $"{guid}_in.yml");
        var outputFile = Path.Combine(Directories.TempFolder, $"{guid}_out.yml");
        try
        {
            await File.WriteAllTextAsync(inputFile, yamlService.ToYaml(request!, YamlType.Pascal), cancellationToken ?? CancellationToken.None);

            var runResponse = await RunCommandAsync($"--input \"{inputFile}\" --output \"{outputFile}\"", cancellationToken);
            if (!runResponse.Success)
            {
                var response = Activator.CreateInstance<TResponse>();
                response.Successful = false;
                response.Error = runResponse.Error;
                return response;
            }

            if (cancellationToken?.IsCancellationRequested == true)
            {
                var response = Activator.CreateInstance<TResponse>();
                response.Successful = false;
                response.Error = "Request cancelled";
                return response;
            }

            var outputYaml = await File.ReadAllTextAsync(outputFile);
            
            if (yamlService.FromYaml<TResponse>(outputYaml, YamlType.Pascal, out var result, out var error))
            {
                return result!;
            }
            else
            {
                var response = Activator.CreateInstance<TResponse>();
                response.Successful = false;
                response.Error = error ?? "Failed running PythonCompanionService";
                return response;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed running PythonCompanionService");
            var response = Activator.CreateInstance<TResponse>();
            response.Successful = false;
            response.Error =  "Failed running PythonCompanionService";
            return response;
        }
    }

    private async Task<RunPyResult> RunCommandAsync(string command, CancellationToken? cancellationToken = null)
    {
        switch (_runMethod)
        {
            case RunMethod.Unknown:
                var result = await RunInternalInstalledAsync(command, cancellationToken);
                if (result.Success)
                {
                    _runMethod = RunMethod.Installed;
                    return result;
                }

                result = await RunInternalDirectAsync(command, cancellationToken);
                if (result.Success)
                {
                    _runMethod = RunMethod.Direct;
                    return result;
                }
                
                result = await RunInternalPyAsync(command, cancellationToken);
                if (result.Success)
                {
                    _runMethod = RunMethod.Py;
                    return result;
                }
                
                result = await RunInternalPython3Async(command, cancellationToken);
                if (result.Success)
                {
                    _runMethod = RunMethod.Python3;
                    return result;
                }
                
                return new RunPyResult
                {
                    Success = false,
                    Error = "No valid run type found"
                };
                
            case RunMethod.Installed:
                return await RunInternalInstalledAsync(command, cancellationToken);
            case RunMethod.Direct:
                return await RunInternalDirectAsync(command, cancellationToken);
            case RunMethod.Py:
                return await RunInternalPyAsync(command, cancellationToken);
            case RunMethod.Python3:
                return await RunInternalPython3Async(command, cancellationToken);
            default:
                return new RunPyResult
                {
                    Success = false,
                    Error = "Invalid run type"
                };
        }
    }
    
    private async Task<RunPyResult> RunInternalDirectAsync(string command, CancellationToken? cancellationToken = null)
    {
        return await RunInternalAsync(BaseCommand, command, cancellationToken);
    }
    
    private async Task<RunPyResult> RunInternalInstalledAsync(string command, CancellationToken? cancellationToken = null)
    {
        if (!string.IsNullOrEmpty(_pythonExecutablePath))
        {
            return await RunInternalAsync(_pythonExecutablePath, $"-m {BaseCommand} {command}", cancellationToken);
        }
        
        var exePath = Directories.Dependencies;
        
        if (!string.IsNullOrEmpty(exePath) && Directory.Exists(Path.Combine(exePath, "python")))
        {
            _pythonExecutablePath = OperatingSystem.IsLinux()
                ? Path.Combine(exePath, "python", "bin", "python3.13")
                : Path.Combine(exePath, "python", "python.exe");

            if (!File.Exists(_pythonExecutablePath))
            {
                _pythonExecutablePath = "";
                return new RunPyResult
                {
                    Success = false,
                    Error = "Python executable not found"
                };
            }
        }
        else
        {
            return new RunPyResult
            {
                Success = false,
                Error = "Invalid run type"
            };
        }
        
        return await RunInternalAsync(_pythonExecutablePath, $"-m {BaseCommand} {command}", cancellationToken);
    }

    private async Task<RunPyResult> RunInternalPyAsync(string command, CancellationToken? cancellationToken = null)
    {
        return await RunInternalAsync("py", $"-m {BaseCommand} {command}", cancellationToken);
    }
    
    private async Task<RunPyResult> RunInternalPython3Async(string command, CancellationToken? cancellationToken = null)
    {
        return await RunInternalAsync("python3", $"-m {BaseCommand} {command}", cancellationToken);
    }

    private async Task<RunPyResult> RunInternalAsync(string command, string arguments, CancellationToken? cancellationToken = null)
    {
        try
        {
            ProcessStartInfo procStartInfo;
            
            var innerCommand = $"{command} {arguments}";
            logger.LogInformation("Executing python command: {Command}", innerCommand);
            
            var workingDirectory = "";
            if (!string.IsNullOrEmpty(_ffmpegPath))
            {
                workingDirectory = _ffmpegPath;
            } else if (File.Exists(command))
            {
                workingDirectory = Directory.GetParent(command)?.FullName;
            }

            var isPipInstall = arguments.StartsWith("-m pip");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string fileName;
                string argumentString;
                
                if (!string.IsNullOrEmpty(workingDirectory) && command.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    var file = Path.GetFileName(command);
                    fileName = "cmd";
                    argumentString = $"/c {file} {arguments}";
                }
                else if (Path.IsPathRooted(command))
                {
                    fileName = command;
                    argumentString = arguments;
                }
                else
                {
                    var file = Path.GetFileName(command);
                    fileName = "cmd";
                    argumentString = $"/c {file} {arguments}";
                }
                
                procStartInfo= new ProcessStartInfo(fileName, argumentString)
                {
                    RedirectStandardOutput = !isPipInstall,
                    RedirectStandardError = !isPipInstall,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };
            }
            else
            {
                procStartInfo= new ProcessStartInfo(command)
                {
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };
            }
            
            using var process = new Process();
            process.StartInfo = procStartInfo;
            process.Start();
            await process.WaitForExitAsync(cancellationToken ??  CancellationToken.None);

            var resultText = "";
            var errorText = "";

            if (isPipInstall)
            {
                errorText = process.ExitCode != 0 ? $"pipx completed with error code {process.ExitCode}" : "";
            }
            else
            {
                resultText = (await process.StandardOutput.ReadToEndAsync()).Replace("\0", "").Trim();
                errorText = (await process.StandardError.ReadToEndAsync()).Replace("\0", "").Trim();
            }
            
            if (!string.IsNullOrEmpty(errorText))
            {
                logger.LogError("Error running {Command}: {Error}", BaseCommand, errorText);
            }

            if (cancellationToken?.IsCancellationRequested == true)
            {
                logger.LogError("Cancellation requested of command {Command}", BaseCommand);
            }
            
            return new RunPyResult
            {
                Success = true,
                Result = resultText,
                Error = errorText
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unknown error running {Command}", BaseCommand);
            return new RunPyResult
            {
                Success = false,
                Error = $"Unknown error running {BaseCommand}"
            };
        }
    }
}
