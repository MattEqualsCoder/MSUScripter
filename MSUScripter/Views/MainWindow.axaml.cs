using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaControls.Controls;
using AvaloniaControls.Models;
using GitHubReleaseChecker;
using Microsoft.Extensions.DependencyInjection;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.Tools;

namespace MSUScripter.Views;

public partial class MainWindow : RestorableWindow
{
    private readonly IServiceProvider? _services;
    private NewProjectPanel? _newProjectPanel;
    private EditProjectPanel? _editProjectPanel;
    private Settings? _settings;
    private SettingsService? _settingsService;
    private MsuPcmService? _msuPcmService;
    private PyMusicLooperService? _pyMusicLooperService;
    private ProjectService? _projectService;

    public MainWindow() : this(null, null, null, null, null, null)
    {
    }
    
    public MainWindow(IServiceProvider? services, Settings? settings, SettingsService? settingsService, MsuPcmService? msuPcmService, PyMusicLooperService? pyMusicLooperService, ProjectService? projectService)
    {
        _services = services;
        _settings = settings;
        _settingsService = settingsService;
        _msuPcmService = msuPcmService;
        _pyMusicLooperService = pyMusicLooperService;
        _projectService = projectService;
        InitializeComponent();
        DisplayNewPanel();
        Title = $"MSU Scripter v{App.Version}";

        Task.Run(() =>
        {
            _msuPcmService?.DeleteTempPcms();
            _msuPcmService?.DeleteTempJsonFiles();
            _msuPcmService?.ClearCache();
            _pyMusicLooperService?.ClearCache();
        });
        
        try
        {
            HotKeyManager.SetHotKey(this.Find<MenuItem>(nameof(SaveMenuItem))!, new KeyGesture(Key.S, KeyModifiers.Control));
        }
        catch
        {
            // Do nothing
        }
    }

    public void SaveChanges()
    {
        _editProjectPanel?.SaveProject();
    }
    
    private void DisplayNewPanel()
    {
        if (_services == null) return;
        
        if (_newProjectPanel?.OnProjectSelected != null)
        {
            _newProjectPanel.OnProjectSelected -= OnProjectSelected;    
        }

        _editProjectPanel = null;
        MainPanel.Children.Clear();
        _newProjectPanel = _services.GetRequiredService<NewProjectPanel>();
        MainPanel.Children.Add(_newProjectPanel);
        _newProjectPanel.OnProjectSelected += OnProjectSelected;
        this.Find<MenuItem>(nameof(MsuDetailsMenuItem))!.IsVisible = false;
        this.Find<MenuItem>(nameof(TrackOverviewMenuItem))!.IsVisible = false;
        UpdateTitle(null);
    }
    
    private void OnProjectSelected(object? sender, EventArgs e)
    {
        if (_newProjectPanel?.Project == null) return;
        var project = _newProjectPanel.Project;
        DisplayEditPanel(project);
    }

    private void DisplayEditPanel(MsuProject project)
    {
        if (_services == null) return;
        
        if (_newProjectPanel?.OnProjectSelected != null)
        {
            _newProjectPanel.OnProjectSelected -= OnProjectSelected;    
        }

        _newProjectPanel = null;
        MainPanel.Children.Clear();
        _editProjectPanel = _services.GetRequiredService<EditProjectPanel>();
        _editProjectPanel.SetProject(project);
        MainPanel.Children.Add(_editProjectPanel);
        this.Find<MenuItem>(nameof(MsuDetailsMenuItem))!.IsVisible = true;
        this.Find<MenuItem>(nameof(TrackOverviewMenuItem))!.IsVisible = true;
        UpdateTitle(project);
    }
    
    private void UpdateTitle(MsuProject? project)
    {
        if (project == null)
        {
            Title = "MSU Scripter";
        }
        else
        {
            Title = string.IsNullOrEmpty(project.BasicInfo.PackName)
                ? $"{new FileInfo(project.ProjectFilePath).Name} - MSU Scripter"
                : $"{project.BasicInfo.PackName} - MSU Scripter";
        }
    }

    private async void NewMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_editProjectPanel != null)
        {
            await _editProjectPanel.CheckPendingChanges();    
        }
        DisplayNewPanel();
    }

    private void SaveMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_editProjectPanel == null) return;
        _editProjectPanel.SaveProject();
    }

    private void SettingsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_services == null) return;
        var settingsWindow = _services.GetRequiredService<SettingsWindow>();
        settingsWindow.ShowDialog(this);
    }

    public bool CheckPendingChanges()
    {
        if (_editProjectPanel == null) return false;
        return _editProjectPanel.HasPendingChanges();
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_services == null || _settings?.PromptOnUpdate != true) return;
        
        if (!string.IsNullOrEmpty(Program.StartingProject) && _projectService != null)
        {
            var project = _projectService!.LoadMsuProject(Program.StartingProject, false);
            
            if (!string.IsNullOrEmpty(project!.BackupFilePath))
            {
                var backupProject = _projectService!.LoadMsuProject(project!.BackupFilePath, true);
                if (backupProject != null && backupProject.LastSaveTime > project.LastSaveTime)
                {
                    var messageWindow = new MessageWindow(new MessageWindowRequest
                    {
                        Message = "A backup with unsaved changes was detected. Would you like to load from the backup instead?",
                        Icon = MessageWindowIcon.Question,
                        Title = "Load Backup?",
                        Buttons = MessageWindowButtons.YesNo
                    });
                    messageWindow.ShowDialog();
                    if (messageWindow.DialogResult?.PressedAcceptButton == true)
                        project = backupProject;
                }
            }

            DisplayEditPanel(project);
        }
        
        var newerGitHubRelease = await _services.GetRequiredService<IGitHubReleaseCheckerService>()
            .GetGitHubReleaseToUpdateToAsync("MattEqualsCoder", "MSUScripter", App.Version, _settings?.PromptOnPreRelease == true);

        if (newerGitHubRelease != null)
        {
            var window = new MessageWindow(new MessageWindowRequest
            {
                Message = "A new update was found for the MSU Scripter is now available.",
                Buttons = MessageWindowButtons.OK,
                LinkText = "Go to GitHub Release Page",
                LinkUrl = newerGitHubRelease.Url
            });

            _ = window.ShowDialog(this);
        }
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_settings == null || _settingsService == null)
        {
            return;
        }
        
        _settingsService.SaveSettings();

        _msuPcmService?.DeleteTempPcms();
        _msuPcmService?.DeleteTempJsonFiles();
    }

    private void MsuDetailsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _editProjectPanel?.DisplayMsuDetails();
    }

    private void TrackOverviewMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _editProjectPanel?.DisplayTrackOverview();
    }

    protected override string RestoreFilePath => Path.Combine(Directories.BaseFolder, "Windows", "main-window.json");
    protected override int DefaultWidth => 1024;
    protected override int DefaultHeight => 768;
}