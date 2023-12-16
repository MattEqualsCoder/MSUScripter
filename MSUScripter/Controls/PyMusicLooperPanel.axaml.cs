using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MSUScripter.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class PyMusicLooperPanel : UserControl
{
    private readonly PyMusicLooperService? _pyMusicLooperService;
    private readonly ConverterService? _converterService;
    private readonly MsuPcmService? _msuPcmService;
    private readonly IAudioPlayerService? _audioPlayerService;
    private PyMusicLooperPanelViewModel _model = new();

    public PyMusicLooperPanel() : this(null, null, null, null)
    {
        
    }
    
    public PyMusicLooperPanel(PyMusicLooperService? pyMusicLooperService, ConverterService? converterService, MsuPcmService? msuPcmService, IAudioPlayerService? audioPlayerService)
    {
        _pyMusicLooperService = pyMusicLooperService;
        _converterService = converterService;
        _msuPcmService = msuPcmService;
        _audioPlayerService = audioPlayerService;
        InitializeComponent();
    }

    public PyMusicLooperPanelViewModel Model
    {
        get => _model;
        set
        {
            _model = value;
            DataContext = _model;
        }
    }
    
    public event EventHandler? OnUpdated;

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        TestPyMusicLooper();
    }

    private void TestPyMusicLooper()
    {
        if (_pyMusicLooperService == null)
        {
            return;
        }
        
        if (!_pyMusicLooperService.TestService(out string message))
        {
            _model.Message = message;
            _model.DisplayGitHubLink = true;
            return;
        }

        if (!_pyMusicLooperService.CanReturnMultipleResults)
        {
            _model.DisplayOldVersionWarning = true;
        }

        RunPyMusicLooper();
    }

    public void RunPyMusicLooper()
    {
        if (_pyMusicLooperService == null || string.IsNullOrEmpty(_model.MsuSongInfoViewModel.MsuPcmInfo.File))
        {
            return;
        }

        Task.Run(() =>
        {
            _model.Message = "Running PyMusicLooper";
            
            var inputFile = _model.MsuSongInfoViewModel.MsuPcmInfo.File!;
            var loopPoints = _pyMusicLooperService.GetLoopPoints(inputFile, out string message,
                _model.MinDurationMultiplier,
                _model.MinLoopDuration, _model.MaxLoopDuration,
                _model.ApproximateStart, _model.ApproximateEnd);

            if (loopPoints?.Any() == true)
            {
                _model.PyMusicLooperResults =
                    loopPoints.Select(x => new PyMusicLooperResultViewModel(x.LoopStart, x.LoopEnd, x.Score)).ToList();
                _model.SelectedResult = _model.PyMusicLooperResults.First();
                _model.SelectedResult.IsSelected = true;
                _model.Message = null;
                RunMsuPcm();
                OnUpdated?.Invoke(this, EventArgs.Empty);
                return;
            }

            _model.Message = message;
        });
    }

    private void RunMsuPcm()
    {
        if (_converterService == null || _msuPcmService == null)
        {
            return;
        }

        _model.GeneratingPcms = true;
        
        _msuPcmService.DeleteTempPcms();
        
        var project = _converterService.ConvertProject(_model.MsuProjectViewModel);
        Parallel.ForEach(_model.CurrentPageResults, result =>
        {
            result.Status = "Generating Preview .pcm File";
            
            if (!_msuPcmService.CreateTempPcm(project, _model.MsuSongInfoViewModel.MsuPcmInfo.File!, out var outputPath,
                    out var message, out var generated, result.LoopStart, result.LoopEnd))
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

    private void NextPageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_model.Page >= _model.LastPage)
        {
            return;
        }

        _audioPlayerService?.StopSongAsync().Wait();
        _model.GeneratingPcms = true;
        _model.Page++;
        Task.Run(RunMsuPcm);
    }

    private void PrevPageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_model.Page <= 0)
        {
            return;
        }

        _audioPlayerService?.StopSongAsync().Wait();
        _model.GeneratingPcms = true;
        _model.Page--;
        Task.Run(RunMsuPcm);
    }

    private void PlaySongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_audioPlayerService == null) return;
        if (sender is not Button button) return;
        if (button.Tag is not PyMusicLooperResultViewModel result) return;

        Task.Run(async () =>
        {
            await _audioPlayerService.StopSongAsync();
            _ = _audioPlayerService.PlaySongAsync(result.TempPath, true);
        });
        
    }

    private void SelectedRadioButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton button) return;
        if (button.Tag is not PyMusicLooperResultViewModel result) return;

        _model.SelectedResult = result;
        
        foreach (var otherResult in _model.PyMusicLooperResults.Where(x => x != result))
        {
            otherResult.IsSelected = false;
        }

        result.IsSelected = true;
        OnUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void RunPyMusicLooperButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RunPyMusicLooper();
    }
    
    private void GitHubLink_OnClick(object? sender, RoutedEventArgs e)
    {
        var url = "https://github.com/arkrow/PyMusicLooper";
        
        try
        {
            Process.Start(url);
        }
        catch
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}