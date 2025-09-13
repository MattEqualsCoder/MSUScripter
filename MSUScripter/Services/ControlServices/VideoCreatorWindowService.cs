using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

// ReSharper disable once ClassNeverInstantiated.Global
public class VideoCreatorWindowService(
    ILogger<VideoCreatorWindowService> logger,
    PythonCompanionService companionService,
    MsuPcmService msuPcmService,
    Settings settings) : ControlService
{
    private readonly VideoCreatorWindowViewModel _model = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public VideoCreatorWindowViewModel InitializeModel(MsuProject project)
    {
        _model.PreviousPath = settings.PreviousPath;
        _model.Project = project;
        _model.Songs = project.Tracks.Where(x => !x.IsScratchPad).SelectMany(x => x.Songs).ToList();
        _model.PcmPaths = _model.Songs.Where(x => x.CheckCopyright == true && File.Exists(x.OutputPath))
            .Select(x => x.OutputPath).ToList();
        
        if (_model.PcmPaths.Count == 0)
        {
            _model.DisplayText = "No songs are set to be added to the copyright test";
            return _model;
        }

        if (!companionService.IsValid)
        {
            _model.DisplayText = "Python MSU Scripter Companion app not setup";
            _model.DisplayGitHubLink = true;
        }
        else
        {
            _model.CanRunVideoCreator = true;
        }
        
        return _model;
    }
    
    public bool CanCreateVideo => _model.CanRunVideoCreator;

    public void CreateVideo(string outputPath)
    {
        if (!_model.CanRunVideoCreator) return;

        logger.LogInformation("Creating test video");
        _model.DisplayText = "Creating video (this could take a while)";

        var cts = _cancellationTokenSource = new CancellationTokenSource();

        _model.IsRunning = true;
        
        ITaskService.Run(async () =>
        {
            await Parallel.ForEachAsync(_model.Songs,
                new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = cts.Token }, async (model, _) =>
                {
                    if (cts.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    try
                    {
                        await msuPcmService.CreatePcm(_model.Project!, model, false, true, true);
                    }
                    catch (Exception)
                    {
                        // Do nothing
                    }
                });
            
            var response = await companionService.CreateVideoAsync(new CreateVideoRequest
            {
                Files = _model.PcmPaths.Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToList(),
                OutputVideo = outputPath
            }, progress =>
            {
                _model.Percentage = progress;
            }, cts.Token);

            if (response.Successful)
            {
                _model.DisplayText = "Video generation successful!";
                _model.CloseButtonText = "Close";
            }
            else
            {
                logger.LogError("Error creating video");
                _model.DisplayText =
                    "Error calling msu_test_video_creator. Make sure you can call it manually via console.";
            }
        }, cts.Token);
    }

    public void Cancel()
    {
        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }
        
        logger.LogInformation("Video creation cancellation requested");
        
        _cancellationTokenSource.Cancel();
    }

    public void LogError(Exception ex, string message)
    {
        logger.LogError(ex, "{Message}", message);
    }
}