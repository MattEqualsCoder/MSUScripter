using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class AddSongWindow : Window
{
    private readonly ILogger<AddSongWindow> _logger = null!;
    private readonly PyMusicLooperPanel _pyMusicLooperPanel = null!;
    private readonly AudioMetadataService _audioMetadataService = null!;
    private readonly AudioAnalysisService _audioAnalysisService = null!;
    private readonly ConverterService _converterService = null!;
    private readonly MsuPcmService _msuPcmService = null!;
    private readonly AudioControl _audioControl = null!;
    private readonly IAudioPlayerService _audioPlayerService = null!;

    private bool _forceClosing;

    public AddSongWindow()
    {
        InitializeComponent();
    }
        
    public AddSongWindow(ILogger<AddSongWindow> logger, PyMusicLooperPanel pyMusicLooperPanel, AudioMetadataService audioMetadataService, AudioAnalysisService audioAnalysisService, MsuPcmService msuPcmService, ConverterService converterService, AudioControl audioControl, IAudioPlayerService audioPlayerService)
    {
        _logger = logger;
        _pyMusicLooperPanel = pyMusicLooperPanel;
        _audioMetadataService = audioMetadataService;
        _audioAnalysisService = audioAnalysisService;
        _msuPcmService = msuPcmService;
        _converterService = converterService;
        _audioControl = audioControl;
        _audioPlayerService = audioPlayerService;
        DataContext = Model;
        InitializeComponent();
        _pyMusicLooperPanel.OnUpdated += PyMusicLooperPanelOnOnUpdated;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    private AddSongWindowViewModel Model { get; set; } = new();

    private MsuProjectViewModel _projectModel = new();
    public MsuProjectViewModel ProjectModel
    {
        get => _projectModel;
        set
        {
            _projectModel = value;
            _project = _converterService.ConvertProject(value);
            LoadMsuTypes();
        }
    }
    
    public int? TrackNumber { get; set; }

    private MsuProject _project = new();

    private void LoadMsuTypes()
    {
        var items = new List<string>() { "Track" };

        var tracks = _projectModel.Tracks.OrderBy(x => x.TrackNumber).ToList();
        foreach (var track in tracks)
        {
            items.Add($"Track #{track.TrackNumber} - {track.TrackName}");
        }

        Model.Tracks = items;
        
        if (TrackNumber == null)
        {
            this.Find<ComboBox>(nameof(MsuTypeComboBox))!.SelectedIndex = 0;    
        }
        else
        {
            this.Find<ComboBox>(nameof(MsuTypeComboBox))!.SelectedIndex =
                tracks.IndexOf(tracks.First(x => x.TrackNumber == TrackNumber)) + 1;
        }
    }

    private void FileControl_OnOnUpdated(object? sender, BasicEventArgs _)
    {
        UpdatePyMusicLooper();
        var path = (sender as FileControl)?.FilePath;
        if (!string.IsNullOrEmpty(path))
        {
            var metadata = _audioMetadataService.GetAudioMetadata(path);
            Model.SongName = metadata.SongName ?? Model.SongName;
            Model.ArtistName = metadata.Artist ?? Model.ArtistName;
            Model.AlbumName = metadata.Album ?? Model.AlbumName;
        }
    }

    private void UpdatePyMusicLooper()
    {
        if (!Model.CanEditMainFields) return;
        _pyMusicLooperPanel.Model = new PyMusicLooperPanelViewModel
        {
            MsuProjectViewModel = _projectModel,
            MsuSongInfoViewModel = new MsuSongInfoViewModel()
            {
                TrackNumber = _projectModel.Tracks.First().TrackNumber,
                MsuPcmInfo = new MsuSongMsuPcmInfoViewModel()
                {
                    File = Model.FilePath
                }
            }
        };
        var parentPanel = this.Find<Panel>(nameof(PyMusicLooperPanel))!;

        if (!parentPanel.Children.Any())
        {
            Model.RunningPyMusicLooper = true;
            parentPanel.Children.Add(_pyMusicLooperPanel);
        }
        else
        {
            Model.RunningPyMusicLooper = true;
            _pyMusicLooperPanel.RunPyMusicLooper();
        }
    }
    
    private void PyMusicLooperPanelOnOnUpdated(object? sender, EventArgs e)
    {
        Model.RunningPyMusicLooper = false;
        
        if (_pyMusicLooperPanel.Model.SelectedResult == null)
        {
            return;
        }
        
        Model.TrimEnd = _pyMusicLooperPanel.Model.SelectedResult.LoopEnd;
        Model.LoopPoint = _pyMusicLooperPanel.Model.SelectedResult.LoopStart;
    }

    private void TestAudioLevelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Task.Run(async () =>
        {
            await StopSong();

            var outputPath = CreateTempPcm();

            if (!string.IsNullOrEmpty(outputPath))
            {
                var output = await _audioAnalysisService.AnalyzeAudio(outputPath);

                if (output is { AvgDecibals: not null, MaxDecibals: not null })
                {
                    Model.AverageAudio = $"Average: {Math.Round(output.AvgDecibals.Value, 2)}db";
                    Model.PeakAudio = $"Peak: {Math.Round(output.MaxDecibals.Value, 2)}db";
                }
                else
                {
                    Model.AverageAudio = "Error analyzing audio";
                    Model.PeakAudio = null;
                }
            }
            else
            {
                Model.AverageAudio = "Error generating PCM";
                Model.PeakAudio = null;
            }
        });
    }

    private void PlaySongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = PlaySong(false);
    }

    private void TestLoopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = PlaySong(true);
    }

    private void StopSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = StopSong();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        this.Find<Panel>(nameof(AudioPanelParent))!.Children.Add(_audioControl);
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Model.SelectedIndex <= 0 || string.IsNullOrEmpty(Model.FilePath))
        {
            return;
        }
        
        var track = _projectModel.Tracks.OrderBy(x => x.TrackNumber).ToList()[Model.SelectedIndex-1];

        var isAlt = track.Songs.Any();
        string outputPath;
        var msu = new FileInfo(_project.MsuPath);
        
        if (!isAlt)
        {
            outputPath = msu.FullName.Replace(msu.Extension, $"-{track.TrackNumber}.pcm");
        }
        else
        {
            var altSuffix = track.Songs.Count == 1 ? "alt" : $"alt{track.Songs.Count}";
            outputPath = msu.FullName.Replace(msu.Extension, $"-{track.TrackNumber}_{altSuffix}.pcm");
        }
        
        track.AddSong(new MsuSongInfoViewModel()
        {
            TrackNumber = track.TrackNumber,
            TrackName = track.TrackName,
            SongName = Model.SongName,
            Artist = Model.ArtistName,
            Album = Model.AlbumName,
            OutputPath = outputPath,
            IsAlt = isAlt,
            LastModifiedDate = DateTime.Now,
            Project = ProjectModel,
            MsuPcmInfo = new MsuSongMsuPcmInfoViewModel()
            {
                Loop = Model.LoopPoint,
                TrimStart = Model.TrimStart,
                TrimEnd = Model.TrimEnd,
                Normalization = Model.Normalization,
                File = Model.FilePath,
                IsTopLevel = true,
                IsAlt = isAlt
            }
        });

        _pyMusicLooperPanel.Model = new();
        Model.Clear();

        Model.AddSongButtonText = "Added Song";
        Task.Run(() =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
            Model.AddSongButtonText = "Add Song";
        });
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClosing)
        {
            return;
        }
        
        if (Model.HasModified)
        {
            var window = new MessageWindow("You currently have unsaved changes. Are you sure you want to close this window?", MessageWindowType.YesNo, "MSU Scripter", this);
            _ = window.ShowDialog(this);
            window.OnButtonClick += MessageWindowButtonClick;
            e.Cancel = true;
        }
    }

    private void MessageWindowButtonClick(object? sender, EventArgs e)
    {
        var window = sender as MessageWindow;
        if (window?.Result == MessageWindowResult.Yes)
        {
            _forceClosing = true;
            Close();
        }
    }

    private void SearchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Model.EnableSearchBox = !Model.EnableSearchBox;
        this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.IsVisible = Model.EnableSearchBox;
        if (Model.EnableSearchBox)
        {
            // Delay because setting focus the first time doesn't work for some reason
            Task.Run(() =>
            {
                Thread.Sleep(50);
                Dispatcher.UIThread.Invoke(() =>
                {
                    this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.Focus();
                });
            });
        }
    }

    private async Task PlaySong(bool fromEnd)
    {
        // Stop the song if it is currently playing
        await StopSong();
        
        var outputPath = CreateTempPcm();
        
        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }
        
        await _audioPlayerService.PlaySongAsync(outputPath, fromEnd);
    }
    
    private async Task StopSong()
    {
        await _audioPlayerService.StopSongAsync(null, true);
    }

    private string? CreateTempPcm()
    {
        _msuPcmService.CreateTempPcm(_project, Model.FilePath, out var outputPath, out _,
            out var generated, Model.LoopPoint, Model.TrimEnd, Model.Normalization ?? ProjectModel.BasicInfo.Normalization, Model.TrimStart);
        return generated ? outputPath : null;
    }

    private void TrackSearchAutoCompleteBox_OnPopulated(object? sender, PopulatedEventArgs e)
    {
        var items = e.Data.Cast<string>().ToList();
        if (items.Count != 1 || string.IsNullOrEmpty(items[0]))
        {
            return;
        }
        else
        {
            Model.SelectedIndex = 0;
        }

        TrackSearch(items.First());
    }
    
    private void TrackSearchAutoCompleteBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = (sender as AutoCompleteBox)?.Text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        TrackSearch(text);
    }

    private void TrackSearch(string selectedItem)
    {
        var track = _projectModel!.Tracks.FirstOrDefault(x =>
            $"Track #{x.TrackNumber} - {x.TrackName}" == selectedItem);

        if (track != null)
        {
            Model.SelectedIndex = _projectModel.Tracks.OrderBy(x => x.TrackNumber).ToList().IndexOf(track) + 1;  
        }
        else
        {
            Model.SelectedIndex = 0;
        }
    }

    
}