using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Configs;
using MSUScripter.Events;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.Tools;
using MSUScripter.ViewModels;
using FileInputControlType = AvaloniaControls.FileInputControlType;

namespace MSUScripter.Views;

public partial class MainWindow : RestorableWindow
{
    private readonly MainWindowService? _service;
    private NewProjectPanel? _newProjectPanel;
    private EditProjectPanel? _editProjectPanel;
    private bool _forceClose;
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

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_model.InitProjectError)
        {
            await MessageWindow.ShowErrorDialog("There was an error in loading the requested project");
        }
        else if (_model.InitProject != null)
        {
            await GetNewProjectPanel().LoadProject(_model.InitProject, _model.InitBackupProject);
        }

        _model.ActiveTabBackground = this.Find<Border>(nameof(SelectedTabBorder))?.Background ?? Brushes.Transparent;
        _model.NewProjectBackground = _model.ActiveTabBackground;

        if (_model.HasDoneFirstTimeSetup) return;
        await SetupMsuPcm();
    }

    private async Task SetupMsuPcm()
    {
        if (_service == null) return;
        
        var result = await MessageWindow.ShowYesNoDialog(
            "If you want to use msupcm++, you'll need to point the MSU Scripter to its location. Would you like to set the msupcm++ path now? You can always set it later in the settings if needed.",
            "Setup msupcm++", this);
        if (!result)
        {
            _service.UpdateHasDoneFirstTimeSetup(null);
        }

        var documentsFolderPath = await this.GetDocumentsFolderPath();
        var filter = OperatingSystem.IsWindows()
            ? "msupcm Executable:msupcm.exe;All Files:*.*"
            : "msupcm Executable:msupcm;All Files:*";
        var msuPcmPath = await CrossPlatformTools.OpenFileDialogAsync(this, FileInputControlType.OpenFile, filter, documentsFolderPath);

        if (msuPcmPath == null)
        {
            _service.UpdateHasDoneFirstTimeSetup(null);
            return;
        }

        _service.UpdateHasDoneFirstTimeSetup(msuPcmPath.Path.LocalPath);
        
        if (!_service.ValidateMsuPcm(msuPcmPath.Path.LocalPath))
        {
            await MessageWindow.ShowErrorDialog(
                "msupcm++ failed to run successfully. Make sure you can run msupcm -v in the commandline.",
                "msupcm++ Error", this);
        }
        else
        {
            await MessageWindow.ShowInfoDialog("msupcm++ setup successful.", "Success", this);
        }
    }

    private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _service?.Shutdown();
        
        if (_forceClose || _service?.IsEditPanelDisplayed != true || !GetEditProjectPanel().HasPendingChanges) return;
        
        e.Cancel = true;
        await GetEditProjectPanel().DisplayPendingChangesWindow();
        _forceClose = true;
        Close();
    }

    protected override string RestoreFilePath => Path.Combine(Directories.BaseFolder, "Windows", "main-window.json");
    protected override int DefaultWidth => 1024;
    protected override int DefaultHeight => 768;
    
    private NewProjectPanel GetNewProjectPanel()
    {
        if (_newProjectPanel != null) return _newProjectPanel;
        _newProjectPanel ??= this.FindControl<NewProjectPanel>(nameof(NewProjectPanel))!;
        return _newProjectPanel;
    }
    
    private EditProjectPanel GetEditProjectPanel()
    {
        if (_editProjectPanel != null) return _editProjectPanel;
        _editProjectPanel ??= this.FindControl<EditProjectPanel>(nameof(EditProjectPanel))!;
        return _editProjectPanel;
    }
    
    private void EditProjectPanel_OnOnCloseProject(object? sender, EventArgs e)
    {
        GetNewProjectPanel().ResetModel();
        _service?.CloseEditProjectPanel();
    }

    private void NewProjectPanel_OnOnProjectSelected(object? sender, ValueEventArgs<MsuProject> e)
    {
        _service?.OpenEditProjectPanel(e.Data);
    }

    private void GitHubUrlLink_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.OpenGitHubReleasePage();
    }

    private void CloseUpdateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.CloseNewReleaseBanner(false);
    }

    private void DisableUpdatesLink_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.CloseNewReleaseBanner(true);
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
        if (backupProject != null && await MessageWindow.ShowYesNoDialog(
                "A backup with unsaved changes was detected. Would you like to load from the backup instead?",
                "Load Backup?", this))
        {
            project = backupProject;
        }
        
        var msuProjectWindow = new MsuProjectWindow(project, this);
        msuProjectWindow.Show();
    }

    private async void BrowseProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var path = await OpenMsuProjectFilePicker(false);
        if (string.IsNullOrEmpty(path)) return;
        await LoadProject(path);
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
}