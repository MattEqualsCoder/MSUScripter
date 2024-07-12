using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using AvaloniaControls.Controls;
using AvaloniaControls.Models;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.Services;

namespace MSUScripter.Views;

public partial class NewProjectPanel : UserControl
{
    private readonly IMsuTypeService? _msuTypeService;
    private readonly ProjectService? _projectService;
    private readonly Settings? _settings;
    private readonly ILogger<NewProjectPanel>? _logger;

    public NewProjectPanel() : this(null, null, null, null)
    {
        
    }
    
    public NewProjectPanel(IMsuTypeService? msuTypeService, ProjectService? projectService, Settings? settings, ILogger<NewProjectPanel>? logger)
    {
        _msuTypeService = msuTypeService;
        _projectService = projectService;
        _settings = settings;
        _logger = logger;
        InitializeComponent();
    }
    
    
    public MsuProject? Project { get; set; }

    private void PopulateMsuTypeComboBox()
    {
        if (_msuTypeService == null) return;
        var msuTypeNames =  _msuTypeService.MsuTypes
            .Where(x => x.Selectable)
            .OrderBy(x => x.DisplayName)
            .Select(x => x.DisplayName);
        var comboBox = this.Find<ComboBox>("MsuTypeComboBox");
        if (comboBox != null)
            comboBox.ItemsSource = msuTypeNames;
    }
    
    public EventHandler? OnProjectSelected;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public ICollection<RecentProject> RecentProjects { get; set; } = new List<RecentProject>();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        PopulateMsuTypeComboBox();
        
        var recentProjectsTextBlock = this.Find<TextBlock>(nameof(RecentProjectsTextBlock));
        if (recentProjectsTextBlock != null)
        {
            recentProjectsTextBlock.IsVisible =
                _settings?.RecentProjects.Any(x => File.Exists(x.ProjectPath)) == true;
        }
            
