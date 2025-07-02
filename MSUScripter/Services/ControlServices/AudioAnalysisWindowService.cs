using System;
using System.IO;
using System.Linq;
using System.Threading;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class AudioAnalysisWindowService(AudioAnalysisService audioAnalysisService, IMsuLookupService msuLookupService) : ControlService
{
    private readonly AudioAnalysisViewModel _model = new();
    private readonly CancellationTokenSource _cts = new();

    public AudioAnalysisViewModel InitializeModel(MsuProject project)
    {
        _model.Project = project;
        
        var msuDirectory = new FileInfo(project.MsuPath).DirectoryName;
        if (string.IsNullOrEmpty(msuDirectory))
        {
            return _model;
        }

        var songs = project.Tracks
            .Where(x => !x.IsScratchPad)
            .SelectMany(x => x.Songs)
            .OrderBy(x => x.TrackNumber)
            .Select(x => new AudioAnalysisSongViewModel()
            {
                SongName = Path.GetRelativePath(msuDirectory, new FileInfo(x.OutputPath!).FullName),
                TrackName = x.TrackName,
                TrackNumber = x.TrackNumber,
                Path = x.OutputPath ?? "",
                MsuSongInfo = x,
            })
            .ToList();

        _model.Rows = songs;
        return _model;
    }

    public AudioAnalysisViewModel InitializeModel(string msuPath)
    {
        _model.ShowCompareButton = false;

        var msuDirectory = new FileInfo(msuPath).DirectoryName;
        if (string.IsNullOrEmpty(msuDirectory))
        {
            _model.LoadError = "Could not load MSU";
            return _model;
        }

        var msu = msuLookupService.LoadMsu(msuPath, null, false, true, true);

        if (msu == null)
        {
            _model.LoadError = "Could not load MSU";
            return _model;
        }

        var songs = msu.Tracks
            .OrderBy(x => x.Number)
            .Select(x => new AudioAnalysisSongViewModel()
            {
                SongName = Path.GetRelativePath(msuDirectory, new FileInfo(x.Path).FullName),
                TrackName = x.TrackName,
                TrackNumber = x.Number,
                Path = x.Path ?? "",
                MsuSongInfo = null,
                CanRefresh = false
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
            
            await audioAnalysisService.AnalyzePcmFiles(_model, _cts.Token);

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
        song.AvgDecibels = null;
        song.MaxDecibels = null;
        song.WarningMessage = "";
        song.HasLoaded = false;

        _ = ITaskService.Run(async () =>
        {
            await audioAnalysisService.AnalyzePcmFile(_model.Project, song);
            CheckSongWarnings(song, GetAverageRms(), GetAveragePeak());
            UpdateBottomMessage();
        }, _cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    public event EventHandler? Completed;
    
    private double GetAverageRms() => Math.Round(_model.Rows.Where(x => x.AvgDecibels != null).Average(x => x.AvgDecibels) ?? 0, 4);
    private double GetAveragePeak() => Math.Round(_model.Rows.Where(x => x.MaxDecibels != null).Average(x => x.MaxDecibels) ?? 0, 4);
    
    private void CheckSongWarnings(AudioAnalysisSongViewModel song, double averageVolume, double maxVolume)
    {
        if (song.AvgDecibels != null && Math.Abs(song.AvgDecibels.Value - averageVolume) > 4)
        {
            song.WarningMessage =
                $"This song's average volume of {song.AvgDecibels} differs greatly from the average volume of all songs, {averageVolume}";
        }
        else if (song.MaxDecibels != null && song.MaxDecibels - maxVolume > 4)
        {
            song.WarningMessage =
                $"This song's peak volume of {song.MaxDecibels} differs greatly from the average peak volume of all songs, {maxVolume}";
        }
    }
    
    private void UpdateBottomMessage()
    {
        _model.BottomBar = $"{GetAverageRms()} Total Average Decibels | {GetAveragePeak()} Average Peak Decibels";
    }
}