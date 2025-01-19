using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using MSUScripter.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Settings = MSUScripter.Configs.Settings;

namespace MSUScripter.Services;

public class PyMusicLooperService
{
    private readonly ILogger<PyMusicLooperService> _logger;
    private readonly PythonCommandRunnerService _python;
    private readonly YamlService _yamlService;
    private static readonly Regex digitsOnly = new(@"[^\d.]");
    private bool _hasValidated;
    private const string MinVersion = "3.0.0";
    private const string MinVersionMultipleResults = "3.2.0";
    private bool _canReturnMultipleResults;
    private readonly string _cachePath;
    private int _currentVersion;
    private string _pyMusicLooperCommand = "pymusiclooper";
    private readonly Settings _settings;

    public PyMusicLooperService(ILogger<PyMusicLooperService> logger, PythonCommandRunnerService python, YamlService yamlService, Settings settings)
    {
        _logger = logger;
        _python = python;
        _yamlService = yamlService;
        _settings = settings;
        _cachePath = Path.Combine(Directories.CacheFolder, "pymusiclooper");
        if (!string.IsNullOrEmpty(settings.PyMusicLooperPath) && File.Exists(settings.PyMusicLooperPath))
        {
            _pyMusicLooperCommand = settings.PyMusicLooperPath;
        }
        if (!Directory.Exists(_cachePath))
        {
            Directory.CreateDirectory(_cachePath);
        }
    }

    public bool CanReturnMultipleResults => _canReturnMultipleResults;
    
    public bool IsRunning { get; private set; }
    
