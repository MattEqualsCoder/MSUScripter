using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class VideoCreatorWindowService(ILogger<VideoCreatorWindowService> logger, PythonCommandRunnerService python) : ControlService
{
    private Process? _process;
    private const string MinVersion = "0.2.0";
    private static readonly Regex DigitsOnly = new(@"[^\d.]");
    private readonly VideoCreatorWindowViewModel _model = new();

    public VideoCreatorWindowViewModel InitializeModel(MsuProjectViewModel project)
    {
        _model.PcmPaths = project.Tracks.SelectMany(x => x.Songs)
            .Where(x => x.CheckCopyright && File.Exists(x.OutputPath)).Select(x => x.OutputPath).ToList();
        
        if (_model.PcmPaths.Count == 0)
        {
            _model.DisplayText = "No songs are set to be added to the copyright test";
            return _model;
        }
        
        if (python.SetBaseCommand("msu_test_video_creator", "--version", out var result, out var error) &&
            result.StartsWith("msu_test_video_creator "))
        {
            logger.LogInformation("{Version} found", result);
            var version = DigitsOnly.Replace(result, "").Split(".").Select(int.Parse).ToList();
            var currentVersionNumber = ConvertVersionNumber(version[0], version[1], version[2]);
            var minVersion = DigitsOnly.Replace(MinVersion, "").Split(".").Select(int.Parse).ToList();
            var minVersionNumber = ConvertVersionNumber(minVersion[0], minVersion[1], minVersion[2]);

            _model.CanRunVideoCreator = currentVersionNumber >= minVersionNumber;
            
            if (!_model.CanRunVideoCreator)
            {
                _model.DisplayText = $"msu_test_video_creator is out of date. Please upgrade to version {MinVersion}";
                _model.DisplayGitHubLink = true;
                logger.LogWarning("{Warning}", _model.DisplayText);
            }
        }
        else
        {
            _model.DisplayText = "Unable to run msu_test_video_creator. Make sure you can run it via command line.";
            _model.DisplayGitHubLink = true;
            logger.LogWarning("Unable to run msu_test_video_creator: {Error}", error);
        }
        
        return _model;
    }
    
    public bool IsRunning { get; private set; }

    public bool CanCreateVideo => _model.CanRunVideoCreator;

    public void CreateVideo(string outputPath)
    {
        if (!_model.CanRunVideoCreator) return;

        logger.LogInformation("Creating test video");
        _model.DisplayText = "Creating video (this could take a while)";
        
        var pcmFilesData = new Dictionary<string, List<string?>>()
        {
            { "Files", _model.PcmPaths }
        };
        
        var yaml = YamlService.Instance.ToYaml(pcmFilesData, false);
        var path = Path.Combine(Directories.TempFolder, "video-creator-list.yml");
        var directory = new FileInfo(path).DirectoryName;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(path, yaml);
        
        _process = python.RunCommandAsync($"-i \"{path}\" -o \"{outputPath}\"", false);

        if (_process == null)
        {
            _model.DisplayText = "Error calling msu_test_video_creator. Make sure you can call it manually via console.";
            _model.DisplayGitHubLink = true;
            logger.LogError("Error in VideoCreatorService: {Message}", _model.DisplayText);
            return;
        }

        ITaskService.Run(() =>
        {
            IsRunning = true;
            _process.WaitForExit();
            IsRunning = false;
            var code = _process.ExitCode;
            if (code != 0)
            {
                logger.LogError("Error {Code} calling msu_test_video_creator", code);
                _model.DisplayText =
                    "Error calling msu_test_video_creator. Make sure you can call it manually via console.";
            }
            else
            {
                _model.DisplayText = "Video generation successful!";
                _model.CloseButtonText = "Close";
            }
        });
    }

    public void Cancel()
    {
        if (_process == null)
        {
            return;
        }
        
        logger.LogInformation("Video creation cancellation requested");
        
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
                logger.LogError(e, "Unable to kill Video Creator");
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