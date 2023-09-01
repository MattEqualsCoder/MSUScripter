using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class MsuPcmGenerationWindow : Window
{
    private readonly MsuPcmService? _msuPcmService;
    private readonly ConverterService? _converterService;
    private readonly MsuGenerationViewModel _rows;
    private readonly CancellationTokenSource _cts = new();
    private readonly ProjectService? _projectService;
    
    private int _errors;
    private bool _hasFinished;
    private bool _exportYaml;
    private bool _splitSmz3;
    
    public MsuProjectViewModel _projectViewModel { get; set; } = new();
    public MsuProject _project { get; set; } = new();
    
    public MsuPcmGenerationWindow() : this(null, null, null)
    {
        
    }
    
    public MsuPcmGenerationWindow(MsuPcmService? msuPcmService, ConverterService? converterService, ProjectService? projectService)
    {
        _msuPcmService = msuPcmService;
        _converterService = converterService;
        _projectService = projectService;
        InitializeComponent();
        DataContext = _rows = new MsuGenerationViewModel(); 
    }
    
    public void SetProject(MsuProjectViewModel project, bool exportYaml, bool splitSmz3)
    {
        if (_converterService == null || _msuPcmService == null) return;
        
        _projectViewModel = project;
        _project = _converterService.ConvertProject(project);
        _exportYaml = exportYaml;
        _splitSmz3 = splitSmz3;

        var msuDirectory = new FileInfo(project.MsuPath).DirectoryName;
        if (string.IsNullOrEmpty(msuDirectory)) return;
        
        var songs = project.Tracks.SelectMany(x => x.Songs)
            .OrderBy(x => x.TrackNumber)
            .Select(x => new MsuGenerationSongViewModel()
            {
                SongName = Path.GetRelativePath(msuDirectory, new FileInfo(x.OutputPath!).FullName),
                TrackName = x.TrackName,
                TrackNumber = x.TrackNumber,
                Path = x.OutputPath!,
                OriginalViewModel = x
            })
            .ToList();
        
        _rows.Rows = songs;
        _rows.TotalSongs = songs.Count;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        _ = Task.Run(() =>
        {
            var start = DateTime.Now;
            
            Parallel.ForEach(_rows.Rows,
                new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = _cts.Token },
                songDetails =>
                {
                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    var songViewModel = songDetails.OriginalViewModel;
                    var song = new MsuSongInfo();
                    _converterService!.ConvertViewModel(songViewModel, song);
                    _converterService!.ConvertViewModel(songViewModel!.MsuPcmInfo, song.MsuPcmInfo);
                    if (!_msuPcmService!.CreatePcm(_project, song, out var error, out var generated))
                    {
                        songDetails.HasWarning = true;
                        songDetails.Message = error ?? "Unknown error";
                        _errors++;
                    }
                    else
                    {
                        songDetails.Message = "Success!";
                    }
                    
                    _rows.SongsCompleted++;
                });
            
            _hasFinished = true;
            _rows.ButtonText = "Close";
            _rows.SongsCompleted = _rows.Rows.Count;

            if (_exportYaml && _projectService != null)
            {
                _projectService.ExportMsuRandomizerYaml(_project, out var error);
                
                if (_splitSmz3 && !_projectService.CreateSMZ3SplitRandomizerYaml(_project, out error))
                {
                    _ = new MessageWindow($"Error generating SMZ3 YAML: {error}", MessageWindowType.Error,
                        "Error").ShowDialog(this);
                }
            }
            
            Dispatcher.UIThread.Invoke(() =>
            {
                var end = DateTime.Now;
                var duration = end - start;
                Title = $"MSU Export - MSU Scripter (Completed in {Math.Round(duration.TotalSeconds, 2)} seconds)";
                
                if (_errors > 0)
                {
                    var errorString = _errors == 1 ? "was 1 error" : $"were {_errors} errors";
                    _ = new MessageWindow($"MSU Generation Complete. There {errorString} when running MsuPcm++", MessageWindowType.Error,
                        "Error").ShowDialog(this);
                }
                else
                {
                    _ = new MessageWindow($"MSU Generation Completed Successfully", MessageWindowType.Info,
                        "MSU Scripter").ShowDialog(this);
                }
            });

        }, _cts.Token);
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        
        if (!_hasFinished)
        {
            _cts.Cancel();    
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}