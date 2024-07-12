using System;
using System.IO;
using System.Linq;
using System.Threading;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class AudioAnalysisWindowService(AudioAnalysisService audioAnalysisService) : ControlService
{
    private readonly AudioAnalysisViewModel _model = new();
    private readonly CancellationTokenSource _cts = new();

    public AudioAnalysisViewModel InitializeModel(MsuProjectViewModel project)
    {
        _model.Project = project;
        
        var msuDirectory = new FileInfo(project.MsuPath).DirectoryName;
        if (string.IsNullOrEmpty(msuDirectory))
        {
            return _model;
        }

        var songs = project.Tracks.SelectMany(x => x.Songs)
            .OrderBy(x => x.TrackNumber)
            .Select(x => new AudioAnalysisSongViewModel()
            {
                SongName = Path.GetRelativePath(msuDirectory, new FileInfo(x.OutputPath!).FullName),
                TrackName = x.TrackName,
                TrackNumber = x.TrackNumber,
                Path = x.OutputPath ?? "",
                OriginalViewModel = x,
            })
            .ToList();

        _model.Rows = songs;
        return _model;
    }

    public void Run()
    {
        _ = ITaskService.Run(async () =>
        {
            var start = DateTime.Now;
            
            await audioAnalysisService.AnalyzePcmFiles(_model.Project, _model, _cts.Token);

            var avg = GetAverageRms();
            var max = GetAveragePeak();

            if (_cts.Token.IsCancellationRequested) return;

            foreach (var row in _model.Rows)
            {
                CheckSongWarnings(row, avg, max);
            }
            
            UpdateBottomMessage();

            var end = DateTime.Now;
            var span = end - start;
            _model.Duration = Math.Round(span.TotalSeconds, 2);
            Completed?.Invoke(this, EventArgs.Empty);
        }, _cts.Token);
    }

    public void RunSong(AudioAnalysisSongViewModel song)
    {
        song.AvgDecibals = null;
        song.MaxDecibals = null;
        song.WarningMessage = "";
        song.HasLoaded = false;

        _ = ITaskService.Run(async () =>
        {
            await audioAnalysisService!.AnalyzePcmFile(_model.Project!, song);
            CheckSongWarnings(song, GetAverageRms(), GetAveragePeak());
            UpdateBottomMessage();
        }, _cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    public event EventHandler? Completed;
    
    private double GetAverageRms() => Math.Round(_model.Rows.Where(x => x.AvgDecibals != null).Average(x => x.AvgDecibals) ?? 0, 4);
    private double GetAveragePeak() => Math.Round(_model.Rows.Where(x => x.MaxDecibals != null).Average(x => x.MaxDecibals) ?? 0, 4);
    
    private void CheckSongWarnings(AudioAnalysisSongViewModel song, double averageVolume, double maxVolume)
    {
        if (song.AvgDecibals != null && Math.Abs(song.AvgDecibals.Value - averageVolume) > 4)
        {
            song.WarningMessage =
                $"This song's average volume of {song.AvgDecibals} differs greatly from the average volume of all songs, {averageVolume}";
        }
        else if (song.MaxDecibals != null && song.MaxDecibals - maxVolume > 4)
        {
            song.WarningMessage =
                $"This song's peak volume of {song.MaxDecibals} differs greatly from the average peak volume of all songs, {maxVolume}";
        }
    }
    
    private void UpdateBottomMessage()
    {
        _model.BottomBar = $"{GetAverageRms()} Total Average Decibals | {GetAveragePeak()} Average Peak Decibals";
    }
}