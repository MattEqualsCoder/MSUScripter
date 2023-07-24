using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.UI;

namespace MSUScripter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IServiceProvider _serviceProvider;
        private ProjectService _projectService;
        private MsuProject _msuProject = new();
        private NewPanel? _newPanel;
        private EditPanel? _editPanel;
        
        public MainWindow(ProjectService projectService, NewPanel newPanel, IServiceProvider serviceProvider)
        {
            _projectService = projectService;
            _serviceProvider = serviceProvider;
            InitializeComponent();
            DisplayNewPanel();
        }

        private void DisplayNewPanel()
        {
            if (_newPanel?.OnProjectSelected != null)
            {
                _newPanel.OnProjectSelected -= OnProjectSelected;    
            }

            _editPanel = null;
            MainPanel.Children.Clear();
            _newPanel = _serviceProvider.GetRequiredService<NewPanel>();
            MainPanel.Children.Add(_newPanel);
            _newPanel.OnProjectSelected += OnProjectSelected;
            UpdateTitle(null);
        }

        private void OnProjectSelected(object? sender, EventArgs e)
        {
            if (_newPanel?.Project == null) return;
            var project = _newPanel.Project;
            DisplayEditPanel(project);
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
                    ? $"MSU Scripter - {new FileInfo(project.ProjectFilePath).Name}"
                    : $"MSU Scripter - {project.BasicInfo.PackName}";
            }
        }

        private void DisplayEditPanel(MsuProject project)
        {
            if (_newPanel?.OnProjectSelected != null)
            {
                _newPanel.OnProjectSelected -= OnProjectSelected;    
            }

            _newPanel = null;
            MainPanel.Children.Clear();
            _editPanel = _serviceProvider.GetRequiredService<EditPanel>();
            _editPanel.SetProject(project);
            MainPanel.Children.Add(_editPanel);
            UpdateTitle(project);
        }

        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DisplayNewPanel();
        }

        private void SaveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_editPanel == null) return;
            var project = _editPanel.UpdateProjectData();
            _projectService.SaveMsuProject(project);
            UpdateTitle(project);
        }

        private void ExportYamlMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_editPanel == null) return;
            var project = _editPanel.UpdateProjectData();
            _projectService.ExportMsuRandomizerYaml(project);
        }

        private void ExportMsuPcmJsonMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_editPanel == null) return;
            var project = _editPanel.UpdateProjectData();
            _projectService.ExportMsuPcmTracksJson(project);
        }
    }
}