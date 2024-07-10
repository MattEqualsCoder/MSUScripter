using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaControls.Controls;
using AvaloniaControls.Models;
using Microsoft.Extensions.DependencyInjection;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.ViewModels;
using Timer = System.Timers.Timer;

namespace MSUScripter.Views;

public partial class EditProjectPanel : UserControl
{
    private const string MsuDetailsTitle = "MSU Details";
    private const string TrackOverviewTitle = "Track Overview";
    private readonly ProjectService? _projectService;
    private readonly MsuPcmService? _msuPcmService;
    private readonly IAudioPlayerService? _audioService;
    private readonly AudioMetadataService? _audioMetadataService;
    private readonly ConverterService? _converterService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly AudioControl? _audioControl;
    private readonly TrackListService? _trackListService;
    private readonly VideoCreatorService? _videoCreatorService;
    private readonly AudioAnalysisService? _audioAnalysisService;
    private readonly Timer _backupTimer = new Timer(TimeSpan.FromSeconds(60));
    private MsuProject? _project;
    private MsuProjectViewModel? _projectViewModel; 
    private UserControl? _currentPage;
    private bool _hasCheckedPendingChanges;
    private DateTime? _lastAutoSave;
    private bool _displaySearchBar;
    private int _previousPage = -1;
    private bool _isAddNewSongWindowOpen = false;
    
    public EditProjectPanel() : this(null, null, null, null, null, null, null, null, null, null, null)
    {
        
    }
    
    public EditProjectPanel(IMsuTypeService? msuTypeService, ProjectService? projectService, MsuPcmService? msuPcmService, IAudioPlayerService? audioService, IServiceProvider? serviceProvider, AudioMetadataService? audioMetadataService, ConverterService? converterService, AudioControl? audioControl, TrackListService? trackListService, VideoCreatorService? videoCreatorService, AudioAnalysisService? audioAnalysisService)
    {
        _projectService = projectService;
        _msuPcmService = msuPcmService;
        _audioService = audioService;
        _serviceProvider = serviceProvider;
        _audioMetadataService = audioMetadataService;
        _converterService = converterService;
        _audioControl = audioControl;
        _trackListService = trackListService;
        _videoCreatorService = videoCreatorService;
        _audioAnalysisService = audioAnalysisService;
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

    public void DisplayMsuDetails()
    {
        this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.Text = MsuDetailsTitle;
        this.Find<ComboBox>(nameof(PageComboBox))!.SelectedIndex = 0;
    }

    public void DisplayTrackOverview()
    {
        this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.Text = TrackOverviewTitle;
        this.Find<ComboBox>(nameof(PageComboBox))!.SelectedIndex = 1;
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
            
        var pages = new List<string>() { MsuDetailsTitle, TrackOverviewTitle };
            
        foreach (var track in  _project.Tracks.OrderBy(x => x.TrackNumber))
        {
            pages.Add($"Track #{track.TrackNumber} - {track.TrackName}");
        }

        comboBox.ItemsSource = pages;
        comboBox.SelectedIndex = Math.Clamp(currentPage, 0, pages.Count - 1);

        this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.ItemsSource = pages;
        
        try
        {
            HotKeyManager.SetHotKey(this.Find<Button>(nameof(SearchButton))!, new KeyGesture(Key.F, KeyModifiers.Control));
        }
        catch
        {
            // Do nothing
        }
    }

    private void PageComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        DisplayPage(this.Find<ComboBox>(nameof(PageComboBox))!.SelectedIndex);
    }
    
