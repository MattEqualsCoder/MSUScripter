using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.Controls;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Models;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.ViewModels;
using MSUScripter.Views;

namespace MSUScripter.Services.ControlServices;

public class AddSongWindowService(
    AudioMetadataService audioMetadataService,
    AudioAnalysisService audioAnalysisService,
    MsuPcmService msuPcmService,
    ConverterService converterService,
    IAudioPlayerService audioPlayerService,
    PyMusicLooperService pyMusicLooperService) : ControlService
{
    private readonly AddSongWindowViewModel _model = new();

    public AddSongWindowViewModel InitializeModel(MsuProjectViewModel project, int? trackNumber, string? filePath, bool singleMode)
    {
        _model.MsuProjectViewModel = project;
        _model.MsuProject = converterService.ConvertProject(project);

        var addDescriptions = project.Tracks.First().HasDescription;
        _model.SingleMode = singleMode;
        _model.TrackSearchItems = [new ComboBoxAndSearchItem(null, "Track", addDescriptions ? "Default description" : null)];
        _model.TrackSearchItems.AddRange(project.Tracks.OrderBy(x => x.TrackNumber).Select(x =>
            new ComboBoxAndSearchItem(x, $"Track #{x.TrackNumber} - {x.TrackName}", x.Description)));
        _model.SelectedTrack = trackNumber == null ? null : project.Tracks.FirstOrDefault(x => x.TrackNumber == trackNumber);

        UpdateFilePath(filePath);

        _model.HasBeenModified = false;

        // TODO: Run PyMusicLooper on start if file exists

        return _model;
    }

    public void UpdateFilePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        
        var metadata = audioMetadataService.GetAudioMetadata(path);
        
        _model.FilePath = path;
        _model.SongName = metadata.SongName ?? _model.SongName;
        _model.ArtistName = metadata.Artist ?? _model.ArtistName;
        _model.AlbumName = metadata.Album ?? _model.AlbumName;
        _model.DisplayHertzWarning = audioAnalysisService.GetAudioSampleRate(path) != 44100;
    }

    public void UpdatePyMusicLooperPanel(PyMusicLooperPanel? panel)
    {
        if (panel == null || !_model.CanEditMainFields) return;

        var pcmData = new MsuSongMsuPcmInfoViewModel
        {
            File = _model.FilePath
        };
        
        panel.UpdateModel(_model.MsuProjectViewModel, new MsuSongInfoViewModel
        {
            MsuPcmInfo = pcmData
        }, pcmData);
    }
    
    public async Task PlaySong(bool fromEnd)
    {
        // Stop the song if it is currently playing
        await StopSong();

        if (string.IsNullOrEmpty(_model.FilePath) || !File.Exists(_model.FilePath))
        {
            return;
        }
        
        var outputPath = await CreateTempPcm();
        
        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }
        
        await audioPlayerService.PlaySongAsync(outputPath, fromEnd);
    }
    
    public async Task StopSong(bool wait = true)
    {
        await audioPlayerService.StopSongAsync(null, wait);
    }

    public void UpdateFromPyMusicLooper(PyMusicLooperResultViewModel? result)
    {
        if (result == null)
        {
            return;
        }

        _model.LoopPoint = result.LoopStart;
        _model.TrimEnd = result.LoopEnd;
    }
    
    private async Task<string?> CreateTempPcm()
    {
        var response = await msuPcmService.CreateTempPcm(false, _model.MsuProject, _model.FilePath, _model.LoopPoint,
            _model.TrimEnd, _model.Normalization ?? _model.MsuProjectViewModel.BasicInfo.Normalization,
            _model.TrimStart);
        
        return response.GeneratedPcmFile ? response.OutputPath : null;
    }

    public async Task<MsuSongInfoViewModel?> AddSongToProject(AddSongWindow parent)
    {
        if (_model.SelectedTrack == null || string.IsNullOrEmpty(_model.FilePath))
        {
            return null;
        }

        var track = _model.SelectedTrack;

        var response = await msuPcmService.CreateTempPcm(true, _model.MsuProject, _model.FilePath, _model.LoopPoint,
            _model.TrimEnd, _model.Normalization ?? _model.MsuProjectViewModel.BasicInfo.Normalization,
            _model.TrimStart);

        if (!response.GeneratedPcmFile)
        {
            await MessageWindow.ShowErrorDialog(response.Message ?? "Unknown error", "Error", parent);
            return null;
        }
        
        if (!response.Successful)
        {
            if (!await MessageWindow.ShowYesNoDialog($"{response.Message}\r\nDo you want to continue adding this song?",
                    "Continue?", parent))
            {
                return null;
            }
        }
        
        var isAlt = track.Songs.Any();
        string outputPath;
        var msu = new FileInfo(_model.MsuProjectViewModel.MsuPath);
        
        if (!isAlt)
        {
            outputPath = msu.FullName.Replace(msu.Extension, $"-{track.TrackNumber}.pcm");
        }
        else
        {
            var altSuffix = track.Songs.Count == 1 ? "alt" : $"alt{track.Songs.Count}";
            outputPath = msu.FullName.Replace(msu.Extension, $"-{track.TrackNumber}_{altSuffix}.pcm");
        }

        var song = new MsuSongInfoViewModel
        {
            TrackNumber = track.TrackNumber,
            TrackName = track.TrackName,
            Track = track,
            SongName = _model.SongName,
            Artist = _model.ArtistName,
            Album = _model.AlbumName,
            OutputPath = outputPath,
            IsAlt = isAlt,
            LastModifiedDate = DateTime.Now,
            Project = _model.MsuProjectViewModel,
            MsuPcmInfo = new MsuSongMsuPcmInfoViewModel
            {
                Loop = _model.LoopPoint,
                TrimStart = _model.TrimStart,
                TrimEnd = _model.TrimEnd,
                Normalization = _model.Normalization,
                File = _model.FilePath,
                IsAlt = isAlt
            }
        };

        track.Songs.Add(song);

        _model.AddSongButtonText = "Added Song";
        _ = ITaskService.Run(() =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
            _model.AddSongButtonText = "Add Song";
        });

        return song;
    }

    public void ClearModel()
    {
        _model.Clear();
    }

    public void AnalyzeAudio()
    {
        _model.AverageAudio = "Running";
        _model.PeakAudio = null;
        
        ITaskService.Run(async () =>
        {
            await StopSong(false);

            var outputPath = await CreateTempPcm();

            if (!string.IsNullOrEmpty(outputPath))
            {
                var output = await audioAnalysisService.AnalyzeAudio(outputPath);

                if (output is { AvgDecibals: not null, MaxDecibals: not null })
                {
                    _model.AverageAudio = $"Average: {Math.Round(output.AvgDecibals.Value, 2)}db";
                    _model.PeakAudio = $"Peak: {Math.Round(output.MaxDecibals.Value, 2)}db";
                }
                else
                {
                    _model.AverageAudio = "Error analyzing audio";
                    _model.PeakAudio = null;
                }
            }
            else
            {
                _model.AverageAudio = "Error generating PCM";
                _model.PeakAudio = null;
            }
        });
    }
    
    public bool IsPyMusicLooperRunning() => pyMusicLooperService.IsRunning;

    public bool HasChanges => !string.IsNullOrEmpty(_model.FilePath);
}