using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services;

public class VideoCreatorService
{
    private readonly ILogger<VideoCreatorService> _logger;
    private readonly PythonCommandRunnerService _python;
    private Process? _process;
    private bool _canCreateTestVideo;

    public VideoCreatorService(ILogger<VideoCreatorService> logger, PythonCommandRunnerService python)
    {
        _logger = logger;
        _python = python;
        if (_python.SetBaseCommand("msu_test_video_creator", "--version", out var result, out var error) &&
            result.StartsWith("msu_test_video_creator "))
        {
            _canCreateTestVideo = true;
        }
    }
    
    public event EventHandler<VideoCreatorServiceEventArgs>? VideoCreationCompleted;

    public bool IsRunning { get; private set; }

    public bool CreateVideo(MsuProjectViewModel project, string outputPath, out string message, out bool showGitHub)
    {
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
        var pathsString = string.Join(",", pcmPaths);
        _process = _python.RunCommandAsync($"-f \"{pathsString}\" -o \"{outputPath}\"", false);

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
}