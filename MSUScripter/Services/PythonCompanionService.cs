using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Models;

namespace MSUScripter.Services;

public class PythonCompanionService(ILogger<PythonCompanionService> logger, YamlService yamlService, DependencyInstallerService dependencyInstallerService)
{
    private enum PythonCompanionServiceMode
    {
        Unknown,
        Direct,
        Installed,
        Py,
        Python3
    }
    
    private const string BaseCommand = "py_msu_scripter_app";
    private const string MinVersion = "v0.1.4";
    private PythonCompanionServiceMode _runMethod = PythonCompanionServiceMode.Unknown;
    private string? _pythonExecutablePath;
    private string? _ffmpegPath;
    
    public bool IsValid { get; private set; }
    public bool IsFfMpegValid { get; private set; }
    
    public bool VerifyInstalled()
    {
        if (!VerifyFfMpeg())
        {
            IsValid = false;
            return false;
        }
        
        _runMethod = PythonCompanionServiceMode.Unknown;
        _pythonExecutablePath = null;
        var response = RunCommand("--version");

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
    
    public bool VerifyFfMpeg()
    {
        var ffmpegFolder = Path.Combine(Directories.Dependencies, "ffmpeg", "bin");
        var ffmpegAppName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var ffmpegPath = Path.Combine(ffmpegFolder, ffmpegAppName);

        if (File.Exists(ffmpegPath))
        {
            var installedResult = ValidateInstalledFfmpeg(ffmpegPath);
            if (installedResult.Success && installedResult.Result.StartsWith("ffmpeg version"))
            {
                logger.LogInformation("FFmpeg validated successfully at {Path}: {Result}", ffmpegPath, installedResult.Result);
                IsFfMpegValid = true;
                _ffmpegPath = ffmpegFolder;
                return true;
            }
        }
        
        var pathResult = RunInternal(ffmpegAppName, "-version", true);
        IsFfMpegValid = pathResult.Success && pathResult.Result.StartsWith("ffmpeg version");
        
        if (IsValid)
        {
            logger.LogInformation("FFmpeg validated successfully from environment path: {Result}", pathResult.Result);
        }
        else
        {
            logger.LogError("Failed to validate companion FFmpeg: {Result} | {Error}", pathResult.Result, pathResult.Error);
        }
        
        return IsFfMpegValid;
    }

    private RunPyResult ValidateInstalledFfmpeg(string ffmpegPath, int attempt = 3)
    {
        var installedResult = RunInternal(ffmpegPath, "-version", true);
        if (installedResult.Success && installedResult.Result.StartsWith("ffmpeg version"))
        {
            return installedResult;
        }
        else if (installedResult.Success && installedResult.Result.Equals("") && installedResult.Error.Equals("") && attempt > 0)
        {
            logger.LogWarning("FFmpeg completed successfully without result. Retrying");
            return ValidateInstalledFfmpeg(ffmpegPath, attempt - 1);
        }
        else
        {
            return new RunPyResult()
            {
                Success = false,
                Error = string.IsNullOrEmpty(installedResult.Error) ? "Unknown error running PyMusicLooper" : installedResult.Error
            };
        }
    }

    public async Task<bool> InstallPyApp(Action<string> response)
    {
        var result = await dependencyInstallerService.InstallPyApp(response,
            async (application, arguments) =>
            {
                RunPyResult result = new();
                try
                {
                    await ITaskService.Run(() => { result = RunInternal(application, arguments, true); });
                }
                catch (TaskCanceledException)
                {
                    // Do nothing
                }
                
                return result;
            });
        return result && VerifyInstalled();
    }
    
    public async Task<bool> InstallFfmpeg(Action<string> response)
    {
        var result = await dependencyInstallerService.InstallFfmpeg(response);
        return result && VerifyFfMpeg();
    }

    public GetSampleRateResponse GetSampleRate(GetSampleRateRequest request, CancellationToken? cancellationToken = null)
    {
        if (!IsValid)
        {
            return new GetSampleRateResponse()
            {
                Successful = false,
                Error = "Companion PyMsuScripterApp not validated"
            };
        }

        var ffprobeResponse = GetSampleRateViaFfprobe(request.File, 3);
        if (ffprobeResponse.Successful)
        {
            return ffprobeResponse;
        }
        
        return RunCommand<GetSampleRateRequest, GetSampleRateResponse>(request, cancellationToken);
    }

    private GetSampleRateResponse GetSampleRateViaFfprobe(string file, int attempts = 0)
    {
        var ffprobePath = string.IsNullOrEmpty(_ffmpegPath) ? "ffprobe" : Path.Combine(_ffmpegPath, "ffprobe");
        var ffprobeResponse = RunInternal(ffprobePath, $"-v quiet -show_streams \"{file}\"", true);
        if (ffprobeResponse.Success && ffprobeResponse.Result.StartsWith("[STREAM]"))
        {
            try
            {
                var sampleRate = -1;
                var duration = -1d;
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
                }

                if (sampleRate > 0 && duration > 0)
                {
                    return new GetSampleRateResponse
                    {
                        Successful = true,
                        SampleRate = sampleRate,
                        Duration = duration
                    };
                }
                else
                {
                    logger.LogError("Failed getting valid sample rate and duration: {SampleRate} | {Duration}", sampleRate, duration);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, "Error running ffmprobe");
            }
        }
        else
        {
            logger.LogError("Unable to call ffprobe: {Result} | {Error}", ffprobeResponse.Result, ffprobeResponse.Error);

            if (ffprobeResponse.Success && ffprobeResponse.Result.Equals("") && ffprobeResponse.Error.Equals(""))
            {
                attempts--;
                if (attempts > 0)
                {
                    logger.LogWarning("Retrying ffprobe");
                    return GetSampleRateViaFfprobe(file, attempts);
                }
            }
        }

        return new GetSampleRateResponse
        {
            Successful = false,
            Error = "Unable to get sample rate via ffprobe"
        };
    }

