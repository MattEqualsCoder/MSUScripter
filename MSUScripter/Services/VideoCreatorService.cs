using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSUScripter.Models;
using MSUScripter.ViewModels;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MSUScripter.Services;

public class VideoCreatorService
{
    private readonly ILogger<VideoCreatorService> _logger;
    private readonly PythonCommandRunnerService _python;
    private Process? _process;
    private bool _canCreateTestVideo;
    private bool _isOutOfDate;
    private const string MinVersion = "0.2.0";
    private static readonly Regex digitsOnly = new(@"[^\d.]");

    public VideoCreatorService(ILogger<VideoCreatorService> logger, PythonCommandRunnerService python)
    {
        _logger = logger;
        _python = python;
        if (_python.SetBaseCommand("msu_test_video_creator", "--version", out var result, out var error) &&
            result.StartsWith("msu_test_video_creator "))
        {
            logger.LogInformation("{Version} found", result);
            var version = digitsOnly.Replace(result, "").Split(".").Select(int.Parse).ToList();
            var currentVersionNumber = ConvertVersionNumber(version[0], version[1], version[2]);
            var minVersion = digitsOnly.Replace(MinVersion, "").Split(".").Select(int.Parse).ToList();
            var minVersionNumber = ConvertVersionNumber(minVersion[0], minVersion[1], minVersion[2]);
            _canCreateTestVideo = currentVersionNumber >= minVersionNumber;
            _isOutOfDate = !_canCreateTestVideo;
        }
    }
    
    public event EventHandler<VideoCreatorServiceEventArgs>? VideoCreationCompleted;

    public bool IsRunning { get; private set; }

    public bool CreateVideo(MsuProjectViewModel project, string outputPath, out string message, out bool showGitHub)
    {
        if (_isOutOfDate)
        {
            message = $"msu_test_video_creator is out of date. Please upgrade to version {MinVersion}";
            showGitHub = true;
            return false;
        }
        
        if (!_canCreateTestVideo)
        {
            message = "Unable to run msu_test_video_creator";
            _logger.LogError("Error in VideoCreatorService: {Message}", message);
            showGitHub = true;
            return false;
        }

        showGitHub = false;
        if (project.Tracks.SelectMany(x => x.Songs).Any(x => !File.Exists(x.OutputPath)))
        {
            message = "Not all songs have PCM files created. You must generate the MSU first.";
            return false;
        }
        
        _logger.LogInformation("Creating test video");
        
        var pcmPaths = project.Tracks.SelectMany(x => x.Songs).Where(x => x.CheckCopyright && File.Exists(x.OutputPath)).Select(x => x.OutputPath).ToList();
        
        if (!pcmPaths.Any())
        {
            message = "No songs are set to be added to the copyright test";
            return false;
        }

        var pcmFilesData = new Dictionary<string, List<string?>>()
        {
            { "Files", pcmPaths }
        };
        
        var yaml = YamlService.Instance.ToYaml(pcmFilesData, false);
        var path = Path.Combine(Directories.TempFolder, "video-creator-list.yml");
        var directory = new FileInfo(path).DirectoryName;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(path, yaml);
        
        _process = _python.RunCommandAsync($"-i \"{path}\" -o \"{outputPath}\"", false);

        if (_process == null)
        {
            message = "Could not start process";
            _logger.LogError("Error in VideoCreatorService: {Message}", message);
            return false;
        }

        Task.Run(() =>
        {
            IsRunning = true;
            _process.WaitForExit();
            IsRunning = false;
            var code = _process.ExitCode;
            if (code != 0)
            {
                _logger.LogError("Error {Code} calling msu_test_video_creator", code);
            }
            VideoCreationCompleted?.Invoke(this, new VideoCreatorServiceEventArgs(code == 0, code == 0 ? "" : "Error calling msu_test_video_creator. Make sure you can call it manually via console."));
        });
        
        message = "";
        return true;
    }

    public void Cancel()
    {
        if (_process == null)
        {
            return;
        }
        
        if (OperatingSystem.IsWindows())
        {
            KillProcessWindows(_process.Id);
        }
        else
        {
            try
            {
                _process?.Kill();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to kill Video Creator");
            }
        }
    }
    
    [SupportedOSPlatform("windows")]
    private static void KillProcessWindows(int pid)
    {
        var processSearch = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={pid}");
        var processCollection = processSearch.Get();

        if (processCollection.Count > 0)
        {
            foreach (var managementObject in processCollection)
            {
                KillProcessWindows(Convert.ToInt32(managementObject["ProcessID"]));
            }
        }

        // Then kill parents.
        try
        {
            Process proc = Process.GetProcessById(pid);
            if (!proc.HasExited) proc.Kill();
        }
        catch (ArgumentException)
        {
            // Process already exited.
        }
    }
    
    private int ConvertVersionNumber(int a, int b, int c)
    {
        return a * 10000 + b * 100 + c;
    }
}