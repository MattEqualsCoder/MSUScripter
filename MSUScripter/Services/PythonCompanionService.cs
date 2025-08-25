using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace MSUScripter.Services;

public class PythonCompanionService(ILogger<PythonCompanionService> logger)
{
    private enum PythonCompanionServiceMode
    {
        Unknown,
        Direct,
        Installed,
        Py,
        Python3
    }
    
    private PythonCompanionServiceMode _runMethod = PythonCompanionServiceMode.Unknown;
    private string _baseCommand = "py_msu_scripter_app";
    private string? _pythonExecutablePath;
    
    public void Verify()
    {
        var successful = RunCommand("--version", out var result, out var error);
        var a = "1";
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
        return RunInternal(_baseCommand, command, out result, out error, redirectOutput, cancellationToken);
    }
    
    private bool RunInternalInstalled(string command, out string result, out string error, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        if (!string.IsNullOrEmpty(_pythonExecutablePath))
        {
            return RunInternal(_pythonExecutablePath, $"-m {_baseCommand} {command}", out result, out error, redirectOutput, cancellationToken);
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
        
        return RunInternal(_pythonExecutablePath, $"-m {_baseCommand} {command}", out result, out error, redirectOutput, cancellationToken);
    }

    private bool RunInternalPy(string command, out string result, out string error, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        return RunInternal("py", $"-m {_baseCommand} {command}", out result, out error, redirectOutput, cancellationToken);
    }
    
    private bool RunInternalPython3(string command, out string result, out string error, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        return RunInternal("python3", $"-m {_baseCommand} {command}", out result, out error, redirectOutput, cancellationToken);
    }
    
    private Process? RunInternalDirectAsync(string command, bool redirectOutput)
    {
        return RunInternalAsync(_baseCommand, command, redirectOutput);
    }

    private Process? RunInternalPyAsync(string command, bool redirectOutput)
    {
        return RunInternalAsync("py", $"-m {_baseCommand} {command}", redirectOutput);
    }

    private Process? RunInternalPython3Async(string command, bool redirectOutput)
    {
        return RunInternalAsync("python3", $"-m {_baseCommand} {command}", redirectOutput);
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
                errorBuilder.AppendLine(e.Data);
            };
    
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            while (cancellationToken?.IsCancellationRequested != true)
            {
                if (process.WaitForExit(TimeSpan.FromMilliseconds(100)))
                {
                    break;
                }
                logger.LogDebug("Waiting for response from {Command}", innerCommand);
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
            
            if (string.IsNullOrEmpty(error)) return true;
            logger.LogError("Error running {Command}: {Error}", _baseCommand, error);
            return false;
        }
        catch (Exception e)
        {
            result = "";
            error = $"Unknown error running {_baseCommand}";
            logger.LogError(e, "Unknown error running {Command}", _baseCommand);
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
            logger.LogError(e, "Unknown error running {Command}", _baseCommand);
            return null;
        }
    }
}