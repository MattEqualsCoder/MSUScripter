using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using MSUScripter.Configs;
using MSUScripter.Events;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class PyMusicLooperPanelService(
    PyMusicLooperService pyMusicLooperService,
    ConverterService converterService,
    MsuPcmService msuPcmService,
    IAudioPlayerService audioPlayerService,
    SettingsService settingsService) : ControlService
{
    private readonly PyMusicLooperPanelViewModel _model = new();
    private CancellationTokenSource? _cts;
    private Settings Settings => settingsService.Settings;
    public event EventHandler<PyMusicLooperPanelUpdatedArgs>? OnUpdated;

    public PyMusicLooperPanelViewModel InitializeModel()
    {
        _model.FilteredResultsUpdated += (sender, args) =>
        {
            if (_model.PyMusicLooperResults.Count <= 0) return;
            FilterResults();

            _ = Task.Run(() =>
            {
                RunMsuPcm(false);
            });

        };
        
        return _model;
    }
    
    public void UpdateModel(MsuProjectViewModel msuProjectViewModel, MsuSongInfoViewModel msuSongInfoViewModel, MsuSongMsuPcmInfoViewModel msuSongMsuPcmInfoViewModel)
    {
        _model.MsuProjectViewModel = msuProjectViewModel;
        _model.MsuProject = converterService.ConvertProject(_model.MsuProjectViewModel);
        _model.MsuSongInfoViewModel = msuSongInfoViewModel;
        _model.MsuSongMsuPcmInfoViewModel = msuSongMsuPcmInfoViewModel;
        _model.FilterStart = msuSongMsuPcmInfoViewModel.TrimStart;

        if (Settings.AutomaticallyRunPyMusicLooper)
        {
            RunPyMusicLooper();
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

        _ = Task.Run(() =>
        {
            RunMsuPcm();
            _model.Message = null;
        });
    }

    public async Task PlayResult(PyMusicLooperResultViewModel result)
    {
        await Task.Run(async () =>
        {
            await audioPlayerService.StopSongAsync();

            var songPath = result.TempPath;
            var playSong = true;
            if (!File.Exists(result.TempPath))
            {
                msuPcmService.CreateTempPcm(_model.MsuProject, _model.MsuSongMsuPcmInfoViewModel.File!,
                    out var outputPath,
                    out var message, out var generated, result.LoopStart, result.LoopEnd, skipCleanup: false);
                if (generated)
                {
                    songPath = outputPath;
                }
                else
                {
                    playSong = false;
                }
            }

            if (playSong)
            {
                _ = audioPlayerService.PlaySongAsync(songPath, true);    
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
            _model.Page = 0;
            _model.LastPage = _model.FilteredResults.Count / _model.NumPerPage;
        }
    }
    
    public void TestPyMusicLooper()
    {
        if (!pyMusicLooperService.TestService(out string message))
        {
            _model.Message = message;
            _model.DisplayGitHubLink = true;
            OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(null));
            return;
        }

        if (!pyMusicLooperService.CanReturnMultipleResults)
        {
            _model.DisplayOldVersionWarning = true;
        }
    }

    public void RunPyMusicLooper()
    {
        if (!_model.HasTestedPyMusicLooper)
        {
            TestPyMusicLooper();

            if (_model.DisplayGitHubLink)
            {
                return;
            }
            else if (!_model.DisplayOldVersionWarning)
            {
                _model.HasTestedPyMusicLooper = true;
            }
        }
        
        if (string.IsNullOrEmpty(_model.MsuSongMsuPcmInfoViewModel.File))
        {
            return;
        }

        if (_model.ApproximateStart >= 0 != _model.ApproximateEnd >= 0)
        {
            _model.Message = "Both approximate loop start and end times must be filled out";
            OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(null));
            return;
        }

        _cts = new CancellationTokenSource();

        Task.Run(() =>
        {
            _model.IsRunning = true;
            _model.Message = "Running PyMusicLooper";
            
            var inputFile = _model.MsuSongMsuPcmInfoViewModel.File!;
            var loopPoints = pyMusicLooperService.GetLoopPoints(inputFile, out string message,
                _model.MinDurationMultiplier,
                _model.MinLoopDuration, _model.MaxLoopDuration,
                _model.ApproximateStart, _model.ApproximateEnd,
                _cts.Token);

            if (_cts?.IsCancellationRequested == true)
            {
                _model.Message = "PyMusicLooper canceled";
                _model.IsRunning = false;
                OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(null));
                return;
            }
            else if (loopPoints?.Any() == true)
            {
                _model.PyMusicLooperResults =
                    loopPoints.Select(x => new PyMusicLooperResultViewModel(x.LoopStart, x.LoopEnd, x.Score)).ToList();
                FilterResults();
                _model.SelectedResult = _model.FilteredResults.First();
                _model.SelectedResult.IsSelected = true;
                _model.Message = "Generating Preview Files";
                RunMsuPcm();
                if (_cts?.IsCancellationRequested == true)
                {
                    _model.Message = "PyMusicLooper canceled";
                    _model.IsRunning = false;
                    OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(_model.SelectedResult));
                    return;
                }
                else
                {
                    _model.Message = null;
                    _model.IsRunning = false;
                    OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(_model.SelectedResult));
                    return;
                }
            }

            _model.IsRunning = false;
            OnUpdated?.Invoke(this, new PyMusicLooperPanelUpdatedArgs(_model.SelectedResult));
            _model.Message = message;
        }, _cts.Token);
    }

    public void StopPyMusicLooper()
    {
        _cts?.Cancel();
    }

    private void RunMsuPcm(bool fullReload = true)
    {
        if (_model.CurrentPageResults.All(x => x.Generated))
        {
            return;
        }

        _model.GeneratingPcms = true;

        if (fullReload)
        {
            msuPcmService.DeleteTempPcms();
        }

        try
        {
            Parallel.ForEach(_model.CurrentPageResults.Where(x => !x.Generated), new ParallelOptions()
                {
                    CancellationToken = _cts?.Token ?? CancellationToken.None
                },
                result =>
                {
                    result.Status = "Generating Preview .pcm File";
            
                    if (!msuPcmService.CreateTempPcm(_model.MsuProject, _model.MsuSongMsuPcmInfoViewModel.File!, out var outputPath,
                            out var message, out var generated, result.LoopStart, result.LoopEnd, skipCleanup: true))
                    {
                        if (generated)
                        {
                            result.Status = $"Generated with message: {message}";
                            result.TempPath = outputPath;
                            GetLoopDuration(result);
                            result.Generated = true;
                        }
                        else
                        {
                            result.Status = $"Error: {message}";
                        }
                    }
                    else
                    {
                        result.Status = "Generated";
                        result.TempPath = outputPath;
                        GetLoopDuration(result);
                        result.Generated = true;
                    }
            
                });
        }
        catch
        {
            // Do nothing
        }
        
        _model.GeneratingPcms = false;
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