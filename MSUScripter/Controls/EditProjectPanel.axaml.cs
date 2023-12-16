using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class EditProjectPanel : UserControl
{
    private readonly ProjectService? _projectService;
    private readonly MsuPcmService? _msuPcmService;
    private readonly IAudioPlayerService? _audioService;
    private readonly AudioMetadataService? _audioMetadataService;
    private readonly ConverterService? _converterService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly AudioControl? _audioControl;
    private readonly TrackListService? _trackListService;
    private readonly Timer _backupTimer = new Timer(TimeSpan.FromSeconds(60));
    private MsuProject? _project;
    private MsuProjectViewModel? _projectViewModel; 
    private UserControl? _currentPage;
    private bool _hasCheckedPendingChanges;
    private DateTime? _lastAutoSave;
    
    public EditProjectPanel() : this(null, null, null, null, null, null, null, null, null)
    {
        
    }
    
    public EditProjectPanel(IMsuTypeService? msuTypeService, ProjectService? projectService, MsuPcmService? msuPcmService, IAudioPlayerService? audioService, IServiceProvider? serviceProvider, AudioMetadataService? audioMetadataService, ConverterService? converterService, AudioControl? audioControl, TrackListService? trackListService)
    {
        _projectService = projectService;
        _msuPcmService = msuPcmService;
        _audioService = audioService;
        _serviceProvider = serviceProvider;
        _audioMetadataService = audioMetadataService;
        _converterService = converterService;
        _audioControl = audioControl;
        _trackListService = trackListService;
        InitializeComponent();
    }

    public void SetProject(MsuProject project)
    {
        _project = project;
        _projectViewModel = _converterService!.ConvertProject(project);
        DataContext = _projectViewModel;
        PopulatePageComboBox();
        
        _projectService!.CreateMsuFiles(project);

        UpdateStatusBarText(project.LastSaveTime == DateTime.MinValue ? "Project Created" : "Project Loaded");

        if (_audioControl != null)
        {
            this.Find<Panel>(nameof(AudioStackPanel))!.Children.Add(_audioControl);    
        }

        DisplayPage(0);

        _backupTimer.Elapsed += BackupTimerOnElapsed;
        _backupTimer.Start();
    }

    private void BackupTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_lastAutoSave == null)
        {
            _lastAutoSave = _projectViewModel!.LastSaveTime;
        }
        if (!_projectViewModel!.HasChangesSince(_lastAutoSave.Value))
            return;
        var backupProject = _converterService!.ConvertProject(_projectViewModel!);
        _projectService!.SaveMsuProject(backupProject, true);
        _lastAutoSave = DateTime.Now;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void PopulatePageComboBox()
    {
        if (_project == null) return;
        var comboBox = this.Find<ComboBox>(nameof(PageComboBox));
        if (comboBox == null) return;
        
        int currentPage = comboBox.SelectedIndex;
            
        var pages = new List<string>() { "MSU Details" };
            
        foreach (var track in  _project.Tracks.OrderBy(x => x.TrackNumber))
        {
            pages.Add($"Track #{track.TrackNumber} - {track.TrackName}");
        }

        comboBox.ItemsSource = pages;
        comboBox.SelectedIndex = Math.Clamp(currentPage, 0, pages.Count - 1);
    }

    private void PageComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        DisplayPage(this.Find<ComboBox>(nameof(PageComboBox))!.SelectedIndex);
    }
    
    private void DisplayPage(int page)
    {
        if (page < 0 || page >= this.Find<ComboBox>(nameof(PageComboBox))!.Items.Count || _project == null)
            return;

        if (_serviceProvider == null)
            throw new InvalidOperationException("Unable to display track page");

        if (_currentPage is MsuTrackInfoPanel prevPage)
        {
            prevPage.PcmOptionSelected -= PagePanelOnPcmOptionSelected;
            prevPage.MetaDataFileSelected -= SongFileSelected;
            prevPage.FileUpdated -= SongFileSelected; 
        }
        
        this.Find<Panel>(nameof(PagePanel))!.Children.Clear();

        if (page == 0)
        {
            var msuBasicInfoPanel = new MsuBasicInfoPanel(_projectViewModel!.BasicInfo);
            _currentPage = msuBasicInfoPanel;
            this.Find<Panel>(nameof(PagePanel))!.Children.Add(_currentPage);
        }
        else
        {
            var track = _projectViewModel!.Tracks.OrderBy(x => x.TrackNumber).ToList()[page-1];
            var pagePanel = _serviceProvider.GetRequiredService<MsuTrackInfoPanel>();
            pagePanel.SetTrackInfo(_projectViewModel, track);
            pagePanel.PcmOptionSelected += PagePanelOnPcmOptionSelected;
            pagePanel.MetaDataFileSelected += SongFileSelected;
            pagePanel.FileUpdated += SongFileSelected;
            pagePanel.AddSongWindowButtonPressed += PagePanelOnAddSongWindowButtonPressed;
            _currentPage = pagePanel;
            this.Find<Panel>(nameof(PagePanel))!.Children.Add(_currentPage);
        }
    }

    private void PagePanelOnAddSongWindowButtonPressed(object? sender, TrackEventArgs e)
    {
        OpenAddSongWindow(e.TrackNumber);
    }

    private void SongFileSelected(object? sender, SongFileEventArgs e)
    {
        ImportAudioMetadata(e.SongViewModel, e.FilePath, e.Force);
    }

    private void PagePanelOnPcmOptionSelected(object? sender, PcmEventArgs e)
    {
        if (_audioService == null) return;

        if (e.Type == PcmEventType.Play)
        {
            Task.Run(() => PlaySong(e.Song, false));
        }
        else if (e.Type == PcmEventType.PlayLoop)
        {
            Task.Run(() => PlaySong(e.Song, true));
        }
        else if (e.Type == PcmEventType.Generate)
        {
            Task.Run(async () =>
            {
                await StopSong();
                return GeneratePcmFile(e.Song, false, false);
            });
        }
        else if (e.Type == PcmEventType.GenerateAsPrimary)
        {
            Task.Run(async () =>
            {
                await StopSong();
                return GeneratePcmFile(e.Song, true, false);
            });
        }
        else if (e.Type == PcmEventType.GenerateEmpty)
        {
            Task.Run(async () =>
            {
                await StopSong();
                return GeneratePcmFile(e.Song, false, true);
            });
        }
        else if (e.Type == PcmEventType.LoopWindow && _serviceProvider != null && _projectViewModel != null)
        {
            LoopCheck(e.Song, e.PcmInfo);
        }
        else if (e.Type == PcmEventType.StopMusic)
        {
            _ = StopSong();
        }
    }
    
    public async Task PlaySong(MsuSongInfoViewModel songModel, bool fromEnd)
    {
        if (_audioService == null || _projectViewModel == null)
            return;

        // Stop the song if it is currently playing
        await StopSong();
        
        // Regenerate the pcm file if it has updates that have been made to it
        if (_projectViewModel.BasicInfo.IsMsuPcmProject && songModel.HasChangesSince(songModel.LastGeneratedDate) && songModel.HasFiles()) {
            if (!GeneratePcmFile(songModel, false, false))
                    return;
        }
        
        if (string.IsNullOrEmpty(songModel.OutputPath) || !File.Exists(songModel.OutputPath))
        {
            ShowError("No pcm file detected");
            return;
        }
        
        UpdateStatusBarText("Playing Song");
        await _audioService.PlaySongAsync(songModel.OutputPath, fromEnd);
    }
    
    public async Task StopSong()
    {
        if (_audioService == null) return;
        UpdateStatusBarText("Stopping Song");
        await _audioService.StopSongAsync(null, true);
        UpdateStatusBarText("Stopped Song");
    }
    
    public bool GeneratePcmFile(MsuSongInfoViewModel songModel, bool asPrimary, bool asEmpty)
    {
        if (_msuPcmService == null || _project == null) return false;
        
        if (_msuPcmService.IsGeneratingPcm) return false;

        if (asEmpty)
        {
            var emptySong = new MsuSongInfo();
            _converterService!.ConvertViewModel(songModel, emptySong);
            var successful = _msuPcmService.CreateEmptyPcm(emptySong);
            if (!successful)
            {
                ShowError("Could not generate empty pcm file");
                return false;
            }
            UpdateStatusBarText("PCM Generated");
            return true;
        }
        
        if (!songModel.HasFiles())
        {
            ShowError("No files specified to generate into a pcm file");
            return false;
        }
        
        UpdateStatusBarText("Generating PCM");
        var song = new MsuSongInfo();
        _converterService!.ConvertViewModel(songModel, song);
        _converterService!.ConvertViewModel(songModel.MsuPcmInfo, song.MsuPcmInfo);

        if (asPrimary)
        {
            var msu = new FileInfo(_project.MsuPath);
            var path = msu.FullName.Replace(msu.Extension, $"-{song.TrackNumber}.pcm");
            song.OutputPath = path;
        }
        
        if (!_msuPcmService.CreatePcm(_project, song, out var message, out var generated))
        {
            if (generated)
            {
                UpdateStatusBarText("PCM Generated with Warning");

                if (!_projectViewModel!.IgnoreWarnings.Contains(song.OutputPath))
                {
                    Task<MessageWindowResult?>? task = null;
                
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        var window = new MessageWindow(message ?? "Unknown error with msupcm++",
                            MessageWindowType.PcmWarning, "msupcm++ Warning");
                        task = window.ShowDialog();
                    });

                    task?.Wait();
                    var result = task?.Result;

                    if (result == MessageWindowResult.DontShow)
                    {
                        _projectViewModel!.IgnoreWarnings.Add(song.OutputPath);
                    }
                }
                
                songModel.LastGeneratedDate = DateTime.Now;
                return true;
            }
            else
            {
                UpdateStatusBarText("msupcm++ Error");
                ShowError(message ?? "Unknown error with msupcm++", "msupcm++ Error");
                return false;
            }
        }
        
        songModel.LastGeneratedDate = DateTime.Now;
        UpdateStatusBarText("PCM Generated");
        return true;
    }

    public void UpdateStatusBarText(string message)
    {
        if (!CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => UpdateStatusBarText(message));
            return;
        }
        
        this.Find<TextBlock>(nameof(StatusMessage))!.Text = message;
    }
    
    public void ImportAudioMetadata(MsuSongInfoViewModel songModel, string file, bool force = false)
    {
        var metadata =  _audioMetadataService?.GetAudioMetadata(file);
        if (metadata?.HasData != true) return;
        if (force || string.IsNullOrEmpty(songModel.SongName) || songModel.SongName.StartsWith("Track #"))
            songModel.SongName = metadata.SongName;
        if (force || (string.IsNullOrEmpty(songModel.Artist) && !string.IsNullOrEmpty(metadata.Artist)))
            songModel.Artist = metadata.Artist;
        if (force || (string.IsNullOrEmpty(songModel.Album) && !string.IsNullOrEmpty(metadata.Album)))
            songModel.Album = metadata.Album;
        if (force || (string.IsNullOrEmpty(songModel.Url) && !string.IsNullOrEmpty(metadata.Url)))
            songModel.Url = metadata.Url;
    }

    public void SaveProject()
    {
        if (_projectViewModel == null || _projectService == null) return;
        _project = _converterService!.ConvertProject(_projectViewModel);
        _projectService.SaveMsuProject(_project, false);
        _projectViewModel.LastSaveTime = _project.LastSaveTime;
        _lastAutoSave = _project.LastSaveTime;
        UpdateStatusBarText("Project Saved");
    }

    public bool HasPendingChanges()
    {
        if (_hasCheckedPendingChanges) return false;
        if (_projectViewModel == null) return false;
        _hasCheckedPendingChanges = true;
        return _projectViewModel.HasPendingChanges();
    }

    public async Task CheckPendingChanges()
    {
        if (!HasPendingChanges()) return;
        var result = await ShowYesNoWindow("You currently have unsaved changes. Do you want to save your changes?");
        if (result == MessageWindowResult.Yes)
        {
            SaveProject();
        }
    }

    private void NextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var comboBox = this.Find<ComboBox>(nameof(PageComboBox))!;
        var newIndex = comboBox.SelectedIndex + 1;
        if (newIndex < comboBox.Items.Count)
            comboBox.SelectedIndex = newIndex;
    }

    private void PrevButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var comboBox = this.Find<ComboBox>(nameof(PageComboBox))!;
        var newIndex = comboBox.SelectedIndex - 1;
        if (newIndex >= 0)
            comboBox.SelectedIndex = newIndex;
    }

    private void ExportButton_Yaml_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_projectViewModel == null || _projectService == null) return;
        _project = _converterService!.ConvertProject(_projectViewModel);
        ExportYaml(_project);
        UpdateStatusBarText("YAML File Written");
    }

    private void ExportYaml(MsuProject project)
    {
        if (!project.BasicInfo.WriteYamlFile) return;
        _projectService!.ExportMsuRandomizerYaml(project, out var error);
        if (!string.IsNullOrEmpty(error))
        {
            ShowError(error);
            return;
        }
        
        // Try to create the extra SMZ3 YAML files
        if (project.BasicInfo.CreateSplitSmz3Script && !_projectService.CreateSMZ3SplitRandomizerYaml(project, out error))
        {
            ShowError(error ?? "Unknown error creating YAML file");
        }
    }

    private void ShowError(string message, string title = "Error")
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => { ShowError(message, title); });
            return;
        }
        
        _ = new MessageWindow(message, MessageWindowType.Error, title).ShowDialog();
    }
    
    private async Task<MessageWindowResult?> ShowYesNoWindow(string message, string title = "MSU Scripter")
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            MessageWindowResult? result = null;
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = new MessageWindow(message, MessageWindowType.YesNo, title);
                result = await window.ShowDialog();
            });
            return result;
        }

        var window = new MessageWindow(message, MessageWindowType.YesNo, title);
        return await window.ShowDialog();
    }

    private void ExportButton_Json_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_msuPcmService == null || _projectViewModel == null) return;
        _project = _converterService!.ConvertProject(_projectViewModel);
        _msuPcmService.ExportMsuPcmTracksJson(_project);
        UpdateStatusBarText("Json File Written");
    }

    private void ExportButton_Msu_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_msuPcmService == null || _projectViewModel == null) return;
        _project = _converterService!.ConvertProject(_projectViewModel);
        _msuPcmService.ExportMsuPcmTracksJson(_project);
        Task.Run(() => DisplayMsuGenerationWindow(false));
    }

    private void ExportButton_Swapper_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_projectService == null || _projectViewModel == null) return;
        _project = _converterService!.ConvertProject(_projectViewModel);
        
        var extraProjects = new List<MsuProject>();

        if (_project.BasicInfo.CreateSplitSmz3Script)
        {
            extraProjects = _projectService.GetSmz3SplitMsuProjects(_project, out var _, out var error).ToList();
            if (!string.IsNullOrEmpty(error))
            {
                ShowError(error);
            }
        }
        
        _projectService.CreateAltSwapperFile(_project, extraProjects);
    }

    private void ExportButton_Smz3_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_projectService == null || _projectViewModel == null) return;
        _project = _converterService!.ConvertProject(_projectViewModel);
        _projectService.GetSmz3SplitMsuProjects(_project, out var conversions, out var error);
        if (!string.IsNullOrEmpty(error))
        {
            ShowError(error);
            return;
        }
        _projectService.CreateSmz3SplitScript(_project, conversions);
    }

    private void ExportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_projectService == null || _projectViewModel == null) return;
        _project = _converterService!.ConvertProject(_projectViewModel);
        _projectService.CreateMsuFiles(_project);
        var extraProjects = new List<MsuProject>();

        if (_project.BasicInfo.CreateSplitSmz3Script)
        {
            extraProjects = _projectService.GetSmz3SplitMsuProjects(_project, out var conversions, out var error).ToList();
            
            if (!string.IsNullOrEmpty(error))
            {
                ShowError(error);
                return;
            }
            
            _projectService.CreateSmz3SplitScript(_project, conversions);
        }
        
        if (_project.BasicInfo.CreateAltSwapperScript)
        {
            _projectService.CreateAltSwapperFile(_project, extraProjects);
        }

        if (_project.BasicInfo.TrackList != TrackListType.Disabled)
        {
            WriteTrackList(_project);
        }
        
        if (!_project.BasicInfo.IsMsuPcmProject || _msuPcmService == null)
        {
            ExportYaml(_project);
            UpdateStatusBarText("Export Complete");
            return;
        }
        
        _msuPcmService.ExportMsuPcmTracksJson(_project);
        Task.Run(() => DisplayMsuGenerationWindow(_project.BasicInfo.WriteYamlFile));
    }
    
    private async Task DisplayMsuGenerationWindow(bool exportYaml)
    {
        if (_projectViewModel == null || _msuPcmService == null || _serviceProvider == null) return;
        if (_msuPcmService.IsGeneratingPcm) return;
        
        if (_audioService != null)
        {
            UpdateStatusBarText("Stopping Song");
            await _audioService.StopSongAsync(null, true);
            UpdateStatusBarText("Stopped Song");
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            var msuPcmGenerationWindow = _serviceProvider.GetRequiredService<MsuPcmGenerationWindow>();
            msuPcmGenerationWindow.SetProject(_projectViewModel, exportYaml, _project!.BasicInfo.CreateSplitSmz3Script);
            msuPcmGenerationWindow.ShowDialog(App.MainWindow!);
            UpdateStatusBarText("MSU Generated");
        });
        
    }

    private void WriteTrackList(MsuProject? project = null)
    {
        if (_projectViewModel == null || _trackListService == null) return;
        if (project == null)
        {
            _project = project = _converterService!.ConvertProject(_projectViewModel);
        }
        _trackListService.WriteTrackListFile(project);
    }

    private AudioAnalysisWindow? _audioAnalysisWindow;

    private void AnalysisButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (_serviceProvider == null || _projectViewModel == null || topLevel == null) return;
        _audioAnalysisWindow = _serviceProvider.GetRequiredService<AudioAnalysisWindow>();
        _audioAnalysisWindow.SetProject(_projectViewModel);
        _audioAnalysisWindow.Show();
    }

    private async void LoopCheck(MsuSongInfoViewModel songInfo, MsuSongMsuPcmInfoViewModel? pcmInfoViewModel)
    {
        if (_audioService == null || _serviceProvider == null) return;

        await _audioService.StopSongAsync();

        pcmInfoViewModel ??= songInfo.MsuPcmInfo;

        if (pcmInfoViewModel.TrimEnd > 0 || pcmInfoViewModel.Loop > 0)
        {
            var result = await ShowYesNoWindow("Either the trim end or loop points have a value. Are you sure you want to overwrite them?");
            if (result != MessageWindowResult.Yes)
                return;
        }
        
        var window = _serviceProvider.GetRequiredService<MusicLooperWindow>();
        window.SetDetails(_projectViewModel!, songInfo);
        var loopResult = await window.ShowDialog();
        if (loopResult != null)
        {
            pcmInfoViewModel.Loop = loopResult.LoopStart;
            pcmInfoViewModel.TrimEnd = loopResult.LoopEnd;
        }
        
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _ = _audioService?.StopSongAsync();
        if (_audioAnalysisWindow?.IsVisible == true)
        {
            _audioAnalysisWindow.Close();
        }
    }

    private void OpenFolderMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_project?.MsuPath))
        {
            Process.Start("explorer.exe", $"/select,\"{_project!.MsuPath}\"");
        }
    }

    private void ExportButton_TrackList_OnClick(object? sender, RoutedEventArgs e)
    {
        WriteTrackList();
    }

    private void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenAddSongWindow();
    }

    private void OpenAddSongWindow(int? trackNumber = null)
    {
        if (_serviceProvider == null || App.MainWindow == null || _projectViewModel == null)
        {
            return;
        }
        
        var addSongWindow = _serviceProvider.GetRequiredService<AddSongWindow>();
        addSongWindow.TrackNumber = trackNumber;
        addSongWindow.ProjectModel = _projectViewModel;
        addSongWindow.ShowDialog(App.MainWindow);
    }
}