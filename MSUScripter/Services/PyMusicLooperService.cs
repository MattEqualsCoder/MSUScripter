using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MSUScripter.Services;

public class PyMusicLooperService
{
    private readonly ILogger<PyMusicLooperService> _logger;
    private readonly PythonCommandRunnerService _python;
    private static readonly Regex digitsOnly = new(@"[^\d.]");
    private bool _hasValidated;
    private const string MinVersion = "3.0.0";

    public PyMusicLooperService(ILogger<PyMusicLooperService> logger, PythonCommandRunnerService python)
    {
        _logger = logger;
        _python = python;
    }

    public bool GetLoopPoints(string filePath, out string message, out int loopStart, out int loopEnd)
    {
        var file = new FileInfo(filePath);
        var arguments = $"export-points --min-duration-multiplier 0.25 --path \"{file.FullName}\"";
        var successful = _python.RunCommand(arguments, out var result, out var error);

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

        if (!_python.SetBaseCommand("pymusiclooper", "--version", out var result, out _) || !result.StartsWith("pymusiclooper ", StringComparison.OrdinalIgnoreCase))
        {
            message = "Could not run PyMusicLooper. Make sure it's installed and executable in command line.";
            return false;
        }
        
        var version = digitsOnly.Replace(result, "").Split(".").Select(int.Parse).ToList();
        var versionNum = version[0] * 10000 + version[1] * 100 + version[2];
        _hasValidated = versionNum >= GetMinVersionNumber();
        message = _hasValidated ? "" : $"Minimum PyMusicLooper version is {MinVersion}";
        return _hasValidated;
    }

    private int GetMinVersionNumber()
    {
        var version = MinVersion.Split(".").Select(int.Parse).ToList();
        return version[0] * 10000 + version[1] * 100 + version[2];
    }
}
