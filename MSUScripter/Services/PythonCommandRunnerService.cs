using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using MSUScripter.Models;

namespace MSUScripter.Services;

public class PythonCommandRunnerService
{
    private ILogger<PythonCommandRunnerService> _logger;
    private RunMethod _runMethod;
    private string _baseCommand = "";

    public PythonCommandRunnerService(ILogger<PythonCommandRunnerService> logger)
    {
        _logger = logger;
    }

    public bool SetBaseCommand(string baseCommand, string testCommand, out string testResult, out string testError)
    {
        _baseCommand = baseCommand;
        return RunCommand(testCommand, out testResult, out testError);
    }
    
    public bool RunCommand(string command, out string result, out string error, bool redirectOutput = true, CancellationToken? cancellationToken = null)
    {
        result = "";
        error = "Unknown error";
        
        switch (_runMethod)
        {
            case RunMethod.Unknown when RunInternalDirect(command, out result, out error, redirectOutput, cancellationToken):
                _runMethod = RunMethod.Direct;
                return true;
            case RunMethod.Unknown when RunInternalPy(command, out result, out error, redirectOutput, cancellationToken):
                _runMethod = RunMethod.Py;
                return true;
            case RunMethod.Unknown when RunInternalPython3(command, out result, out error, redirectOutput, cancellationToken):
                _runMethod = RunMethod.Python3;
                return true;
            case RunMethod.Direct:
                return RunInternalDirect(command, out result, out error, redirectOutput, cancellationToken);
            case RunMethod.Py:
                return RunInternalPy(command, out result, out error, redirectOutput, cancellationToken);
            case RunMethod.Python3:
                return RunInternalPython3(command, out result, out error, redirectOutput, cancellationToken);
            default:
                return false;
        }
    }
    
    public Process? RunCommandAsync(string command, bool redirectOutput = true)
    {
        switch (_runMethod)
        {
            case RunMethod.Direct:
                return RunInternalDirectAsync(command, redirectOutput);
            case RunMethod.Py:
                return RunInternalPyAsync(command, redirectOutput);
            case RunMethod.Python3:
                return RunInternalPython3Async(command, redirectOutput);
            default:
                return null;
        }
    }

    private bool RunInternalDirect(string command, out string result, out string error, bool redirectOutput, CancellationToken? cancellationToken = null)
    {
        return RunInternal(_baseCommand, command, out result, out error, redirectOutput, cancellationToken);
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
            _logger.LogInformation("Executing python command: {Command}", innerCommand);

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
                _logger.LogDebug("Waiting for response from {Command}", innerCommand);
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
            _logger.LogError("Error running {Command}: {Error}", _baseCommand, error);
            return false;
        }
        catch (Exception e)
        {
            result = "";
            error = $"Unknown error running {_baseCommand}";
            _logger.LogError(e, "Unknown error running {Command}", _baseCommand);
            return false;
        }
    }
    
    private Process? RunInternalAsync(string command, string arguments, bool redirectOutput)
    {
        try
        {
            ProcessStartInfo procStartInfo;
            
            var innerCommand = $"{command} {arguments}";
            _logger.LogInformation("Executing async python command: {Command}", innerCommand);

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
            _logger.LogError(e, "Unknown error running {Command}", _baseCommand);
            return null;
        }
    }
}