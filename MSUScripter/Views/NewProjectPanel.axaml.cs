using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Configs;
using MSUScripter.Events;
using MSUScripter.Services.ControlServices;
using MSUScripter.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class NewProjectPanel : UserControl
{
    private readonly NewProjectPanelService? _service;
    private readonly NewProjectPanelViewModel _model;
    
    public NewProjectPanel()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = _model = (NewProjectPanelViewModel)new NewProjectPanelViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<NewProjectPanelService>();
            DataContext = _model = _service?.InitializeModel() ?? new NewProjectPanelViewModel();
            _service?.ResetModel();
        }
    }
    
    public MsuProject? Project { get; set; }


    public event EventHandler<ValueEventArgs<MsuProject>>? OnProjectSelected;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void ResetModel()
    {
        _service?.ResetModel();
    }

    public async Task LoadProject(MsuProject project, MsuProject? backupProject)
    {
        if (backupProject != null && await MessageWindow.ShowYesNoDialog(
                "A backup with unsaved changes was detected. Would you like to load from the backup instead?",
                "Load Backup?", ParentWindow))
        {
            project = backupProject;
        }

        // var msuProjectWindow = new MsuProjectWindow(project);
        // await msuProjectWindow.ShowDialog(this.ParentWindow);
        //     
        // OnProjectSelected?.Invoke(this, new ValueEventArgs<MsuProject>(project));
    }
    
    private async void NewProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        
        var path = await OpenMsuProjectFilePicker(true);

        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        
        if (!_service.CreateNewProject(path, out var newProject, out var isLegacySmz3, out var error) || newProject == null)
        {
            await MessageWindow.ShowErrorDialog(error ?? "Error creating new project", "Error", ParentWindow);
            return;
        }

        if (isLegacySmz3)
        {
            var result = await MessageWindow.ShowYesNoDialog(
                "You have selected the legacy SMZ3 MSU type. While this will be compatible with SMZ3 Cas' and the old ips patch, it will not be compatible with the beta of Total's original SMZ3 version. Would you like to swap the tracks to the new SMZ3 MSU order?",
                "Update MSU Type?", ParentWindow);

            if (result && _service.UpdateLegacySmz3Msu(newProject) == false)
            {
                await MessageWindow.ShowErrorDialog("There was an error updating the classic SMZ3 MSU.", "Error", ParentWindow);
                return;
            }
        }

        OnProjectSelected?.Invoke(this, new ValueEventArgs<MsuProject>(newProject));
    }

    private async void RecentProject_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        if (sender is not LinkControl { Tag: string projectPath }) return;
        await LoadProject(projectPath);
    }

    private async void SelectProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;

        var path = await OpenMsuProjectFilePicker(false);

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        await LoadProject(path);
    }

    private async Task LoadProject(string path)
    {
        if (_service == null) return;
        
        if (_service.LoadProject(path, out var project, out var backupProject, out var error) && project != null)
        {
            await LoadProject(project, backupProject);
        }
        else
        {
            await MessageWindow.ShowErrorDialog(
                error ?? "Could not open MSU Project. Please contact MattEqualsCoder on GitHub",
                "Error Loading Project");
        }
    }

    private async void ImportProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        
        var path = await OpenMsuProjectFilePicker(false);

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (!_service.LoadProject(path, out var oldProject, out _, out var error) || oldProject == null)
        {
            await MessageWindow.ShowErrorDialog(error ?? "Could not load previous project file", "Error", ParentWindow);
            return;
        }
        
        var window = new CopyProjectWindow();
        
        // var project = await window.ShowDialog(ParentWindow, oldProject);

        // if (project != null)
        // {
        //     _service.SaveProject(project);
        //     OnProjectSelected?.Invoke(this, new ValueEventArgs<MsuProject>(project));
        // }
    }

    private async Task<string?> OpenMsuProjectFilePicker(bool isSave)
    {
        var folder = string.IsNullOrEmpty(_model.MsuPath)
            ? await this.GetDocumentsFolderPath()
            : Path.GetDirectoryName(_model.MsuPath);
        var path = await CrossPlatformTools.OpenFileDialogAsync(ParentWindow, isSave ? FileInputControlType.SaveFile : FileInputControlType.OpenFile,
            "MSU Scripter Project File:*.msup", folder);
        return path?.Path.LocalPath;
    }

    private Window ParentWindow => TopLevel.GetTopLevel(this) as Window ?? App.MainWindow;

    private async void MenuButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow();
        await window.ShowDialog(ParentWindow);
    }
}