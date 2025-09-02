using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Models;

namespace MSUScripter.Services;

public class PythonCompanionService(ILogger<PythonCompanionService> logger, YamlService yamlService)
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
    
    public bool IsValid { get; private set; }
    
    public bool Verify()
    {
        _runMethod = PythonCompanionServiceMode.Unknown;
        _pythonExecutablePath = null;
        IsValid = RunCommand("--version", out var result, out var error) && result.EndsWith(MinVersion);

        if (IsValid)
        {
            logger.LogInformation("Companion PyMsuScripterApp validated successfully: {Result}", result);
        }
        else
        {
            logger.LogInformation("Failed to validate companion PyMsuScripterApp: {Result} | {Error}", result, error);
        }
        
        return IsValid;
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
        return RunCommand<GetSampleRateRequest, GetSampleRateResponse>(request, cancellationToken);
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
            
            if (!RunCommand($"--input \"{inputFile}\" --output \"{outputFile}\"", out _, out var error, true, cancellationToken))
            {
                var response = Activator.CreateInstance<TResponse>();
                response.Successful = false;
                response.Error = error;
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
            
            if (yamlService.FromYaml<TResponse>(outputYaml, YamlType.Pascal, out var result, out error))
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

    private bool RunCommand(string command, out string result, out string error, bool redirectOutput = true,
        CancellationToken? cancellationToken = null)
    {
        result = "";
        error = "";
        
        switch (_runMethod)
        {
            case PythonCompanionServiceMode.Unknown when RunInternalInstalled(command, out result, out error, redirectOutput, cancellationToken):
                _runMethod = PythonCompanionServiceMode.Installed;
                return true;
            case PythonCompanionServiceMode.Unknown when RunInternalDirect(command, out result, out error, redirectOutput, cancellationToken):
                _runMethod = PythonCompanionServiceMode.Direct;
                return true;
            case PythonCompanionServiceMode.Unknown when RunInternalPy(command, out result, out error, redirectOutput, cancellationToken):
                _runMethod = PythonCompanionServiceMode.Py;
                return true;
            case PythonCompanionServiceMode.Unknown when RunInternalPython3(command, out result, out error, redirectOutput, cancellationToken):
                _runMethod = PythonCompanionServiceMode.Python3;
                return true;
            case PythonCompanionServiceMode.Installed:
                return RunInternalInstalled(command, out result, out error, redirectOutput, cancellationToken);
            case PythonCompanionServiceMode.Direct:
                return RunInternalDirect(command, out result, out error, redirectOutput, cancellationToken);
            case PythonCompanionServiceMode.Py:
                return RunInternalPy(command, out result, out error, redirectOutput, cancellationToken);
            case PythonCompanionServiceMode.Python3:
                return RunInternalPython3(command, out result, out error, redirectOutput, cancellationToken);
            default:
                return false;
        }
    }
    
    private bool RunInternalDirect(string command, out string result, out string error, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        return RunInternal(BaseCommand, command, out result, out error, redirectOutput, cancellationToken);
    }
    
    private bool RunInternalInstalled(string command, out string result, out string error, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        if (!string.IsNullOrEmpty(_pythonExecutablePath))
        {
            return RunInternal(_pythonExecutablePath, $"-m {BaseCommand} {command}", out result, out error, redirectOutput, cancellationToken);
        }
        
        var exePath = Path.GetDirectoryName(Environment.ProcessPath);
        
        if (!string.IsNullOrEmpty(exePath) && Directory.Exists(Path.Combine(exePath, "py")))
        {
            if (OperatingSystem.IsLinux())
            {
                _pythonExecutablePath = Path.Combine(exePath, "py", "bin", "python");
            }
            else
            {
                _pythonExecutablePath = Path.Combine(exePath, "py", "python.exe");
            }

            if (!File.Exists(_pythonExecutablePath))
            {
                result = "";
                error = "";
                return false;
            }
        }
        else
        {
            result = "";
            error = "";
            return false;
        }
        
        return RunInternal(_pythonExecutablePath, $"-m {BaseCommand} {command}", out result, out error, redirectOutput, cancellationToken);
    }

    private bool RunInternalPy(string command, out string result, out string error, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        return RunInternal("py", $"-m {BaseCommand} {command}", out result, out error, redirectOutput, cancellationToken);
    }
    
    private bool RunInternalPython3(string command, out string result, out string error, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        return RunInternal("python3", $"-m {BaseCommand} {command}", out result, out error, redirectOutput, cancellationToken);
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
    
    private bool RunInternal(string command, string arguments, out string result, out string error, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        try
        {
            ProcessStartInfo procStartInfo;
            
            var innerCommand = $"{command} {arguments}";
            logger.LogInformation("Executing python command: {Command}", innerCommand);
            
            var workingDirectory = "";
            if (File.Exists(command))
            {
                workingDirectory = Directory.GetParent(command)?.FullName;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!string.IsNullOrEmpty(workingDirectory))
                {
                    var file = Path.GetFileName(command);
                    innerCommand = $"{file} {arguments}";
                }
                
                procStartInfo= new ProcessStartInfo("cmd", "/c " + innerCommand)
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

                result = "";
                error = "";
                return false;
            }
            
            result = resultBuilder.ToString().Trim();
            error = errorBuilder.ToString().Trim();

            if (!string.IsNullOrEmpty(error))
            {
                logger.LogError("Error running {Command}: {Error}", BaseCommand, error);
            }
            
            return true;
        }
        catch (Exception e)
        {
            result = "";
            error = $"Unknown error running {BaseCommand}";
            logger.LogError(e, "Unknown error running {Command}", BaseCommand);
            return false;
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