using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
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
    public partial class MainWindow
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ProjectService _projectService;
        private readonly SettingsService _settingsService;
        private readonly AudioService _audioService;
        private MsuProject? _msuProject;
        private NewPanel? _newPanel;
        private EditPanel? _editPanel;
        
        public MainWindow(ProjectService projectService, IServiceProvider serviceProvider, SettingsService settingsService, AudioService audioService)
        {
            _projectService = projectService;
            _serviceProvider = serviceProvider;
            _settingsService = settingsService;
            _audioService = audioService;
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
            _msuProject = null;
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
                    ? $"{new FileInfo(project.ProjectFilePath).Name} - MSU Scripter"
                    : $"{project.BasicInfo.PackName} - MSU Scripter";
            }
        }

        private void DisplayEditPanel(MsuProject project)
        {
            if (_newPanel?.OnProjectSelected != null)
            {
                _newPanel.OnProjectSelected -= OnProjectSelected;    
            }

            _msuProject = project;
            _newPanel = null;
            MainPanel.Children.Clear();
            _editPanel = _serviceProvider.GetRequiredService<EditPanel>();
            _editPanel.SetProject(project);
            MainPanel.Children.Add(_editPanel);
            UpdateTitle(project);
        }

        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => _audioService.StopSongAsync());
            CheckUnsavedChanges();
            DisplayNewPanel();
        }

        private void SaveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_editPanel == null) return;
            _msuProject = _editPanel.UpdateCurrentPageData();
            _projectService.SaveMsuProject(_msuProject);
            _editPanel?.UpdateStatusBarText("Project Saved");
            UpdateTitle(_msuProject);
        }

        private void SettingsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var settingsViewModel = new SettingsWindow();
            if (settingsViewModel.ShowDialog() != true) return;
            _settingsService.SaveSettings();
        }

        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            CheckUnsavedChanges();
        }

        private void CheckUnsavedChanges()
        {
            if (_editPanel == null || _msuProject == null) return;
            _editPanel.UpdateCurrentPageData();
            if (!_editPanel.HasChangesSince(_msuProject.LastSaveTime)) return;
            var result = MessageBox.Show("You have unsaved changes. Do you want to save?", "Unsaved Changes",
                MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _projectService.SaveMsuProject(_msuProject);
            }
        }
    }
}