    public RunPyMusicLooperResponse RunPyMusicLooper(RunPyMusicLooperRequest request, CancellationToken? cancellationToken = null)
    {
        if (!IsValid)
        {
            return new RunPyMusicLooperResponse()
            {
                Successful = false,
                Error = "Companion PyMsuScripterApp not validated"
            };
        }
        return RunCommand<RunPyMusicLooperRequest, RunPyMusicLooperResponse>(request, cancellationToken);
    }

    public CreateVideoResponse CreateVideo(CreateVideoRequest request, Action<double>? updateCallback = null, CancellationToken? cancellationToken = null)
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
            ITaskService.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        var text = await File.ReadAllTextAsync(request.ProgressFile, cts.Token);
                        if (text.Contains('|'))
                        {
                            var parts = text.Split("|");
                            var section = int.Parse(parts[0]);
                            var percentage = float.Parse(parts[1]);
                            var totalPercentage = (section * 0.33f) + (percentage / 100 * 0.33);
                            updateCallback(totalPercentage);
                        }
                        else
                        {
                            updateCallback(0);
                        }
                    }
                    catch
                    {
                        updateCallback(0);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                }
            }, cts.Token);
        }
        
        var response = RunCommand<CreateVideoRequest, CreateVideoResponse>(request, cancellationToken);
        cts.Cancel();

        if (updateCallback != null)
        {
            updateCallback(1);
        }
        return response;
    }

    private TResponse RunCommand<TRequest, TResponse>(TRequest request, CancellationToken? cancellationToken) where TResponse : PythonCompanionModeResponse
    {
        var guid = Guid.NewGuid().ToString();
        var inputFile = Path.Combine(Directories.TempFolder, $"{guid}_in.yml");
        var outputFile = Path.Combine(Directories.TempFolder, $"{guid}_out.yml");
        try
        {
            File.WriteAllText(inputFile, yamlService.ToYaml(request!, YamlType.Pascal));

            var runResponse = RunCommand($"--input \"{inputFile}\" --output \"{outputFile}\"", true, cancellationToken);
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

            var outputYaml = File.ReadAllText(outputFile);
            
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
            response!.Successful = false;
            response.Error =  "Failed running PythonCompanionService";
            return response;
        }
    }

    private RunPyResult RunCommand(string command, bool redirectOutput = true, CancellationToken? cancellationToken = null)
    {
        switch (_runMethod)
        {
            case PythonCompanionServiceMode.Unknown:
                var result = RunInternalInstalled(command, redirectOutput, cancellationToken);
                if (result.Success)
                {
                    _runMethod = PythonCompanionServiceMode.Installed;
                    return result;
                }

                result = RunInternalDirect(command, redirectOutput, cancellationToken);
                if (result.Success)
                {
                    _runMethod = PythonCompanionServiceMode.Direct;
                    return result;
                }
                
                result = RunInternalPy(command, redirectOutput, cancellationToken);
                if (result.Success)
                {
                    _runMethod = PythonCompanionServiceMode.Py;
                    return result;
                }
                
                result = RunInternalPython3(command, redirectOutput, cancellationToken);
                if (result.Success)
                {
                    _runMethod = PythonCompanionServiceMode.Python3;
                    return result;
                }
                
                return new RunPyResult
                {
                    Success = false,
                    Error = "No valid run type found"
                };
                
            case PythonCompanionServiceMode.Installed:
                return RunInternalInstalled(command, redirectOutput, cancellationToken);
            case PythonCompanionServiceMode.Direct:
                return RunInternalDirect(command, redirectOutput, cancellationToken);
            case PythonCompanionServiceMode.Py:
                return RunInternalPy(command, redirectOutput, cancellationToken);
            case PythonCompanionServiceMode.Python3:
                return RunInternalPython3(command, redirectOutput, cancellationToken);
            default:
                return new RunPyResult
                {
                    Success = false,
                    Error = "Invalid run type"
                };
        }
    }
    
    private RunPyResult RunInternalDirect(string command, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        return RunInternal(BaseCommand, command, redirectOutput, cancellationToken);
    }
    
    private RunPyResult RunInternalInstalled(string command, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        if (!string.IsNullOrEmpty(_pythonExecutablePath))
        {
            return RunInternal(_pythonExecutablePath, $"-m {BaseCommand} {command}", redirectOutput, cancellationToken);
        }
        
        var exePath = Directories.Dependencies;
        
        if (!string.IsNullOrEmpty(exePath) && Directory.Exists(Path.Combine(exePath, "python")))
        {
            if (OperatingSystem.IsLinux())
            {
                _pythonExecutablePath = Path.Combine(exePath, "python", "bin", "python3.13");
            }
            else
            {
                _pythonExecutablePath = Path.Combine(exePath, "python", "python.exe");
            }

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
        
        return RunInternal(_pythonExecutablePath, $"-m {BaseCommand} {command}", redirectOutput, cancellationToken);
    }

    private RunPyResult RunInternalPy(string command, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        return RunInternal("py", $"-m {BaseCommand} {command}", redirectOutput, cancellationToken);
    }
    
    private RunPyResult RunInternalPython3(string command, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        return RunInternal("python3", $"-m {BaseCommand} {command}",  redirectOutput, cancellationToken);
    }
    
    private Process? RunInternalDirectAsync(string command, bool redirectOutput)
    {
        return RunInternalAsync(BaseCommand, command, redirectOutput);
    }

    private Process? RunInternalPyAsync(string command, bool redirectOutput)
    {
        return RunInternalAsync("py", $"-m {BaseCommand} {command}", redirectOutput);
    }

    private Process? RunInternalPython3Async(string command, bool redirectOutput)
    {
        return RunInternalAsync("python3", $"-m {BaseCommand} {command}", redirectOutput);
    }
    
    private RunPyResult RunInternal(string command, string arguments, bool redirectOutput, CancellationToken? cancellationToken = null)
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
                    RedirectStandardOutput = redirectOutput,
                    RedirectStandardError = redirectOutput,
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
                    RedirectStandardOutput = redirectOutput,
                    RedirectStandardError = redirectOutput,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };
            }
            
            using var process = new Process();
            process.StartInfo = procStartInfo;

            var resultBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                resultBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data?.Contains("Warning:") == true)
                {
                    return;
                }
                errorBuilder.AppendLine(e.Data);
            };
    
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            int checkValue = 0;
            while (cancellationToken?.IsCancellationRequested != true)
            {
                if (process.WaitForExit(TimeSpan.FromMilliseconds(100)))
                {
                    break;
                }

                if (checkValue == 0)
                {
                    logger.LogDebug("Waiting for response from {Command}", innerCommand);
                }

                checkValue = (checkValue + 1) % 10;
            }

            if (cancellationToken?.IsCancellationRequested == true)
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Do nothing
                }

                return new RunPyResult
                {
                    Success = false,
                    Error = "User terminated request"
                };
            }
            
            var resultText = resultBuilder.ToString().Trim();
            var errorText = errorBuilder.ToString().Trim();

            if (!string.IsNullOrEmpty(errorText))
            {
                logger.LogError("Error running {Command}: {Error}", BaseCommand, errorText);
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
    
    private Process? RunInternalAsync(string command, string arguments, bool redirectOutput)
    {
        try
        {
            ProcessStartInfo procStartInfo;
            
            var innerCommand = $"{command} {arguments}";
            logger.LogInformation("Executing async python command: {Command}", innerCommand);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                procStartInfo= new ProcessStartInfo("cmd", "/c " + innerCommand)
                {
                    RedirectStandardOutput = redirectOutput,
                    RedirectStandardError = redirectOutput,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                procStartInfo= new ProcessStartInfo(command)
                {
                    Arguments = arguments,
                    RedirectStandardOutput = redirectOutput,
                    RedirectStandardError = redirectOutput,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            var process = new Process();
            process.StartInfo = procStartInfo;
            if (process.Start())
            {
                return process;
            }

            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unknown error running {Command}", BaseCommand);
            return null;
        }
    }
}

public class GetSampleRateRequest
{
    public string Mode => "samples";
    public required string File { get; set; }
}

public class GetSampleRateResponse : PythonCompanionModeResponse
{
    public double Duration { get; set; }
    public int SampleRate { get; set; }
}

public class RunPyMusicLooperRequest
{
    public string Mode => "py_music_looper";
    public required string File { get; set; }
    public double? MinDurationMultiplier { get; set; } = 0.25f;
    public double? MinLoopDuration { get; set; }
    public double? MaxLoopDuration { get; set; }
    public double? ApproxLoopStart { get; set; }
    public double? ApproxLoopEnd { get; set; }

}

public class RunPyMusicLooperResponse : PythonCompanionModeResponse
{
    public List<PyMusicLooperPair> Pairs { get; set; } = [];
}

public class PyMusicLooperPair
{
    public int LoopStart { get; set; }
    public int LoopEnd { get; set; }
    public double LoudnessDifference { get; set; }
    public double NoteDistance { get; set; }
    public double Score { get; set; }
}

public class PythonCompanionModeResponse
{
    public bool Successful { get; set; }
    public string Error { get; set; } = string.Empty;
}

public class CreateVideoRequest
{
    public string Mode => "create_video";
    public required string OutputVideo { get; set; }
    public string? ProgressFile { get; set; }
    public required List<string> Files { get; set; }
}

public class CreateVideoResponse : PythonCompanionModeResponse;

public class RunPyResult
{
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}