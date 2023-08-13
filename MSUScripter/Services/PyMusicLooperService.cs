using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MSUScripter.Services;

public class PyMusicLooperService
{
    private readonly ILogger<PyMusicLooperService> _logger;
    private static readonly Regex digitsOnly = new Regex(@"[^\d.]");
    private bool _hasValidated;
    private const string MinVersion = "3.0.0";

    public PyMusicLooperService(ILogger<PyMusicLooperService> logger)
    {
        _logger = logger;
    }

    public bool GetLoopPoints(string filePath, out string message, out int loopStart, out int loopEnd)
    {
        var file = new FileInfo(filePath);
        var arguments = $"export-points --min-duration-multiplier 0.50 --path \"{file.FullName}\"";
        var successful = RunInternal(arguments, out var result, out var error);

        if (!successful || !result.Contains("LOOP_START: ") || !result.Contains("LOOP_END: "))
        {
            loopStart = -1;
            loopEnd = -1;
            message = Regex.Replace(string.IsNullOrEmpty(error) ? result : error, @"\s\s+", " ");
            return false;
        }

        loopStart = -1;
        loopEnd = -1;
        
        var regex = new Regex(@"LOOP_START: (\d)+");
        var match = regex.Match(result);
        if (match.Success)
        {
            
            loopStart = int.Parse(match.Groups[0].Value.Split(" ")[1]);
        }

        regex = new Regex(@"LOOP_END: (\d)+");
        match = regex.Match(result);
        if (match.Success)
        {
            loopEnd = int.Parse(match.Groups[0].Value.Split(" ")[1]);
        }

        message = "";
        return true;
    }

    public bool TestService(out string message)
    {
        if (_hasValidated)
        {
            message = "";
            return true;
        }
        
        RunInternal("--version", out var result, out var error);
        if (!result.StartsWith("pymusiclooper ", StringComparison.OrdinalIgnoreCase))
        {
            message = "Invalid pymusiclooper response.";
            return false;
        }

        var version = digitsOnly.Replace(result, "").Split(".").Select(int.Parse).ToList();
        var versionNum = version[0] * 10000 + version[1] * 100 + version[2];
        _hasValidated = versionNum >= GetMinVersionNumber();
        message = _hasValidated ? "" : $"Minimum pymusiclooper version is {MinVersion}";
        return _hasValidated;
    }
    
    private bool RunInternal(string arguments, out string result, out string error)
    {
        var command = "pymusiclooper";
        
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
            _logger.LogError("Error running PyMusicLooper: {Error}", error);
            return false;
        }
        catch (Exception e)
        {
            result = "";
            error = "Unknown error running PyMusicLooper";
            _logger.LogError(e, "Unknown error running PyMusicLooper");
            return false;
        }
    }
    
    private int GetMinVersionNumber()
    {
        var version = MinVersion.Split(".").Select(int.Parse).ToList();
        return version[0] * 10000 + version[1] * 100 + version[2];
    }
}