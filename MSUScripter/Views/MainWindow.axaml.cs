using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
}