    public void ClearCache()
    {
        var cacheDirectory = new DirectoryInfo(_cachePath);
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
                    _logger.LogWarning("Could not delete {File}", file.FullName);
                }
            }
        }
    }

    public List<(int LoopStart, int LoopEnd, decimal Score)>? GetLoopPoints(string filePath, out string message, double minDurationMultiplier = 0.25, int? minLoopDuration = null, int? maxLoopDuration = null, int? approximateLoopStart = null, int? approximateLoopEnd = null, CancellationToken? cancellationToken = null)
    {
        IsRunning = true;
        
        if (!_hasValidated)
        {
            if (!TestService(out message, false))
            {
                IsRunning = false;
                return null;
            }
        }

        if (minLoopDuration != null && minLoopDuration < 1)
        {
            minLoopDuration = 1;
        }
        
        var file = new FileInfo(filePath);

        var path = GetCacheFilePath(file.FullName, minDurationMultiplier, minLoopDuration, maxLoopDuration, approximateLoopStart, approximateLoopEnd);
        if (File.Exists(path))
        {
            var ymlText = File.ReadAllText(path);

            if (_yamlService.FromYaml<List<(int, int, decimal)>>(ymlText, YamlType.UnderscoreIgnoreDefaults, out var result, out _))
            {
                message = "";
                IsRunning = false;
                return result;
            }
        }
        
        var arguments = GetArguments(file.FullName, minDurationMultiplier, minLoopDuration, maxLoopDuration, approximateLoopStart, approximateLoopEnd);
        List<(int, int, decimal)>? loopPoints;

        if (!_canReturnMultipleResults)
        {
            loopPoints = GetLoopPointsSingle(arguments, out message, cancellationToken ?? CancellationToken.None);
        }
        else
        {
            loopPoints = GetLoopPointsMulti(arguments, out message, cancellationToken ?? CancellationToken.None);
        }

        if (loopPoints != null)
        {
            try
            {
                var ymlText = _yamlService.ToYaml(loopPoints, YamlType.UnderscoreIgnoreDefaults);
                File.WriteAllText(path, ymlText);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error saving PyMusicLooper cache");
            }
            
        }

        IsRunning = false;
        return loopPoints;
    }
    
    public bool TestService(out string message, bool force)
    {
        if (_hasValidated && !force)
        {
            message = "";
            return true;
        }
        
        if (!string.IsNullOrEmpty(_settings.PyMusicLooperPath) && File.Exists(_settings.PyMusicLooperPath))
        {
            _pyMusicLooperCommand = _settings.PyMusicLooperPath;
        }
        else
        {
            _pyMusicLooperCommand = "pymusiclooper";
        }

        if (!_python.SetBaseCommand(_pyMusicLooperCommand, "--version", out var result, out _) || !result.StartsWith("pymusiclooper ", StringComparison.OrdinalIgnoreCase))
        {
            message = "Could not run PyMusicLooper. Make sure it's installed and executable in command line.";
            return false;
        }
        
        _logger.LogInformation("{Version} found", result);
        var version = digitsOnly.Replace(result, "").Split(".").Select(int.Parse).ToList();
        _currentVersion = ConvertVersionNumber(version[0], version[1], version[2]);
        _hasValidated = _currentVersion >= GetMinVersionNumber();
        _canReturnMultipleResults = _currentVersion >= GetMinVersionNumberForMultipleResults();
        message = _hasValidated ? "" : $"Minimum required PyMusicLooper version is {MinVersion}";
        return _hasValidated;
    }

    private List<(int, int, decimal)>? GetLoopPointsSingle(string arguments, out string message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing PyMusicLooper: {Command}", arguments);

        var successful = _python.RunCommand(arguments, out var result, out var error, true, cancellationToken);

        if (!successful || !result.Contains("LOOP_START: ") || !result.Contains("LOOP_END: "))
        {
            message = CleanPyMusicLooperError(string.IsNullOrEmpty(error) ? result : error);
            return null;
        }

        var loopStart = -1;
        var loopEnd = -1;
        
        var regex = new Regex(@"LOOP_START: (\d)+");
        var match = regex.Match(result);
        if (match.Success)
        {
            loopStart = int.Parse(match.Groups[0].Value.Split(" ")[1], CultureInfo.InvariantCulture);
        }

        regex = new Regex(@"LOOP_END: (\d)+");
        match = regex.Match(result);
        if (match.Success)
        {
            loopEnd = int.Parse(match.Groups[0].Value.Split(" ")[1], CultureInfo.InvariantCulture);
        }

        if (loopStart == -1 || loopEnd == -1)
        {
            message = "Invalid loop found";
            return null;
        }
        else
        {
            message = "";
            return new List<(int, int, decimal)>() { (loopStart, loopEnd, 0) };
        }
    }
    
    private List<(int, int, decimal)>? GetLoopPointsMulti(string arguments, out string message, CancellationToken cancellationToken)
    {
        arguments += " --alt-export-top -1";
        
        _logger.LogInformation("Executing PyMusicLooper: {Command}", arguments);

        var successful = _python.RunCommand(arguments, out var result, out var error, true, cancellationToken);

        var regexValid = new Regex("^[0-9- .-nae\r\n]+$");
        if (!successful || !regexValid.IsMatch(result))
        {
            message = CleanPyMusicLooperError(string.IsNullOrEmpty(error) ? result : error);
            return null;
        }

        message = "";

        return result.Split("\n")
            .Select(ParsePyMusicLooperLine)
            .Where(x => x != null)
            .Cast<(int, int, decimal)>()
            .ToList();
    }

    private (int, int, decimal)? ParsePyMusicLooperLine(string input)
    {
        var parts = input.Split(" ");
        if (parts.Length < 5)
        {
            return null;
        }
        int.TryParse(parts[0], CultureInfo.InvariantCulture, out var loopStart);
        int.TryParse(parts[1], CultureInfo.InvariantCulture, out var loopEnd);
        decimal.TryParse(parts[4], CultureInfo.InvariantCulture, out var score);
        return (loopStart, loopEnd, score);
    }

    private string GetArguments(string filePath, double minDurationMultiplier = 0.25, int? minLoopDuration = null, int? maxLoopDuration = null, int? approximateLoopStart = null, int? approximateLoopEnd = null)
    {
        var arguments = string.Create(CultureInfo.InvariantCulture, $"export-points --min-duration-multiplier {minDurationMultiplier} --path \"{filePath}\"");
        
        if (minLoopDuration != null)
        {
            arguments += string.Create(CultureInfo.InvariantCulture, $" --min-loop-duration {minLoopDuration}");
        }

        if (maxLoopDuration != null)
        {
            arguments += string.Create(CultureInfo.InvariantCulture, $" --max-loop-duration {maxLoopDuration}");
        }

        if (approximateLoopStart != null && approximateLoopEnd != null)
        {
            arguments += string.Create(CultureInfo.InvariantCulture, $" --approx-loop-position {approximateLoopStart} {approximateLoopEnd}");
        }

        return arguments;
    }

    private int GetMinVersionNumber()
    {
        var version = MinVersion.Split(".").Select(int.Parse).ToList();
        return ConvertVersionNumber(version[0], version[1], version[2]);
    }
    
    private int GetMinVersionNumberForMultipleResults()
    {
        var version = MinVersionMultipleResults.Split(".").Select(int.Parse).ToList();
        return ConvertVersionNumber(version[0], version[1], version[2]);
    }
    
    private int ConvertVersionNumber(int a, int b, int c)
    {
        return a * 10000 + b * 100 + c;
    }

    private string GetCacheFilePath(string path, double minDurationMultiplier = 0.25, int? minLoopDuration = null, int? maxLoopDuration = null, int? approximateLoopStart = null, int? approximateLoopEnd = null)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        var pathHash = GetHexString(md5.ComputeHash(Encoding.Default.GetBytes(path)));
        var fileHash = GetHexString(md5.ComputeHash(stream));
        var fileName = string.Create(CultureInfo.InvariantCulture, $"{pathHash}_{fileHash}_{_currentVersion}_{Math.Round(minDurationMultiplier, 2)}_{minLoopDuration}_{maxLoopDuration}_{approximateLoopStart}_{approximateLoopEnd}.yml");
        return Path.Combine(_cachePath, fileName);
    }

    private string GetHexString(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", string.Empty);
    }

    private string CleanPyMusicLooperError(string message)
    {
        _logger.LogError("PyMusicLooper Error: {Message}", message);
        if (message.Contains("\u2502"))
        {
            message = message.Split("\u2502")[1];
            return message.Trim();
        }
        else
        {
            message = Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(message, @"\s\s+", " "), "@__+", "_"), @"[─╭╮╯╰│]+", ""), @"---+", "-");
            if (message.Contains("+- Error -+"))
            {
                message = "PyMusicLooper Error: " + message.Substring(message.IndexOf("+- Error -+", StringComparison.OrdinalIgnoreCase));    
            }
            return message;
        }
    }
}
