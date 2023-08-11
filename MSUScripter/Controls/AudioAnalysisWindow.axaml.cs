using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MSUScripter.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class AudioAnalysisWindow : Window
{
    private readonly AudioAnalysisService? _audioAnalysisService;
    private MsuProjectViewModel? _project;
    private readonly AudioAnalysisViewModel _rows;
    private readonly CancellationTokenSource _cts = new();

    public AudioAnalysisWindow() : this(null)
    {
    }
    
    public AudioAnalysisWindow(AudioAnalysisService? audioAnalysisService)
    {
        _audioAnalysisService = audioAnalysisService;
        InitializeComponent();
        DataContext = _rows = new AudioAnalysisViewModel(); 
    }

    public void SetProject(MsuProjectViewModel project)
    {
        if (_audioAnalysisService == null) return;

        _project = project;

        var msuDirectory = new FileInfo(project.MsuPath).DirectoryName;
        if (string.IsNullOrEmpty(msuDirectory)) return;

        var songs = project.Tracks.SelectMany(x => x.Songs)
            .Where(x => !string.IsNullOrEmpty(x.OutputPath) && File.Exists(x.OutputPath))
            .OrderBy(x => x.TrackNumber)
            .Select(x => new AudioAnalysisSongViewModel()
            {
                SongName = Path.GetRelativePath(msuDirectory, new FileInfo(x.OutputPath!).FullName),
                TrackName = x.TrackName,
                TrackNumber = x.TrackNumber,
                Path = x.OutputPath!,
                OriginalViewModel = x
            })
            .ToList();

        _rows.Rows = songs;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_audioAnalysisService == null || _project == null) return;

        _ = Task.Run(() =>
        {
            _audioAnalysisService!.AnalyzePcmFiles(_project!, _rows.Rows, _cts.Token);

            var avg = GetAverageRms();
            var max = GetAveragePeak();

            if (_cts.Token.IsCancellationRequested) return;

            foreach (var row in _rows.Rows)
            {
                CheckSongWarnings(row, avg, max);
            }
        }, _cts.Token);
    }

    private double GetAverageRms() => Math.Round(_rows.Rows.Average(x => x.AvgDecibals) ?? 0, 4);
    private double GetAveragePeak() => Math.Round(_rows.Rows.Average(x => x.MaxDecibals) ?? 0, 4);

    private void RefreshSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_audioAnalysisService == null || _project == null) return;
        if (sender is not Button button) return;
        if (button.Tag is not AudioAnalysisSongViewModel song) return;
        
        song.AvgDecibals = null;
        song.MaxDecibals = null;
        song.HasWarning = false;
        song.HasLoaded = false;

        _ = Task.Run(() =>
        {
            _audioAnalysisService!.AnalyzePcmFile(_project!, song);
            CheckSongWarnings(song, GetAverageRms(), GetAveragePeak());
        });
    }

    private void CheckSongWarnings(AudioAnalysisSongViewModel song, double averageVolume, double maxVolume)
    {
        if (song.AvgDecibals != null && Math.Abs(song.AvgDecibals.Value - averageVolume) > 4)
        {
            song.HasWarning = true;
            song.WarningMessage =
                $"This song's average volume of {song.AvgDecibals} differs greatly from the average volume of all songs, {averageVolume}";
        }
        else if (song.MaxDecibals != null && song.MaxDecibals - maxVolume > 4)
        {
            song.HasWarning = true;
            song.WarningMessage =
                $"This song's peak volume of {song.MaxDecibals} differs greatly from the average peak volume of all songs, {maxVolume}";
        }
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _cts.Cancel();
    }
}