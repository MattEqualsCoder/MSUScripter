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
    private bool _displaySearchBar;
    private int _previousPage = -1;
    private bool _isAddNewSongWindowOpen = false;
    
    public EditProjectPanel() : this(null, null, null, null, null, null, null, null)
    {
        
    }
    
    public EditProjectPanel(ProjectService? projectService, MsuPcmService? msuPcmService, IAudioPlayerService? audioService, IServiceProvider? serviceProvider, ConverterService? converterService, AudioControl? audioControl, TrackListService? trackListService, StatusBarService? statusBarService)
    {
        _projectService = projectService;
        _msuPcmService = msuPcmService;
        _audioService = audioService;
        _serviceProvider = serviceProvider;
        _converterService = converterService;
        _audioControl = audioControl;
        _trackListService = trackListService;
        InitializeComponent();

        if (statusBarService != null)
        {
            statusBarService.StatusBarTextUpdated += (_, args) =>
            {
                UpdateStatusBarText(args.Data);
            };
        }
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
            var trackOverviewPanel = new TrackOverviewPanel(_projectViewModel);
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

    public void UpdateStatusBarText(string message)
    {
        if (!CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => UpdateStatusBarText(message));
            return;
        }
        
        this.Find<TextBlock>(nameof(StatusMessage))!.Text = message;
    }
    
    public void SaveProject()
    {
        if (_projectViewModel == null || _projectService == null) return;
        _project = _converterService!.ConvertProject(_projectViewModel);
        _projectService.SaveMsuProject(_project, false);
        _projectViewModel.LastSaveTime = _project.LastSaveTime;
        _lastAutoSave = _project.LastSaveTime;
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
    }

    private void ExportYaml(MsuProject project)
    {
        if (!project.BasicInfo.WriteYamlFile) return;
        _projectService!.ExportMsuRandomizerYaml(project, out var error);
        if (!string.IsNullOrEmpty(error))
        {
            ShowError(error);
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
            await _audioService.StopSongAsync(null, true);
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            var msuPcmGenerationWindow = new MsuPcmGenerationWindow(_projectViewModel, exportYaml);
            msuPcmGenerationWindow.ShowDialog(App.MainWindow!);
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
        if (_audioAnalysisWindow != null) return;
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (_serviceProvider == null || _projectViewModel == null || topLevel == null) return;
        _audioAnalysisWindow = new AudioAnalysisWindow(_projectViewModel);
        _audioAnalysisWindow.Closed += (_, _) => _audioAnalysisWindow = null; 
        _audioAnalysisWindow.Show();
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
        var addSongWindow = new AddSongWindow(_projectViewModel, trackNumber);
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
    }

    private void ExportButton_Video_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null || _projectViewModel == null)
        {
            return;
        }

        this.Find<ComboBox>(nameof(PageComboBox))!.Focus();
        var window = new VideoCreatorWindow(_projectViewModel);
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