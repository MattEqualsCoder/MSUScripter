using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.Controls;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Models;
using Microsoft.Extensions.Logging;
using MSUScripter.ViewModels;
using MSUScripter.Views;

namespace MSUScripter.Services.ControlServices;

public class AddSongWindowService(
    ILogger<AddSongWindowService> logger,
    AudioMetadataService audioMetadataService,
    AudioAnalysisService audioAnalysisService,
    MsuPcmService msuPcmService,
    ConverterService converterService,
    IAudioPlayerService audioPlayerService,
    PyMusicLooperService pyMusicLooperService) : ControlService
{
    private readonly AddSongWindowViewModel _model = new();

    public AddSongWindowViewModel InitializeModel(MsuProjectViewModel project, int? trackNumber)
    {
        _model.MsuProjectViewModel = project;
        _model.MsuProject = converterService.ConvertProject(project);

        var addDescriptions = project.Tracks.First().HasDescription;
        _model.TrackSearchItems = [new ComboBoxAndSearchItem(null, "Track", addDescriptions ? "Default description" : null)];
        _model.TrackSearchItems.AddRange(project.Tracks.OrderBy(x => x.TrackNumber).Select(x =>
            new ComboBoxAndSearchItem(x, $"Track #{x.TrackNumber} - {x.TrackName}", x.Description)));
        _model.SelectedTrack = trackNumber == null ? null : project.Tracks.FirstOrDefault(x => x.TrackNumber == trackNumber);

        _model.HasBeenModified = false;
        
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

        panel.UpdateModel(_model.MsuProjectViewModel, new MsuSongInfoViewModel
        {
            MsuPcmInfo = new MsuSongMsuPcmInfoViewModel
            {
                File = _model.FilePath
            }
        });
    }
    
    public async Task PlaySong(bool fromEnd)
    {
        // Stop the song if it is currently playing
        await StopSong();

        if (string.IsNullOrEmpty(_model.FilePath) || !File.Exists(_model.FilePath))
        {
            return;
        }
        
        var outputPath = CreateTempPcm();
        
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
    
    private string? CreateTempPcm()
    {
        msuPcmService.CreateTempPcm(_model.MsuProject, _model.FilePath, out var outputPath, out _,
            out var generated, _model.LoopPoint, _model.TrimEnd, _model.Normalization ?? _model.MsuProjectViewModel.BasicInfo.Normalization, _model.TrimStart);
        return generated ? outputPath : null;
    }

    public async Task<bool> AddSongToProject(AddSongWindow parent)
    {
        if (_model.SelectedTrack == null || string.IsNullOrEmpty(_model.FilePath))
        {
            return false;
        }

        var track = _model.SelectedTrack;
        
        var successful = msuPcmService.CreateTempPcm(_model.MsuProject, _model.FilePath, out var tempPcmPath, out var message,
            out var generated, _model.LoopPoint, _model.TrimEnd, _model.Normalization ?? _model.MsuProjectViewModel.BasicInfo.Normalization, _model.TrimStart);

        if (!generated)
        {
            var window = new MessageWindow(new MessageWindowRequest
            {
                Message = message ?? "Unknown error",
                Buttons = MessageWindowButtons.OK,
                Icon = MessageWindowIcon.Error
            });
            await window.ShowDialog(parent);
            return false;
        }
        
        if (!successful)
        {
            var window = new MessageWindow(new MessageWindowRequest
            {
                Message = $"{message}\r\nDo you want to continue adding this song?",
                Buttons = MessageWindowButtons.YesNo,
                Icon = MessageWindowIcon.Error
            });
            await window.ShowDialog(parent);
            if (window.DialogResult?.PressedAcceptButton != true)
            {
                return false;
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

        track.AddSong(song);

        _model.AddSongButtonText = "Added Song";
        _ = Task.Run(() =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
            _model.AddSongButtonText = "Add Song";
        });

        return true;
    }

    public void ClearModel()
    {
        _model.Clear();
    }

    public void AnalyzeAudio()
    {
        _model.AverageAudio = "Running";
        _model.PeakAudio = null;
        
        Task.Run(async () =>
        {
            await StopSong(false);

            var outputPath = CreateTempPcm();

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