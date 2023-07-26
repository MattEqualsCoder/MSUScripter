using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.UI.Tools;

namespace MSUScripter.UI;

public partial class EditPanel : UserControl
{
    private MsuProject _project = null!;
    private Dictionary<int, UserControl> _pages = new();
    private UserControl _currentPage = null!;
    private bool _enableMsuPcm = true;
    
    private readonly ProjectService? _projectService;
    private readonly MsuPcmService? _msuPcmService;

    public EditPanel() : this(null, null)
    {
    }

    public EditPanel(ProjectService? projectService, MsuPcmService? msuPcmService)
    {
        _projectService = projectService;
        _msuPcmService = msuPcmService;
        InitializeComponent();
    }

    public void SetProject(MsuProject project)
    {
        ToggleMsuPcm(project.BasicInfo.IsMsuPcmProject);
        _project = project;
        PopulatePageComboBox();
        var msuBasicInfoPanel = new MsuBasicInfoPanel(this);
        ConverterService.ConvertViewModel(_project.BasicInfo, msuBasicInfoPanel.MsuBasicInfo);
        _currentPage = msuBasicInfoPanel;
        _pages[0] = msuBasicInfoPanel;
        PagePanel.Children.Add(_currentPage);
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
        
        _currentPage.Visibility = Visibility.Collapsed;
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
        var pagePanel = new MsuTrackInfoPanel();
        pagePanel.SetTrackInfo(_project, track);
        _pages[page] = pagePanel;
        _currentPage = pagePanel;
        pagePanel.ToggleMsuPcm(_enableMsuPcm);
        PagePanel.Children.Add(_currentPage);
    }

    public MsuProject UpdateProjectData()
    {
        foreach (var page in _pages.Values)
        {
            if (page is MsuBasicInfoPanel basicInfoPanel)
            {
                basicInfoPanel.UpdateControlBindings();
                ConverterService.ConvertViewModel(basicInfoPanel.MsuBasicInfo, _project.BasicInfo);
            }
            else if (page is MsuTrackInfoPanel trackInfoPanel)
            {
                trackInfoPanel.UpdateData();
            }
        }

        return _project;
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
        _project = UpdateProjectData();
        _projectService.ExportMsuRandomizerYaml(_project);

        if (!_enableMsuPcm || _msuPcmService == null)
        {
            ShowExportComplete();
            return;
        }
        
        _msuPcmService.ExportMsuPcmTracksJson(_project);
        var msuPcmWindow = new MsuPcmGenerationWindow(_project,
            _project.Tracks.SelectMany(x => x.Songs).ToList());
        msuPcmWindow.ShowDialog();
        ShowExportComplete();
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
        _project = UpdateProjectData();
        _projectService.ExportMsuRandomizerYaml(_project);
        ShowExportComplete();
    }

    private void ExportButton_Json_OnClick(object sender, RoutedEventArgs e)
    {
        if (_msuPcmService == null) return;
        _project = UpdateProjectData();
        _msuPcmService.ExportMsuPcmTracksJson(_project);
        ShowExportComplete();
    }

    private void ExportButton_Msu_OnClick(object sender, RoutedEventArgs e)
    {
        if (_msuPcmService == null) return;
        _project = UpdateProjectData();
        _msuPcmService.ExportMsuPcmTracksJson(_project);
        var msuPcmWindow = new MsuPcmGenerationWindow(_project,
            _project.Tracks.SelectMany(x => x.Songs).ToList());
        msuPcmWindow.ShowDialog();
        ShowExportComplete();
    }

    private void ShowExportComplete()
    {
        Task.Run(() =>
        {
            Dispatcher.Invoke(() =>
            {
                ExportStatusTextBlock.Text = "Complete!";
            });
            ;
            Thread.Sleep(5000);

            Dispatcher.Invoke(() =>
            {
                ExportStatusTextBlock.Text = "";
            });
        });

    }
}