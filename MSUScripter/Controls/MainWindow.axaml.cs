using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using MSUScripter.Configs;

namespace MSUScripter.Controls;

public partial class MainWindow : Window
{
    private readonly IServiceProvider? _services;
    private NewProjectPanel? _newProjectPanel;
    private EditProjectPanel? _editProjectPanel;

    public MainWindow() : this(null)
    {
    }
    
    public MainWindow(IServiceProvider? services)
    {
        _services = services;
        InitializeComponent();
        DisplayNewPanel();
        var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);
        Title = $"MSU Scripter v{version.ProductVersion}";
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

    public async Task CheckPendingChanges()
    {
        if (_editProjectPanel == null) return;
        await _editProjectPanel.CheckPendingChanges();
    }
}