        var recentProjectsList = this.Find<ItemsControl>(nameof(RecentProjectsList));
        if (recentProjectsList != null)
        {
            recentProjectsList.ItemsSource = _settings?.RecentProjects.Where(x => File.Exists(x.ProjectPath))
                .OrderByDescending(x => x.Time).ToList();
        }
    }

    private async void NewProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null || _projectService == null || _msuTypeService == null) return;
        
        var msuTypeName = this.Find<ComboBox>(nameof(MsuTypeComboBox))?.SelectedItem as string;
        var msuPath = this.Find<FileControl>(nameof(MsuPath))?.FilePath;
        var tracksJsonPath = this.Find<FileControl>(nameof(MsuPcmJsonFile))?.FilePath;
        var msuPcmWorking = this.Find<FileControl>(nameof(MsuPcmWorkingDirectory))?.FilePath;
        var msuType = _msuTypeService?.GetMsuType(msuTypeName);

        if (string.IsNullOrEmpty(msuPath))
        {
            await new MessageWindow(new MessageWindowRequest
            {
                Message = "Please enter a MSU path",
                Icon = MessageWindowIcon.Warning,
                Buttons = MessageWindowButtons.OK,
            }).ShowDialog(this);
            return;
        }

        if (msuType == null)
        {
            await new MessageWindow(new MessageWindowRequest
            {
                Message = "Please enter a valid MSU type",
                Icon = MessageWindowIcon.Warning,
                Buttons = MessageWindowButtons.OK,
            }).ShowDialog(this);
            return;
        }
        
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select MSU Scripter Project File",
            FileTypeChoices = new List<FilePickerFileType>()
            {
                new("MSU Scripter Project File") { Patterns = new List<string>() { "*.msup" }}
            },
            ShowOverwritePrompt = true
        });

        if (string.IsNullOrEmpty(file?.Path.LocalPath)) return;
        
        var projectPath = file.Path.LocalPath;

        if (!projectPath.EndsWith(".msup"))
        {
            projectPath += ".msup";
        }

        try
        {
            Project = _projectService.NewMsuProject(projectPath, msuType, msuPath, tracksJsonPath, msuPcmWorking);
        }
        catch (Exception exception)
        {
            await new MessageWindow(new MessageWindowRequest
            {
                Message = exception.Message,
                Icon = MessageWindowIcon.Error,
                Buttons = MessageWindowButtons.OK,
            }).ShowDialog(this);
            return;
        }
        
        if (Project.MsuType == _msuTypeService!.GetSMZ3LegacyMSUType() && _msuTypeService.GetSMZ3MsuType() != null)
        {
            var window = new MessageWindow(new MessageWindowRequest
            {
                Message = "This MSU is currently a classic SMZ3 MSU. Would you like to swap the tracks to the new order?",
                Title = "Swap Tracks?",
                Icon = MessageWindowIcon.Question,
                Buttons = MessageWindowButtons.YesNo,
            });

            await window.ShowDialog(this);

            if (window.DialogResult?.PressedAcceptButton == true)
            {
                _projectService.ConvertProjectMsuType(Project, _msuTypeService.GetSMZ3MsuType()!, true);
                _projectService.SaveMsuProject(Project, false);
            }
        }
        
        OnProjectSelected?.Invoke(this, EventArgs.Empty);
    }

    private void RecentProject_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_projectService == null) return;
        if (sender is not LinkControl { Tag: string projectPath }) return;
        _ = LoadProject(projectPath);
    }

    private async void SelectProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null || _projectService == null) return;
        
        var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Select MSU Scripter Project File",
            FileTypeFilter = new List<FilePickerFileType>()
            {
                new("MSU Scripter Project File") { Patterns = new List<string>() { "*.msup" }}
            },
        });

        if (string.IsNullOrEmpty(file.FirstOrDefault()?.Path.LocalPath))
        {
            return;
        }
        
        _ = LoadProject(file.First().Path.LocalPath);
    }

    private async Task LoadProject(string path)
    {
        try
        {
            Project = _projectService!.LoadMsuProject(path, false);
            if (!string.IsNullOrEmpty(Project!.BackupFilePath))
            {
                var backupProject = _projectService!.LoadMsuProject(Project!.BackupFilePath, true);
                if (backupProject != null && backupProject.LastSaveTime > Project.LastSaveTime)
                {
                    var window = new MessageWindow(new MessageWindowRequest
                    {
                        Message = "A backup with unsaved changes was detected. Would you like to load from the backup instead?",
                        Title = "Load Backup?",
                        Icon = MessageWindowIcon.Question,
                        Buttons = MessageWindowButtons.YesNo,
                    });

                    await window.ShowDialog(this);

                    if (window.DialogResult?.PressedAcceptButton == true)
                    {
                        Project = backupProject;
                    }
                }
            }

            OnProjectSelected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error opening project");
            await new MessageWindow(new MessageWindowRequest
            {
                Message = "Error opening project. Please contact MattEqualsCoder or post an issue on GitHub",
                Icon = MessageWindowIcon.Error,
                Buttons = MessageWindowButtons.OK,
            }).ShowDialog(this);
        }
    }

    private void ImportProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = ImportProject();
    }

    private async Task ImportProject()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null || _projectService == null) return;
        
        var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Select MSU Scripter Project File",
            FileTypeFilter = new List<FilePickerFileType>()
            {
                new("MSU Scripter Project File") { Patterns = new List<string>() { "*.msup" }}
            },
        });

        if (string.IsNullOrEmpty(file.FirstOrDefault()?.Path.LocalPath))
        {
            return;
        }

        var oldProject = _projectService!.LoadMsuProject(file.First().Path.LocalPath, false);

        if (oldProject == null)
        {
            return;
        }
        
        var window = new CopyProjectWindow();
        
        Project = await window.ShowDialog((Window)topLevel, oldProject);

        if (Project != null)
        {
            _projectService.SaveMsuProject(Project, false);
            OnProjectSelected?.Invoke(this, EventArgs.Empty);
        }
        
    }
}