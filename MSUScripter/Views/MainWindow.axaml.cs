using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.Tools;
using MSUScripter.ViewModels;
using FileInputControlType = AvaloniaControls.FileInputControlType;

namespace MSUScripter.Views;

public partial class MainWindow : RestorableWindow
{
    private readonly MainWindowService? _service;
    private readonly MainWindowViewModel _model;
    
    public MainWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = _model = (MainWindowViewModel)new MainWindowViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<MainWindowService>();
            DataContext = _model = _service?.InitializeModel() ?? new MainWindowViewModel();
        }
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_model.InitProject != null)
        {
            _ = LoadProject(_model.InitProject);
        }

        _model.ActiveTabBackground = this.Find<Border>(nameof(SelectedTabBorder))?.Background ?? Brushes.Transparent;
        _model.NewProjectBackground = _model.ActiveTabBackground;

        if (_model.RecentProjects.Count == 0)
        {
            _model.NewProjectBackground = _model.ActiveTabBackground;
            _model.OpenProjectBackground = Brushes.Transparent;
            _model.DisplayNewProjectPage = true;
            _model.DisplayOpenProjectPage = false;
        }
        else
        {
            _model.NewProjectBackground = Brushes.Transparent;
            _model.OpenProjectBackground = _model.ActiveTabBackground;
            _model.DisplayNewProjectPage = false;
            _model.DisplayOpenProjectPage = true;
        }

        ValidateDependencies();
    }
    
    private void ValidateDependencies()
    {
        if (_service == null)
        {
            return;
        }
        
        Dispatcher.UIThread.Invoke(async () =>
        {
            await Task.Delay(100);
            if (await _service.ValidateDependencies() == false)
            {
                var dependencyWindow = new InstallDependenciesWindow();
                await dependencyWindow.ShowDialog(this);
            }
        });

    }
  
    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _service?.Shutdown();
    }

    protected override string RestoreFilePath => Path.Combine(Directories.BaseFolder, "Windows", "main-window.json");
    protected override int DefaultWidth => 1024;
    protected override int DefaultHeight => 768;

    public void RefreshRecentProjects()
    {
        _service?.RefreshRecentProjects();
    }
   
    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        _ = LoadProject();
    }

    private async Task LoadProject(string? path = null)
    {
        if (_service == null) return;
        
        var response = _service.LoadProject(path);

        if (!string.IsNullOrEmpty(response.error))
        {
            await MessageWindow.ShowErrorDialog(response.error, null, this);
            return;
        }
        
        OpenProject(response.mainProject!, response.backupProject);
    }

    private void NewProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveSettings();
        _model.NewProjectBackground = _model.ActiveTabBackground;
        _model.OpenProjectBackground = Brushes.Transparent;
        _model.SettingsBackground = Brushes.Transparent;
        _model.AboutBackground = Brushes.Transparent;
        _model.DisplayNewProjectPage = true;
        _model.DisplayOpenProjectPage = false;
        _model.DisplaySettingsPage = false;
        _model.DisplayAboutPage = false;
    }
    
    private void OpenProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveSettings();
        _model.NewProjectBackground = Brushes.Transparent;
        _model.OpenProjectBackground = _model.ActiveTabBackground;
        _model.SettingsBackground = Brushes.Transparent;
        _model.AboutBackground = Brushes.Transparent;
        _model.DisplayNewProjectPage = false;
        _model.DisplayOpenProjectPage = true;
        _model.DisplaySettingsPage = false;
        _model.DisplayAboutPage = false;
    }

    private void SettingsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _model.Settings.LoadSettings();
        _model.NewProjectBackground = Brushes.Transparent;
        _model.OpenProjectBackground = Brushes.Transparent;
        _model.SettingsBackground = _model.ActiveTabBackground;
        _model.AboutBackground = Brushes.Transparent;
        _model.DisplayNewProjectPage = false;
        _model.DisplayOpenProjectPage = false;
        _model.DisplaySettingsPage = true;
        _model.DisplayAboutPage = false;
    }

    private void AboutButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveSettings();
        _model.NewProjectBackground = Brushes.Transparent;
        _model.OpenProjectBackground = Brushes.Transparent;
        _model.SettingsBackground = Brushes.Transparent;
        _model.AboutBackground = _model.ActiveTabBackground;
        _model.DisplayNewProjectPage = false;
        _model.DisplayOpenProjectPage = false;
        _model.DisplaySettingsPage = false;
        _model.DisplayAboutPage = true;
    }

    private void CreateProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var project = _service?.CreateNewProject();
        if (project == null)
        {
            // show error
            return;
        }

        OpenProject(project, null);
    }

    private async void OpenProject(MsuProject project, MsuProject? backupProject)
    {
        try
        {
            if (_service == null)
            {
                return;
            }
            
            if (backupProject != null && await MessageWindow.ShowYesNoDialog(
                    "A backup with unsaved changes was detected. Would you like to load from the backup instead?",
                    "Load Backup?", this))
            {
                project = backupProject;
            }

            if (_service.IsLegacySmz3Project(project) && await MessageWindow.ShowYesNoDialog(
                    "This MSU is in the legacy format that is not supported by the mainline SMZ3. Do you want to update it to the modern SMZ3 format?",
                    "Update MSU Type?", this))
            {
                _service.UpdateLegacySmz3Project(project);
            }

            if (_service.ValidateProjectPaths(project) == false)
            {
                var window = new CopyProjectWindow();
                var updatedProject = await window.ShowDialog(this, project, false);
                if (updatedProject == null) return;
            }
        
            var msuProjectWindow = new MsuProjectWindow(project, this);
            msuProjectWindow.Show();
            msuProjectWindow.Closed += (sender, args) =>
            {
                _model.MsuProjectName = "";
                _model.MsuCreatorName = "";
                _model.SelectedMsuType = null;
                _model.MsuPath = "";
                _model.MsuProjectPath = "";
                _model.MsuPcmJsonPath = "";
                _model.MsuPcmWorkingPath = "";
                if (msuProjectWindow.CloseReason == MsuProjectWindowCloseReason.ExitApplication)
                {
                    Close();
                }
                else if (msuProjectWindow.CloseReason == MsuProjectWindowCloseReason.OpenProject)
                {
                    _ = LoadProject(msuProjectWindow.OpenProjectPath);
                }
                else if (msuProjectWindow.CloseReason == MsuProjectWindowCloseReason.NewProject)
                {
                    _model.NewProjectBackground = _model.ActiveTabBackground;
                    _model.OpenProjectBackground = Brushes.Transparent;
                    _model.SettingsBackground = Brushes.Transparent;
                    _model.AboutBackground = Brushes.Transparent;
                    _model.DisplayNewProjectPage = true;
                    _model.DisplayOpenProjectPage = false;
                    _model.DisplaySettingsPage = false;
                    _model.DisplayAboutPage = false;
                }
            
                GC.Collect();
                GC.WaitForPendingFinalizers();
            };
        }
        catch (Exception e)
        {
            _service?.LogError(e, "Error opening project");
            await MessageWindow.ShowErrorDialog(_model.Text.GenericError, _model.Text.GenericErrorTitle, this);
        }
    }

    private async void BrowseProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var path = await OpenMsuProjectFilePicker(false);
            if (string.IsNullOrEmpty(path)) return;
            await LoadProject(path);
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error opening project");
            await MessageWindow.ShowErrorDialog(_model.Text.GenericError, _model.Text.GenericErrorTitle, this);
        }
    }
    
    private async Task<string?> OpenMsuProjectFilePicker(bool isSave)
    {
        var folder = string.IsNullOrEmpty(_model.MsuPath)
            ? await this.GetDocumentsFolderPath()
            : Path.GetDirectoryName(_model.MsuPath);
        var path = await CrossPlatformTools.OpenFileDialogAsync(this, isSave ? FileInputControlType.SaveFile : FileInputControlType.OpenFile,
            "MSU Scripter Project File:*.msup", folder);
        return path?.Path.LocalPath;
    }

    private async void CloneProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var path = await OpenMsuProjectFilePicker(false);
            if (string.IsNullOrEmpty(path) || _service == null) return;
        
            var response = _service.LoadProject(path);
            if (!string.IsNullOrEmpty(response.error))
            {
                await MessageWindow.ShowErrorDialog(response.error, null, this);
                return;
            }
        
            var window = new CopyProjectWindow();
            var project = await window.ShowDialog(this, response.mainProject!, true);
            if (project != null)
            {
                OpenProject(project, null);
            }
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error cloning project");
            await MessageWindow.ShowErrorDialog(_model.Text.GenericError, _model.Text.GenericErrorTitle, this);
        }
    }
}