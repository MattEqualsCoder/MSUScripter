using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaControls.Controls;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using MSUScripter.Configs;
using MSUScripter.Events;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class PyMusicLooperDetails
{
    public required MsuProject Project { get; set; }
    public required string? FilePath { get; set; }
    public required int? FilterStart { get; set; }
    public double? Normalization { get; set; }
    public bool AllowRunByDefault { get; set; }
    public bool ForceRun { get; set; }
}

public class PyMusicLooperPanelService(
    PythonCompanionService pythonCompanionService,
    MsuPcmService msuPcmService,
    IAudioPlayerService audioPlayerService,
    SettingsService settingsService) : ControlService
{
    private readonly PyMusicLooperPanelViewModel _model = new();
    private CancellationTokenSource? _cts;
    private Settings Settings => settingsService.Settings;
    public event EventHandler<PyMusicLooperPanelUpdatedArgs>? OnUpdated;
    public event EventHandler<bool>? RunningUpdated;

    public PyMusicLooperPanelViewModel InitializeModel()
    {
        _model.FilteredResultsUpdated += (sender, args) =>
        {
            if (_model.PyMusicLooperResults.Count <= 0) return;
            FilterResults();

            _ = ITaskService.Run(() =>
            {
                var cts = _cts = new CancellationTokenSource();
                RunMsuPcm(false, cts);
            });

        };
        
        return _model;
    }
    
    public void UpdateDetails(PyMusicLooperDetails details)
    {
        _model.AutoRun = Settings.AutomaticallyRunPyMusicLooper;
        _model.FilePath = details.FilePath;
        _model.FilterStart = details.FilterStart;
        _model.MsuProject = details.Project;
        _model.Normalization = details.Normalization;

        if (string.IsNullOrEmpty(_model.FilePath))
        {
            _model.Message = "No file selected. Please select a file and click run.";
            _model.CanRun = false;
            _model.DisplayAutoRun = true;
        }
        else if (details.ForceRun || (Settings.AutomaticallyRunPyMusicLooper && details.AllowRunByDefault))
        {
            RunPyMusicLooper();
        }
        else
        {
            _model.Message = "Click run to execute PyMusicLooper.";
            _model.CanRun = true;
            _model.DisplayAutoRun = true;
        }
    }

    public void UpdateFilterStart(int? filterStart)
    {
        _model.FilterStart = filterStart;
    }

    public async Task ChangePage(int mod)
    {
        if (_model.Page + mod < 0 || _model.Page + mod > _model.LastPage)
        {
            return;
        }
        
        _model.Message = "Generating Preview Files";
        await audioPlayerService.StopSongAsync();
        _model.GeneratingPcms = true;
        _model.Page += mod;

        _ = ITaskService.Run(() =>
        {
            var cts = _cts = new CancellationTokenSource();
            RunMsuPcm(true, cts);
        });
    }

    public async Task PlayResult(PyMusicLooperResultViewModel result)
    {
        await ITaskService.Run(async () =>
        {
            await audioPlayerService.StopSongAsync();

            var songPath = result.TempPath;
            var playSong = true;
            if (!File.Exists(result.TempPath))
            {
                var response = await CreateTempPcm(result, false);
                
                if (response.GeneratedPcmFile)
                {
                    songPath = response.OutputPath;
                }
                else
                {
                    playSong = false;
                }
            }

            if (playSong && !string.IsNullOrEmpty(songPath))
            {
                _ = audioPlayerService.PlaySongAsync(songPath, true, true);    
            }
            else
            {
                _model.Message = "Could not play song";
            }
        });
    }

    public void SelectResult(PyMusicLooperResultViewModel result)
    {
        _model.SelectedResult = result;
        
        foreach (var otherResult in _model.FilteredResults.Where(x => x != result))
        {
            otherResult.IsSelected = false;
        }

        result.IsSelected = true;
        OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(result));
    }
    
    private void FilterResults()
    {
        if (_model.FilterStart == null && _model.FilterEnd == null)
        {
            _model.FilteredResults = _model.PyMusicLooperResults;
        }
        else
        {
            _model.FilteredResults = _model.PyMusicLooperResults.Where(x =>
                    (_model.FilterStart == null || x.LoopStart >= _model.FilterStart) &&
                    (_model.FilterEnd == null || x.LoopEnd <= _model.FilterEnd))
                .ToList();
        }
        
        _model.Page = 0;
        _model.LastPage = _model.FilteredResults.Count / _model.NumPerPage;
    }
    
    public void TestPyMusicLooper()
    {
        if (pythonCompanionService.IsValid)
        {
            return;
        }
        
        _model.Message = "Companion PyMsuScripterApp is not detected";
        _model.DisplayGitHubLink = true;
        OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(null));
    }

    public void RunPyMusicLooper()
    {
        _model.DisplayAutoRun = false;
        RunningUpdated?.Invoke(this, true);
        
        if (!_model.HasTestedPyMusicLooper)
        {
            TestPyMusicLooper();

            if (_model.DisplayGitHubLink)
            {
                RunningUpdated?.Invoke(this, false);
                return;
            }
            
            _model.HasTestedPyMusicLooper = true;
        }
        
        if (string.IsNullOrEmpty(_model.FilePath))
        {
            RunningUpdated?.Invoke(this, false);
            return;
        }

        if (_model.ApproximateStart >= 0 != _model.ApproximateEnd >= 0)
        {
            _model.Message = "Both approximate loop start and end times must be filled out";
            OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(null));
            RunningUpdated?.Invoke(this, false);
            return;
        }

        var cts = _cts = new CancellationTokenSource();

        ITaskService.Run(() =>
        {
            _model.IsRunning = true;
            _model.CanRun = false;
            _model.Message = "Running PyMusicLooper";
            
            var inputFile = _model.FilePath;
            
            var response = pythonCompanionService.RunPyMusicLooper(new RunPyMusicLooperRequest()
            {
                File = inputFile,
                MinDurationMultiplier = _model.MinDurationMultiplier,
                MinLoopDuration = _model.MinLoopDuration,
                MaxLoopDuration = _model.MaxLoopDuration,
                ApproxLoopStart = _model.ApproximateStart,
                ApproxLoopEnd = _model.ApproximateEnd,
            }, cts.Token);
            
            if (cts.IsCancellationRequested)
            {
                _model.Message = "PyMusicLooper canceled";
                _model.IsRunning = false;
                _model.CanRun = true;
                OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(null));
                RunningUpdated?.Invoke(this, false);
                return;
            }
            else if (response is { Successful: true, Pairs.Count: > 0 })
            {
                _model.PyMusicLooperResults =
                    response.Pairs.Select(x => new PyMusicLooperResultViewModel(x.LoopStart, x.LoopEnd, new decimal(x.Score))).ToList();
                FilterResults();

                if (_model.FilteredResults.Count > 0)
                {
                    _model.SelectedResult = _model.FilteredResults.First();
                    _model.SelectedResult.IsSelected = true;
                    _model.Message = "Generating Preview Files";
                    RunMsuPcm(true, cts);
                    if (cts.IsCancellationRequested)
                    {
                        _model.Message = "PyMusicLooper canceled";
                        _model.IsRunning = false;
                        _model.CanRun = true;
                        OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(_model.SelectedResult));
                        RunningUpdated?.Invoke(this, false);
                        return;
                    }
                    else
                    {
                        _model.Message = null;
                        _model.IsRunning = false;
                        _model.CanRun = true;
                        OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(_model.SelectedResult));
                        RunningUpdated?.Invoke(this, false);
                        return;
                    }
                }
                else
                {
                    _model.Message = "No matching results found";
                    _model.IsRunning = false;
                    _model.CanRun = true;
                    OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(null));
                    RunningUpdated?.Invoke(this, false);
                    return;
                }
            }

            _model.IsRunning = false;
            _model.CanRun = true;
            OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(_model.SelectedResult));
            RunningUpdated?.Invoke(this, false);
            _model.Message = response.Error;
        }, cts.Token);
    }
    
    public void StopPyMusicLooper()
    {
        _cts?.Cancel();
    }

    public void SaveAutoRun(bool? value)
    {
        Settings.AutomaticallyRunPyMusicLooper = value ?? false;
        settingsService.SaveSettings();
    }

    private void RunMsuPcm(bool fullReload, CancellationTokenSource cts)
    {
        if (_model.CurrentPageResults.All(x => x.Generated && File.Exists(x.TempPath)))
        {
            _model.Message = null;
            _model.GeneratingPcms = false;
            return;
        }

        _model.GeneratingPcms = true;

        if (fullReload)
        {
            msuPcmService.DeleteTempPcms();
        }

        try
        {
            async void GenerateTempPcm(PyMusicLooperResultViewModel result)
            {
                result.Status = "Generating Preview .pcm File";

                var response = await CreateTempPcm(result, true);

                if (!response.Successful)
                {
                    if (response.GeneratedPcmFile)
                    {
                        result.Status = $"Generated with message: {response.Message}";
                        result.TempPath = response.OutputPath ?? throw new InvalidOperationException("GeneratePcmFileResponse for generated PCM missing output path");
                        GetLoopDuration(result);
                        result.Generated = true;
                    }
                    else
                    {
                        result.Status = $"Error: {response.Message}";
                    }
                }
                else
                {
                    result.Status = "Generated";
                    result.TempPath = response.OutputPath ?? throw new InvalidOperationException("GeneratePcmFileResponse for generated PCM missing output path");
                    GetLoopDuration(result);
                    result.Generated = true;
                }
            }

            Parallel.ForEach(_model.CurrentPageResults.Where(x => !x.Generated || !File.Exists(x.TempPath)), new ParallelOptions()
                {
                    CancellationToken = cts.Token
                },
                GenerateTempPcm);
        }
        catch
        {
            // Do nothing
        }

        if (_cts != cts || cts.IsCancellationRequested)
        {
            _model.PyMusicLooperResults = [];
            _model.FilteredResults = [];
            _model.Message = "Click run to execute PyMusicLooper.";
            _model.CanRun = true;
            _model.DisplayAutoRun = true;
        }
        else
        {
            _model.Message = null;
        }
        _model.GeneratingPcms = false;
    }

    private async Task<GeneratePcmFileResponse> CreateTempPcm(PyMusicLooperResultViewModel result, bool skipCleanup)
    {
        var normalization = _model.Normalization ?? _model.MsuProject.BasicInfo.Normalization;
        return await msuPcmService.CreateTempPcm(false, _model.MsuProject, _model.FilePath, result.LoopStart, result.LoopEnd, normalization, skipCleanup: skipCleanup);
    }

    private void GetLoopDuration(PyMusicLooperResultViewModel song)
    {
        var file = new FileInfo(song.TempPath);
        var lengthSamples = (file.Length - 8) / 4;
        var initBytes = new byte[8];
        using var reader = new BinaryReader(new FileStream(song.TempPath, FileMode.Open));
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        _ = reader.Read(initBytes, 0, 8);
        var loopPoint = BitConverter.ToInt32(initBytes, 4) * 1.0;
        var seconds = (lengthSamples - loopPoint) / 44100.0;
        song.Duration = $@"{TimeSpan.FromSeconds(seconds):mm\:ss\.fff}";
    }
}