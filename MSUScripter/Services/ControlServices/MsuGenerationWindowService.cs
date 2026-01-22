using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Events;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

// ReSharper disable once ClassNeverInstantiated.Global
public class MsuGenerationWindowService(
    MsuPcmService msuPcmService,
    ProjectService projectService,
    StatusBarService statusBarService,
    TrackListService trackListService,
    ILogger<MsuGenerationWindowService> logger) : ControlService
{
    private readonly MsuGenerationViewModel _model = new();
    
    private readonly CancellationTokenSource _cts = new();

    public event EventHandler<ValueEventArgs<MsuGenerationViewModel>>? PcmGenerationComplete;

    public MsuGenerationViewModel InitializeModel(MsuProject project)
    {
        _model.MsuProject = project;

        var msuDirectory = new FileInfo(project.MsuPath).DirectoryName;
        if (string.IsNullOrEmpty(msuDirectory)) return _model;
        
        var rows = project.Tracks
            .Where(x => !x.IsScratchPad)
            .SelectMany(x => x.Songs)
            .OrderBy(x => x.TrackNumber)
            .Select(x => new MsuGenerationRowViewModel(x))
            .ToList();

        rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.Msu, project));
        
        if (project.BasicInfo.WriteYamlFile)
        {
            rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.Yaml, project));
        }
        
        if (project.BasicInfo.TrackListType != TrackList.Disabled)
        {
            rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.TrackList, project));
        }
        
        if (project.BasicInfo.IncludeJson == true)
        {
            rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.MsuPcmJson, project));
        }
        
        if (project.BasicInfo.CreateAltSwapperScript && project.Tracks.Any(x => x is { IsScratchPad: false, Songs.Count: > 1 }))
        {
            rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.SwapperScript, project));
        }

        if (project.BasicInfo.IsSmz3Project)
        {
            if (!string.IsNullOrEmpty(project.BasicInfo.ZeldaMsuPath))
            {
                rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.Smz3Zelda, project));
            }

            if (!string.IsNullOrEmpty(project.BasicInfo.MetroidMsuPath))
            {
                rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.Smz3Metroid, project));
            }

            if (project.BasicInfo.CreateSplitSmz3Script)
            {
                rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.Smz3Script, project));
            }

            if (project.BasicInfo.WriteYamlFile)
            {
                rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.Smz3MetroidYaml, project));
                rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.Smz3ZeldaYaml, project));
            }
        }

        if (!string.IsNullOrEmpty(_model.ZipPath))
        {
            rows.Add(new MsuGenerationRowViewModel(MsuGenerationRowType.Compress, project, _model.ZipPath));    
        }
        
        _model.Rows = rows;
        _model.TotalSongs = rows.Count;
        
        return _model;
    }

    public void SetZipPath(string path)
    {
        _model.ZipPath = path;
        _model.TotalSongs = _model.TotalSongs * 2 + 1;
        var rows = _model.Rows.Concat([
            new MsuGenerationRowViewModel(MsuGenerationRowType.Compress, _model.MsuProject, _model.ZipPath)
        ]).ToList();
        _model.Rows = rows;
    }

    public void RunGeneration()
    {
        _ = ITaskService.Run(async () =>
        {

            var start = DateTime.Now;

            // If the yaml file exists, but the user doesn't want to use it,
            // then we need to remove it to prevent issues validating the msu
            if (!_model.MsuProject.BasicInfo.WriteYamlFile)
            {
                var yamlPath = Path.ChangeExtension(_model.MsuProject.MsuPath, ".yml");
                if (File.Exists(yamlPath))
                {
                    try
                    {
                        File.Delete(yamlPath);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to delete yaml file");
                    }
                }
            }

            ConcurrentBag<MsuGenerationRowViewModel> toRetry = [];

            var generationRows = _model.Rows.Where(x => x.CanParallelize).ToList();

            await Parallel.ForEachAsync(generationRows,
                new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = _cts.Token }, async (model, _) =>
                {
                    try
                    {
                        await PerformAction(model, toRetry);
                    }
                    catch (Exception)
                    {
                        // Do nothing
                    }
                });

            // For retries, try again linearly
            foreach (var songDetails in toRetry)
            {
                logger.LogInformation("Retrying song {File}", songDetails.Path);
                await ProcessSong(songDetails, true);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(1));

            foreach (var details in _model.Rows.Where(x => !x.CanParallelize)
                         .OrderBy(x => x.Type == MsuGenerationRowType.Compress))
            {
                await PerformAction(details);
            }

            if (_model.NumErrors > 0)
            {
                var errorString = _model.NumErrors == 1 ? "was 1 error" : $"were {_model.NumErrors} errors";
                _model.GenerationErrors.Add($" - There were {errorString} when generating the MSU project files.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            msuPcmService.SaveGenerationCache(_model.MsuProject);

            if (!projectService.ValidateProject(_model.MsuProject, out var validationError))
            {
                _model.GenerationErrors.Add($" - {validationError}");
            }

            var end = DateTime.Now;
            var duration = end - start;
            _model.GenerationSeconds = Math.Round(duration.TotalSeconds, 2);
            _model.IsFinished = true;
            _model.ButtonText = "Close";
            _model.SongsCompleted = _model.TotalSongs;
            statusBarService.UpdateStatusBar("MSU Generated");
            PcmGenerationComplete?.Invoke(this, new ValueEventArgs<MsuGenerationViewModel>(_model));
            
        }, _cts.Token);
    }

    private async Task PerformAction(MsuGenerationRowViewModel rowDetails, ConcurrentBag<MsuGenerationRowViewModel>? toRetry = null)
    {
        try
        {
            if (rowDetails.Type == MsuGenerationRowType.Song)
            {
                if (!await ProcessSong(rowDetails, false))
                {
                    toRetry?.Add(rowDetails);
                }
            }
            else if (rowDetails.Type is MsuGenerationRowType.Msu or MsuGenerationRowType.Smz3Metroid
                     or MsuGenerationRowType.Smz3Zelda)
            {
                if (!File.Exists(rowDetails.Path))
                {
                    await using (File.Create(rowDetails.Path))
                    {
                    }
                }

                rowDetails.Message = "Generated";
                _model.SongsCompleted++;
            }
            else if (rowDetails.Type == MsuGenerationRowType.Yaml)
            {
                projectService.ExportMsuRandomizerYaml(_model.MsuProject, out var error);
                rowDetails.Message = string.IsNullOrEmpty(error) ? "Generated" : error;
                _model.SongsCompleted++;
            }
            else if (rowDetails.Type == MsuGenerationRowType.MsuPcmJson)
            {
                msuPcmService.ExportMsuPcmTracksJson(_model.MsuProject);
                rowDetails.Message =
                    _model.MsuProject.BasicInfo.DitherType is DitherType.DefaultOn or DitherType.DefaultOff
                        ? "Generated with dither value inconsistencies"
                        : "Generated";
                _model.SongsCompleted++;
            }
            else if (rowDetails.Type == MsuGenerationRowType.TrackList)
            {
                trackListService.WriteTrackListFile(_model.MsuProject);
                rowDetails.Message = "Generated";
                _model.SongsCompleted++;
            }
            else if (rowDetails.Type == MsuGenerationRowType.SwapperScript)
            {
                rowDetails.Message = !projectService.CreateAltSwapperFile(_model.MsuProject)
                    ? "Could not create alt swapper bat file. Project file may be corrupt. Verify output pcm file paths."
                    : "Generated";
                _model.SongsCompleted++;
            }
            else if (rowDetails.Type == MsuGenerationRowType.Smz3Script)
            {
                rowDetails.Message = projectService.CreateSmz3SplitScript(_model.MsuProject)
                    ? "Generated"
                    : "Could not generate SMZ3 Split Script";
                _model.SongsCompleted++;
            }
            else if (rowDetails.Type == MsuGenerationRowType.Smz3MetroidYaml)
            {
                rowDetails.Message =
                    projectService.CreateSmz3SplitRandomizerYaml(_model.MsuProject, true, false, out var error)
                        ? "Generated"
                        : error ?? "Could not generate YAML file";
                _model.SongsCompleted++;
            }
            else if (rowDetails.Type == MsuGenerationRowType.Smz3ZeldaYaml)
            {
                rowDetails.Message =
                    projectService.CreateSmz3SplitRandomizerYaml(_model.MsuProject, false, true, out var error)
                        ? "Generated"
                        : error ?? "Could not generate YAML file";
                _model.SongsCompleted++;
            }
            else if (rowDetails.Type == MsuGenerationRowType.Compress)
            {
                Compress(rowDetails);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running action {Type}", rowDetails.Type);
        }
    }

    private void Compress(MsuGenerationRowViewModel compressRow)
    {
        try
        {
            if (File.Exists(_model.ZipPath))
            {
                File.Delete(_model.ZipPath);
            }
            
            using var zip = ZipFile.Open(_model.ZipPath!, ZipArchiveMode.Create);
            
            foreach (var row in _model.Rows.Where(row => row.Type != MsuGenerationRowType.Compress).TakeWhile(_ => !_cts.Token.IsCancellationRequested))
            {
                if (!File.Exists(row.Path))
                {
                    _model.SongsCompleted++;
                    continue;
                }
                
                try
                {
                    zip.CreateEntryFromFile(row.Path, Path.GetFileName(row.Path));
                    row.Message = "Compressed";
                    _model.SongsCompleted++;
                }
                catch (Exception)
                {
                    row.Message = "Failed to add to zip file";
                    _model.SongsCompleted++;
                    return;
                }
            }

            compressRow.Message = "Success!";
            _model.SongsCompleted++;
        }
        catch (Exception)
        {
            compressRow.HasWarning = true;
            compressRow.Message = "Failed to create zip file";
        }
        
        
    }

    public void Cancel()
    {
        if (!_model.IsFinished)
        {
            _cts.Cancel();
        }
    }
    
    private async Task<bool> ProcessSong(MsuGenerationRowViewModel rowDetails, bool isRetry)
    {
        if (_cts.IsCancellationRequested)
        {
            return true;
        }

        if (!_model.MsuProject.BasicInfo.IsMsuPcmProject)
        {
            if (!File.Exists(rowDetails.Path))
            {
                rowDetails.HasWarning = true;
                rowDetails.Message = "PCM file not found";
                _model.NumErrors++;
            }
            else
            {
                rowDetails.Message = "Waiting";
            }
            
            _model.SongsCompleted++;
            return true;
        }
                    
        var songInfo = rowDetails.SongInfo!;
        var generationResponse = await msuPcmService.CreatePcm(_model.MsuProject, songInfo, false, true, true);
        
        if (!generationResponse.Successful)
        {
            // If this is an error for the sox temp file for the first run, ignore so it can be retried
            if (!isRetry && generationResponse.Message?.Contains("__sox_wrapper_temp") == true &&
                generationResponse.Message.Contains("Permission denied"))
            {
                return false;
            }
            // Partially ignore empty pcms with no input files
            else if (generationResponse.Message?.EndsWith("No input files specified") == true && File.Exists(rowDetails.Path) && new FileInfo(rowDetails.Path).Length <= 44500)
            {
                rowDetails.HasWarning = true;
                rowDetails.Message = generationResponse.Message;
            }
            else
            {
                rowDetails.HasWarning = true;
                rowDetails.Message = generationResponse.Message ?? "Unknown error";
                _model.NumErrors++;
            }
                        
        }
        else
        {
            rowDetails.Message = "Generated";
        }
                    
        _model.SongsCompleted++;
        return true;
    }

    public void LogError(Exception ex, string message)
    {
        logger.LogError(ex, "{Message}", message);
    }
}