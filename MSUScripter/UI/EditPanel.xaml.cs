using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MSUScripter.Configs;
using MSUScripter.Services;

namespace MSUScripter.UI;

public partial class EditPanel : UserControl
{

    public static EditPanel? Instance;
    
    private MsuProject _project = null!;
    private Dictionary<int, UserControl> _pages = new();
    private UserControl _currentPage = null!;
    private bool _enableMsuPcm = true;
    
    private readonly ProjectService? _projectService;
    private readonly MsuPcmService? _msuPcmService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly AudioService? _audioService;

    public EditPanel() : this(null, null, null, null, null)
    {
    }

    public EditPanel(ProjectService? projectService, MsuPcmService? msuPcmService, IServiceProvider? serviceProvider, AudioControl? audioControl, AudioService? audioService)
    {
        _projectService = projectService;
        _msuPcmService = msuPcmService;
        _serviceProvider = serviceProvider;
        _audioService = audioService;
        InitializeComponent();
        if (audioControl != null)
        {
            AudioStackPanel.Children.Add(audioControl);    
        }

        Instance = this;
    }
    
    public void SetProject(MsuProject project)
    {
        ToggleMsuPcm(project.BasicInfo.IsMsuPcmProject);
        _project = project;
        PopulatePageComboBox();
        var msuBasicInfoPanel = new MsuBasicInfoPanel(this, _project);
        _currentPage = msuBasicInfoPanel;
        _pages[0] = msuBasicInfoPanel;
        PagePanel.Children.Add(_currentPage);

        if (project.LastSaveTime == DateTime.MinValue)
        {
            UpdateStatusBarText("Project Created");
        }
        else
        {
            UpdateStatusBarText("Project Loaded");
        }
    }

    public void ToggleMsuPcm(bool enable)
    {
        _enableMsuPcm = enable;
        ExportMenuButton.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
    }

    public void DisplayPage(int page)
    {
        if (page < 0 || page >= PageComboBox.Items.Count || _pages.Count() == 0)
            return;

        if (_serviceProvider == null)
            throw new InvalidOperationException("Unable to dislay track page");
        
        _currentPage.Visibility = Visibility.Collapsed;

        UpdateCurrentPageData();
        
        if (_pages.TryGetValue(page, out var previousPage))
        {
            previousPage.Visibility = Visibility.Visible;
            _currentPage = previousPage;
            if (page > 0 && _currentPage is MsuTrackInfoPanel trackPage)
            {
                trackPage.ToggleMsuPcm(_enableMsuPcm);
            }
            return;
        }

        var track = _project.Tracks.OrderBy(x => x.TrackNumber).ToList()[page-1];
        var pagePanel = _serviceProvider.GetRequiredService<MsuTrackInfoPanel>();
        pagePanel.SetTrackInfo(_project, track);
        _pages[page] = pagePanel;
        _currentPage = pagePanel;
        pagePanel.ToggleMsuPcm(_enableMsuPcm);
        PagePanel.Children.Add(_currentPage);
    }

    public MsuProject UpdateCurrentPageData()
    {
        if (_currentPage is MsuBasicInfoPanel basicInfoPanel)
        {
            basicInfoPanel.UpdateData();
        }
        else if (_currentPage is MsuTrackInfoPanel trackInfoPanel)
        {
            trackInfoPanel.UpdateData();
        }
        return _project;
    }
    
    public bool HasChangesSince(DateTime time)
    {
        foreach (var page in _pages.Values)
        {
            if (page is MsuBasicInfoPanel basicPanel)
            {
                if (basicPanel.HasChangesSince(time))
                {
                    return true;
                }
            }
            else if (page is MsuTrackInfoPanel trackPanel)
            {
                if (trackPanel.HasChangesSince(time))
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    private void PopulatePageComboBox()
    {
        int currentPage = PageComboBox.SelectedIndex;
            
        var pages = new List<string>() { "MSU Details" };
            
        foreach (var track in  _project.Tracks.OrderBy(x => x.TrackNumber))
        {
            pages.Add($"Track #{track.TrackNumber} - {track.TrackName}");
        }

        PageComboBox.ItemsSource = pages;
        PageComboBox.SelectedIndex = Math.Clamp(currentPage, 0, pages.Count - 1);
    }

    private void PageComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DisplayPage(PageComboBox.SelectedIndex);
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        var newIndex = PageComboBox.SelectedIndex + 1;
        if (newIndex < PageComboBox.Items.Count)
            PageComboBox.SelectedIndex = newIndex;
    }

    private void PrevButton_OnClick(object sender, RoutedEventArgs e)
    {
        var newIndex = PageComboBox.SelectedIndex - 1;
        if (newIndex >= 0)
            PageComboBox.SelectedIndex = newIndex;
    }

    private void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_projectService == null) return;
        _project = UpdateCurrentPageData();
        _projectService.ExportMsuRandomizerYaml(_project);

        if (!_enableMsuPcm || _msuPcmService == null)
        {
            UpdateStatusBarText("Export Complete");
            return;
        }
        
        _msuPcmService.ExportMsuPcmTracksJson(_project);
        Task.Run(DisplayMsuGenerationWindow);
    }

    private void ExportMenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ExportMenuButton.ContextMenu == null) return;
        ExportMenuButton.ContextMenu.DataContext = ExportMenuButton.DataContext;
        ExportMenuButton.ContextMenu.IsOpen = true;
    }

    private void ExportButton_Yaml_OnClick(object sender, RoutedEventArgs e)
    {
        if (_projectService == null) return;
        _project = UpdateCurrentPageData();
        _projectService.ExportMsuRandomizerYaml(_project);
        UpdateStatusBarText("YAML File Written");
    }

    private void ExportButton_Json_OnClick(object sender, RoutedEventArgs e)
    {
        if (_msuPcmService == null) return;
        _project = UpdateCurrentPageData();
        _msuPcmService.ExportMsuPcmTracksJson(_project);
        UpdateStatusBarText("Json File Written");
    }

    private void ExportButton_Msu_OnClick(object sender, RoutedEventArgs e)
    {
        if (_msuPcmService == null) return;
        _project = UpdateCurrentPageData();
        _msuPcmService.ExportMsuPcmTracksJson(_project);
        Task.Run(DisplayMsuGenerationWindow);
    }

    private async Task DisplayMsuGenerationWindow()
    {
        if (MsuPcmService.Instance.IsGeneratingPcm) return;
        
        if (_audioService != null)
        {
            UpdateStatusBarText("Stopping Song");
            await _audioService.StopSongAsync(null, true);
            UpdateStatusBarText("Stopped Song");
        }

        Dispatcher.Invoke(() =>
        {
            var msuPcmWindow = new MsuPcmGenerationWindow(_project,
                _project.Tracks.SelectMany(x => x.Songs).ToList());
            msuPcmWindow.ShowDialog();
            UpdateStatusBarText("MSU Generated");
        });
        
    }

    public void UpdateStatusBarText(string message)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateStatusBarText(message));
            return;
        }
        
        StatusMessage.Text = message;
    }

    private void EditPanel_OnUnloaded(object sender, RoutedEventArgs e)
    {
        Instance = null;
    }
}