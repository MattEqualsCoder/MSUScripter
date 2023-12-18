using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        
        var pcmPaths = project.Tracks.SelectMany(x => x.Songs).Where(x => x.CheckCopyright && File.Exists(x.OutputPath)).Select(x => x.OutputPath);
        var pathsString = string.Join(",", pcmPaths);
        _process = _python.RunCommandAsync($"-f \"{pathsString}\" -o \"{outputPath}\"");

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
            var result = _process.StandardOutput.ReadToEnd().Replace("\0", "").Trim();
            var error = _process.StandardError.ReadToEnd().Replace("\0", "").Trim();
            if (code != 0)
            {
                _logger.LogError("Error calling msu_test_video_creator: {Message}", error);
            }
            VideoCreationCompleted?.Invoke(this, new VideoCreatorServiceEventArgs(code == 0, code == 0 ? "" : error));
        });

        message = "";
        return true;
    }

    public void Cancel()
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