    private void DisplayPage(int page)
    {
        var comboBox = this.Find<ComboBox>(nameof(PageComboBox))!;
        
        if (page < 0 || page >= comboBox.Items.Count || _project == null || _previousPage == page)
            return;

        if (_serviceProvider == null)
            throw new InvalidOperationException("Unable to display track page");

        comboBox.SelectedIndex = page;

        if (_currentPage is MsuTrackInfoPanel prevPage)
        {
            prevPage.PcmOptionSelected -= PagePanelOnPcmOptionSelected;
            prevPage.MetaDataFileSelected -= SongFileSelected;
            prevPage.FileUpdated -= SongFileSelected; 
        }
        
        var parentPagePanel = this.Find<Panel>(nameof(PagePanel))!;
        var parentPageDockPanel = this.Find<Panel>(nameof(PageDockPanel))!;
        var scrollViewer = this.Find<ScrollViewer>(nameof(ScrollViewer))!;
        var scrollViewerBorder = this.Find<Border>(nameof(ScrollViewerBorder))!;
            
        parentPagePanel.Children.Clear();
        parentPageDockPanel.Children.Clear();
        
        if (page == 0)
        {
            var msuBasicInfoPanel = new MsuBasicInfoPanel(_projectViewModel!.BasicInfo);
            _currentPage = msuBasicInfoPanel;
            parentPagePanel.Children.Add(_currentPage);
            scrollViewerBorder.IsVisible = true;
            parentPageDockPanel.IsVisible = false;
        }
        else if (page == 1)
        {
            var trackOverviewPanel = new TrackOverviewPanel(_projectViewModel!.Tracks);
            _currentPage = trackOverviewPanel;
            parentPageDockPanel.Children.Add(_currentPage);
            scrollViewerBorder.IsVisible = false;
            parentPageDockPanel.IsVisible = true;
            trackOverviewPanel.OnSelectedTrack += TrackOverviewPanelOnOnSelectedTrack;
        }
        else
        {
            var track = _projectViewModel!.Tracks.OrderBy(x => x.TrackNumber).ToList()[page-2];
            var pagePanel = _serviceProvider.GetRequiredService<MsuTrackInfoPanel>();
            pagePanel.SetTrackInfo(_projectViewModel, track);
            pagePanel.PcmOptionSelected += PagePanelOnPcmOptionSelected;
            pagePanel.MetaDataFileSelected += SongFileSelected;
            pagePanel.FileUpdated += SongFileSelected;
            pagePanel.AddSongWindowButtonPressed += PagePanelOnAddSongWindowButtonPressed;
            _currentPage = pagePanel;
            parentPagePanel.Children.Add(_currentPage);
            scrollViewerBorder.IsVisible = true;
            parentPageDockPanel.IsVisible = false;
        }

        _previousPage = page;
    }

    private void TrackOverviewPanelOnOnSelectedTrack(object? sender, TrackEventArgs e)
    {
        var track = _projectViewModel!.Tracks.First(x => x.TrackNumber == e.TrackNumber);
        var newIndex = _projectViewModel.Tracks.OrderBy(x => x.TrackNumber).ToList().IndexOf(track) + 2;
        var comboBox = this.Find<ComboBox>(nameof(PageComboBox))!;
        if (newIndex > 0 && newIndex < comboBox.Items.Count)
        {
            comboBox.SelectedIndex = newIndex;
            this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.Text = comboBox.SelectedItem as string;
        }
    }

    private void PagePanelOnAddSongWindowButtonPressed(object? sender, TrackEventArgs e)
    {
        _ = OpenAddSongWindow(e.TrackNumber);
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
        else if (e is { Type: PcmEventType.StartingSamples, PcmInfo: not null } && OperatingSystem.IsWindows())
        {
            GetStartingSamples(e.PcmInfo);
        }
        else if (e.Type == PcmEventType.AddedSubChannelOrSubTrack)
        {
            e.Song.MsuPcmInfo.UpdateSubTrackSubChannelWarning();
            e.Song.MsuPcmInfo.UpdateMultiWarning();
        }
    }

    public void GetStartingSamples(MsuSongMsuPcmInfoViewModel pcmInfoViewModel)
    {
        if (_audioAnalysisService == null || string.IsNullOrEmpty(pcmInfoViewModel.File) ||
            !File.Exists(pcmInfoViewModel.File))
        {
            return;
        }

        try
        {
            var samples = _audioAnalysisService.GetAudioStartingSample(pcmInfoViewModel.File);
            pcmInfoViewModel.TrimStart = samples;
            UpdateStatusBarText("Starting samples retrieved");
        }
        catch
        {
            ShowError("Unable to get starting samples for file");
        }
    }
    
