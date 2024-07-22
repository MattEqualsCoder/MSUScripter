using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using MSUScripter.Configs;
using MSUScripter.Events;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MsuPcmGenerationWindowService(MsuPcmService msuPcmService, ConverterService converterService, ProjectService projectService, StatusBarService statusBarService) : ControlService
{
    private MsuPcmGenerationViewModel _model = new();
    
    private readonly CancellationTokenSource _cts = new();

    public event EventHandler<ValueEventArgs<MsuPcmGenerationViewModel>>? PcmGenerationComplete;

    public MsuPcmGenerationViewModel InitializeModel(MsuProjectViewModel project, bool exportYaml)
    {
        _model.MsuProjectViewModel = project;
        _model.MsuProject = converterService.ConvertProject(project);
        _model.ExportYaml = exportYaml;
        _model.SplitSmz3 = project.BasicInfo.CreateSplitSmz3Script;

        var msuDirectory = new FileInfo(project.MsuPath).DirectoryName;
        if (string.IsNullOrEmpty(msuDirectory)) return _model;
        
        var songs = project.Tracks.SelectMany(x => x.Songs)
            .OrderBy(x => x.TrackNumber)
            .Select(x => new MsuPcmGenerationSongViewModel()
            {
                SongName = Path.GetRelativePath(msuDirectory, new FileInfo(x.OutputPath!).FullName),
                TrackName = x.TrackName,
                TrackNumber = x.TrackNumber,
                Path = x.OutputPath!,
                OriginalViewModel = x
            })
            .ToList();
        
        _model.Rows = songs;
        _model.TotalSongs = songs.Count;
        
        return _model;
    }

    public void RunGeneration()
    {
        _ = ITaskService.Run(() => {
        
            var start = DateTime.Now;
            
            List<MsuPcmGenerationSongViewModel> toRetry = [];

            Parallel.ForEach(_model.Rows,
                new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = _cts.Token },
                songDetails =>
                {
                    if (!ProcessSong(songDetails, false))
                    {
                        toRetry.Add(songDetails);
                    }
                });

            // For retries, try again linearly
            foreach (var songDetails in toRetry)
            {
                ProcessSong(songDetails, true);
            }

            if (_model.ExportYaml)
            {
                projectService.ExportMsuRandomizerYaml(_model.MsuProject, out var error);

                if (!string.IsNullOrEmpty(error))
                {
                    _model.GenerationErrors.Add($"- YAML file generation failed: {error}");
                }
            }

            if (_model.NumErrors > 0)
            {
                var errorString = _model.NumErrors == 1 ? "was 1 error" : $"were {_model.NumErrors} errors";
                _model.GenerationErrors.Add($"- There {errorString} when running MsuPcm++");
            }

            var end = DateTime.Now;
            var duration = end - start;
            _model.GenerationSeconds = Math.Round(duration.TotalSeconds, 2);
            _model.IsFinished = true;
            _model.ButtonText = "Close";
            _model.SongsCompleted = _model.Rows.Count;
            statusBarService.UpdateStatusBar("MSU Generated");
            PcmGenerationComplete?.Invoke(this, new ValueEventArgs<MsuPcmGenerationViewModel>(_model));

        }, _cts.Token);
    }

    public void Cancel()
    {
        if (!_model.IsFinished)
        {
            _cts.Cancel();
        }
    }
    
    private bool ProcessSong(MsuPcmGenerationSongViewModel songDetails, bool isRetry)
    {
        if (_cts.IsCancellationRequested)
        {
            return true;
        }
                    
        var songViewModel = songDetails.OriginalViewModel;
        var song = new MsuSongInfo();
        converterService.ConvertViewModel(songViewModel, song);
        converterService.ConvertViewModel(songViewModel!.MsuPcmInfo, song.MsuPcmInfo);
        if (!msuPcmService.CreatePcm(false, _model.MsuProject, song, out var error, out var generated))
        {
            // If this is an error for the sox temp file for the first run, ignore so it can be retried
            if (!isRetry && error?.Contains("__sox_wrapper_temp") == true &&
                error.Contains("Permission denied"))
            {
                return false;
            }
            // Partially ignore empty pcms with no input files
            else if (error?.EndsWith("No input files specified") == true && File.Exists(song.OutputPath) && new FileInfo(song.OutputPath).Length <= 44500)
            {
                songDetails.HasWarning = true;
                songDetails.Message = error;
            }
            else
            {
                songDetails.HasWarning = true;
                songDetails.Message = error ?? "Unknown error";
                _model.NumErrors++;
            }
                        
        }
        else
        {
            songViewModel.LastGeneratedDate = DateTime.Now;
            songDetails.Message = "Success!";
        }
                    
        _model.SongsCompleted++;
        return true;
    }
}