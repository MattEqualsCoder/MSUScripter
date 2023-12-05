using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
    
    public bool RunCommand(string command, out string result, out string error)
    {
        result = "";
        error = "Unknown error";
        
        switch (_runMethod)
        {
            case RunMethod.Unknown when RunInternalDirect(command, out result, out error):
                _runMethod = RunMethod.Direct;
                return true;
            case RunMethod.Unknown when RunInternalPy(command, out result, out error):
                _runMethod = RunMethod.Py;
                return true;
            case RunMethod.Unknown when RunInternalPython3(command, out result, out error):
                _runMethod = RunMethod.Python3;
                return true;
            case RunMethod.Direct:
                return RunInternalDirect(command, out result, out error);
            case RunMethod.Py:
                return RunInternalPy(command, out result, out error);
            case RunMethod.Python3:
                return RunInternalPython3(command, out result, out error);
            default:
                return false;
        }
    }
    
    public Process? RunCommandAsync(string command)
    {
        switch (_runMethod)
        {
            case RunMethod.Direct:
                return RunInternalDirectAsync(command);
            case RunMethod.Py:
                return RunInternalPyAsync(command);
            case RunMethod.Python3:
                return RunInternalPython3Async(command);
            default:
                return null;
        }
    }

    private bool RunInternalDirect(string command, out string result, out string error)
    {
        return RunInternal(_baseCommand, command, out result, out error);
    }

    private bool RunInternalPy(string command, out string result, out string error)
    {
        return RunInternal("py", $"-m {_baseCommand} {command}", out result, out error);
    }

    private bool RunInternalPython3(string command, out string result, out string error)
    {
        return RunInternal("python3", $"-m {_baseCommand} {command}", out result, out error);
    }
    
    private Process? RunInternalDirectAsync(string command)
    {
        return RunInternalAsync(_baseCommand, command);
    }

    private Process? RunInternalPyAsync(string command)
    {
        return RunInternalAsync("py", $"-m {_baseCommand} {command}");
    }

    private Process? RunInternalPython3Async(string command)
    {
        return RunInternalAsync("python3", $"-m {_baseCommand} {command}");
    }
    
    private bool RunInternal(string command, string arguments, out string result, out string error)
    {
        try
        {
            ProcessStartInfo procStartInfo;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var innerCommand = $"{command} {arguments}";
                procStartInfo= new ProcessStartInfo("cmd", "/c " + innerCommand)
                {
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
    
    private Process? RunInternalAsync(string command, string arguments)
    {
        try
        {
            ProcessStartInfo procStartInfo;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var innerCommand = $"{command} {arguments}";
                procStartInfo= new ProcessStartInfo("cmd", "/c " + innerCommand)
                {
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
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            // wrap IDisposable into using (in order to release hProcess) 
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