    public async Task PlaySong(MsuSongInfoViewModel songModel, bool fromEnd)
    {
        if (_audioService == null || _projectViewModel == null)
            return;

        // Stop the song if it is currently playing
        await StopSong();
        
        // Regenerate the pcm file if it has updates that have been made to it
        if (!GeneratePcmFile(songModel, false, false))
            return;
        
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
        var tempProject = _converterService!.ConvertProject(_projectViewModel!);

        if (asPrimary)
        {
            var msu = new FileInfo(_project.MsuPath);
            var path = msu.FullName.Replace(msu.Extension, $"-{song.TrackNumber}.pcm");
            song.OutputPath = path;
        }
        
        if (!_msuPcmService.CreatePcm(tempProject, song, out var message, out var generated, false))
        {
            if (generated)
            {
                UpdateStatusBarText("PCM Generated with Warning");

                if (!_projectViewModel!.IgnoreWarnings.Contains(song.OutputPath))
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        var window = new MessageWindow(new MessageWindowRequest
                        {
                            Message = message ?? "Unknown error with msupcm++",
                            Buttons = MessageWindowButtons.YesNo,
                            Icon = MessageWindowIcon.Warning,
                            CheckBoxText = "Ignore future warnings for this song"
                        });

                        window.Closed += (sender, args) =>
                        {
                            if (window.DialogResult?.CheckedBox == true)
                            {
                                _projectViewModel!.IgnoreWarnings.Add(song.OutputPath);
                            }
                        };
                        
                        _ = window.ShowDialog(App.MainWindow!);
                    });
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

        var hasAlts = tempProject.Tracks.First(x => x.TrackNumber == songModel.TrackNumber).Songs.Count > 1;
        
        UpdateStatusBarText(hasAlts ? "PCM Generated - YAML Regeneration Needed" : "PCM Generated");
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

        songModel.MsuPcmInfo.UpdateHertzWarning(_audioAnalysisService?.GetAudioSampleRate(file));
        songModel.MsuPcmInfo.UpdateMultiWarning();
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
        if (result)
        {
            SaveProject();
        }
    }

    private void NextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var comboBox = this.Find<ComboBox>(nameof(PageComboBox))!;
        var newIndex = comboBox.SelectedIndex + 1;
        if (newIndex < comboBox.Items.Count)
        {
            comboBox.SelectedIndex = newIndex;
            this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.Text = comboBox.SelectedItem as string;
        }
    }

    private void PrevButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var comboBox = this.Find<ComboBox>(nameof(PageComboBox))!;
        var newIndex = comboBox.SelectedIndex - 1;
        if (newIndex >= 0)
        {
            comboBox.SelectedIndex = newIndex;
            this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.Text = comboBox.SelectedItem as string;
        }
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

        _ = new MessageWindow(new MessageWindowRequest
        {
            Message = message,
            Icon = MessageWindowIcon.Error,
            Title = title
        }).ShowDialog(App.MainWindow!);
    }
    
    private async Task<bool> ShowYesNoWindow(string message, string title = "MSU Scripter")
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            var result = false;
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                result = await ShowYesNoWindow(message, title);
            });
            return result;
        }

        var window = new MessageWindow(new MessageWindowRequest
        {
            Message = message,
            Title = title,
            Buttons = MessageWindowButtons.YesNo,
            Icon = MessageWindowIcon.Question
        });
        await window.ShowDialog(App.MainWindow!);
        return window.DialogResult?.PressedAcceptButton == true;
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

        if (!_projectService.CreateAltSwapperFile(_project, extraProjects))
        {
            ShowError("Could not create alt swapper bat file. Project file may be corrupt. Verify output pcm file paths.", "Alt Swapper Error");
        }
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

        if (_projectService.CreateSmz3SplitScript(_project, conversions))
        {
            UpdateStatusBarText("SMZ3 Split Script Created");
        }
        else
        {
            UpdateStatusBarText("Insufficient tracks");
        }
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
            if (!_projectService.CreateAltSwapperFile(_project, extraProjects))
            {
                ShowError("Could not create alt swapper bat file. Project file may be corrupt. Verify output pcm file paths.", "Alt Swapper Error");
                return;
            }
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
            var result = await ShowYesNoWindow("Either the trim end or loop point have a value. Are you sure you want to overwrite them?");
            if (!result)
                return;
        }
        
        var window = _serviceProvider.GetRequiredService<PyMusicLooperWindow>();
        window.SetDetails(_projectViewModel!, songInfo, pcmInfoViewModel);
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
            if (!Directories.OpenDirectory(_project.MsuPath, true))
            {
                ShowError("Could not open MSU directory");
            }
        }
    }

    private void ExportButton_TrackList_OnClick(object? sender, RoutedEventArgs e)
    {
        WriteTrackList();
    }

    private void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = OpenAddSongWindow();
    }

    private async Task OpenAddSongWindow(int? trackNumber = null)
    {
        if (_serviceProvider == null || App.MainWindow == null || _projectViewModel == null)
        {
            return;
        }
        
        _isAddNewSongWindowOpen = true;
        var addSongWindow = _serviceProvider.GetRequiredService<AddSongWindow>();
        addSongWindow.TrackNumber = trackNumber;
        addSongWindow.ProjectModel = _projectViewModel;
        await addSongWindow.ShowDialog(App.MainWindow);
        _isAddNewSongWindowOpen = false;
    }

    private void SearchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isAddNewSongWindowOpen)
        {
            return;
        }

        ToggleSearchBar(!_displaySearchBar);    
    }

    private void ToggleSearchBar(bool enabled)
    {
        _displaySearchBar = enabled;
        this.Find<ComboBox>(nameof(PageComboBox))!.IsVisible = !_displaySearchBar;
        this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.IsVisible = _displaySearchBar;
        if (_displaySearchBar)
        {
            this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.Text = "";
            
            // Delay because setting focus the first time doesn't work for some reason
            Task.Run(() =>
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Invoke(() =>
                {
                    this.Find<AutoCompleteBox>(nameof(TrackSearchAutoCompleteBox))!.Focus();
                });
            });
        }
        else
        {
            Task.Run(() =>
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Invoke(() =>
                {
                    this.Find<ComboBox>(nameof(PageComboBox))!.Focus();
                });
            });
        }
    }

    private void ExportButton_ValidatedYaml_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_projectService == null || _projectViewModel == null)
        {
            return;
        }

        if (!_projectService.ValidateProject(_projectViewModel, out var message))
        {
            ShowError(message);
        }
        else
        {
            UpdateStatusBarText("YAML file validated successfully");
        }
    }

    private void ExportButton_Video_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null || _projectViewModel == null)
        {
            return;
        }

        this.Find<ComboBox>(nameof(PageComboBox))!.Focus();
        var window = _serviceProvider.GetRequiredService<VideoCreatorWindow>();
        window.Project = _projectViewModel;
        window.ShowDialog(App.MainWindow!);
    }

    private void TrackSearchAutoCompleteBox_OnPopulated(object? sender, PopulatedEventArgs e)
    {
        var items = e.Data.Cast<string>().ToList();
        if (items.Count != 1 || string.IsNullOrEmpty(items[0]))
        {
            return;
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
        if (selectedItem == MsuDetailsTitle)
        {
            DisplayPage(0);
            return;
        }
        
        if (selectedItem == TrackOverviewTitle)
        {
            DisplayPage(1);
            return;
        }

        var track = _projectViewModel!.Tracks.FirstOrDefault(x =>
            $"Track #{x.TrackNumber} - {x.TrackName}" == selectedItem);

        if (track != null)
        {
            DisplayPage(_projectViewModel.Tracks.OrderBy(x => x.TrackNumber).ToList().IndexOf(track) + 2);    
        }
    }

    private void ExportButton_Package_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = PackageProject();
    }

    private async Task PackageProject()
    {
        if (_projectViewModel == null)
        {
            return;
        }

        if (_projectViewModel.BasicInfo.IsMsuPcmProject && _projectViewModel.Tracks.SelectMany(x => x.Songs).Any(x => x.HasChangesSince(x.LastGeneratedDate)))
        {
            var result = await ShowYesNoWindow("One or more PCM file is out of date. It is recommended to export the MSU first before packaging. Do you want to continue?");
            if (!result)
            {
                return;
            }
        }

        this.Find<ComboBox>(nameof(PageComboBox))!.Focus();
        var packageWindow = new PackageMsuWindow(_projectViewModel!);
        await packageWindow.ShowDialog(App.MainWindow!);
    }

    private void TrackSearchAutoCompleteBox_OnDropDownClosed(object? sender, EventArgs e)
    {
        ToggleSearchBar(false);
    }
}