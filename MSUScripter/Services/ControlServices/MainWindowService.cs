using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using AppImageDesktopFileCreator;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using GitHubReleaseChecker;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

// ReSharper disable once ClassNeverInstantiated.Global
public class MainWindowService(
    Settings settings,
    SettingsService settingsService,
    MsuPcmService msuPcmService,
    ProjectService projectService,
    IGitHubReleaseCheckerService gitHubReleaseCheckerService,
    PythonCompanionService pythonCompanionService,
    IMsuTypeService msuTypeService,
    ILogger<MainWindowService> logger) : ControlService
{
    private readonly MainWindowViewModel _model = new();

    public MainWindowViewModel InitializeModel()
    {
        _ = CleanUpFolders();
        
        _model.InitProject = Program.StartingProject;
        _model.AppVersion = $" v{App.Version}";
        
        if (!settings.HasDoneFirstTimeSetup && !string.IsNullOrEmpty(settings.MsuPcmPath))
        {
            settings.HasDoneFirstTimeSetup = true;
            settingsService.SaveSettings();
        }
        
        _model.Settings.LoadSettings(settings);
        
        _model.MsuTypes = msuTypeService.MsuTypes
            .OrderBy(x => x.DisplayName)
            .ToList();
        _model.RecentProjects = settings.RecentProjects.ToList();

        if (_model.RecentProjects.Count != 0)
        {
            _model.DisplayNewProjectPage = false;
            _model.DisplayOpenProjectPage = true;
        }
        else
        {
            _model.DisplayNewProjectPage = true;
            _model.DisplayOpenProjectPage = false;
        }
        
        UpdateTitle();
        return _model;
    }

    public (MsuProject? mainProject, MsuProject? backupProject, string? error) LoadProject(string? path = null)
    {
        path ??= _model.SelectedRecentProject?.ProjectPath;

        if (string.IsNullOrEmpty(path))
        {
            return (null, null, "Invalid project path");
        }
        
        try
        {
            var project = projectService.LoadMsuProject(path, false);
            MsuProject? backupProject = null;

            if (project == null)
            {
                return (null, null, "Project not found");
            }
            
            if (!string.IsNullOrEmpty(project.BackupFilePath))
            {
                var potentialBackupProject = projectService.LoadMsuProject(project.BackupFilePath, true);
                if (potentialBackupProject != null && potentialBackupProject.LastSaveTime > project.LastSaveTime)
                {
                    backupProject = potentialBackupProject;
                }
            }

            return (project, backupProject, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error opening project");
            return (null, null, "Error opening project. Please contact MattEqualsCoder or post an issue on GitHub");
        }
    }

    public bool ValidateProjectPaths(MsuProject project)
    {
        return project.Tracks.SelectMany(x => x.Songs).All(x => x.MsuPcmInfo.AreFilesValid());
    }

    public void UpdateLegacySmz3Project(MsuProject project)
    {
        projectService.ConvertLegacySmz3Project(project);
        projectService.SaveMsuProject(project, false);
    }

    public bool IsLegacySmz3Project(MsuProject project)
    {
        return project.MsuType.DisplayName == "SMZ3 Classic (Metroid First)";
    }

    public void RefreshRecentProjects()
    {
        _model.RecentProjects = settings.RecentProjects.Where(x => File.Exists(x.ProjectPath)).ToList();
        settingsService.TrySaveSettings();
    }

    public async Task<bool> ValidateDependencies()
    {
        var isMsuPcmServiceValid = await msuPcmService.VerifyInstalledAsync();
        var isCompanionServiceValid = await pythonCompanionService.VerifyInstalledAsync();
        if (settings.IgnoreMissingDependencies)
        {
            return true;
        }
        return isMsuPcmServiceValid.Successful && isCompanionServiceValid;
    }
    
    public void Shutdown()
    {
        settingsService.SaveSettings();
        CleanDirectory(Directories.TempFolder);
    }

    private void UpdateTitle()
    {
        if (_model.CurrentMsuProject == null)
        {
            _model.Title = $"MSU Scripter{_model.AppVersion}";
        }
        else
        {
            _model.Title = string.IsNullOrEmpty(_model.CurrentMsuProject.BasicInfo.PackName)
                ? $"{new FileInfo(_model.CurrentMsuProject.ProjectFilePath).Name} - MSU Scripter"
                : $"{_model.CurrentMsuProject.BasicInfo.PackName} - MSU Scripter";
        }
    }

    public MsuProject? CreateNewProject()
    {
        var name = _model.MsuProjectName;
        var creator = _model.MsuCreatorName;
        var msuPath = _model.MsuPath;
        var projectPath = _model.MsuProjectPath;
        var msuType = _model.SelectedMsuType;
        var msuPcmJson = _model.MsuPcmJsonPath;
        var msuPcmWorkingDir = _model.MsuPcmWorkingPath;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(creator) || string.IsNullOrEmpty(msuPath) ||
            string.IsNullOrEmpty(projectPath) || msuType == null)
        {
            return null;
        }
        
        try
        {
            logger.LogInformation("Creating new MSU Project");
            return projectService.NewMsuProject(projectPath, msuType, msuPath, msuPcmJson, msuPcmWorkingDir, name, creator);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void SaveSettings()
    {
        if (!_model.DisplaySettingsPage)
        {
            return;
        }
        
        _model.Settings.SaveChanges();
        settingsService.SaveSettings();
    }

    public void LogError(Exception ex, string message)
    {
        logger.LogError(ex, "{Message}", message);
    }

    private bool CleanDirectory(string path, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.Zero;
        var currentDateTime = DateTime.UtcNow;
        var isEmpty = true;
        foreach (var file in Directory.EnumerateFiles(path))
        {
            var fileInfo = new FileInfo(file);
            if (currentDateTime - fileInfo.LastWriteTimeUtc > timeout)
            {
                try
                {
                    fileInfo.Delete();
                }
                catch
                {
                    // Do nothing
                }
            }
            else
            {
                isEmpty = false;
            }
        }

        foreach (var folder in Directory.EnumerateDirectories(path))
        {
            if (CleanDirectory(folder, timeout))
            {
                try
                {
                    Directory.Delete(folder);
                }
                catch
                {
                    // Do nothing
                }
            }
            else
            {
                isEmpty = false;
            }
        }
        
        return isEmpty;
    }
    
    public async Task<string> CheckForNewRelease()
    {
        if (!settings.CheckForUpdates)
        {
            return string.Empty;
        }

        var newerGitHubRelease = await gitHubReleaseCheckerService.GetGitHubReleaseToUpdateToAsync("MattEqualsCoder",
            "MSUScripter", App.Version, settings.PromptOnPreRelease);

        if (newerGitHubRelease != null)
        {
            return newerGitHubRelease.Url;
        }
        
        return string.Empty;
    }

    [SupportedOSPlatform("linux")]
    public void CreateDesktopFile()
    {
        ITaskService.Run(() =>
        {
            var response = Program.BuildLinuxDesktopFile();

            if (response.Success)
            {
                logger.LogInformation("Created desktop file for AppImage");
            }
            else
            {
                logger.LogError("Error creating desktop fie for AppImage: {Error}",
                    response.ErrorMessage ?? "Unknown Error");
            }
        });
    }

    public void SkipDesktopFile()
    {
        settingsService.Settings.SkipDesktopFile = true;
        settingsService.TrySaveSettings();
    }

    public void IgnoreFutureUpdates()
    {
        settings.CheckForUpdates = false;
        settingsService.SaveSettings();
    }
    
    private async Task CleanUpFolders()
    {
        await ITaskService.Run(() =>
        {
            CleanDirectory(Directories.CacheFolder, TimeSpan.FromDays(30));
            CleanDirectory(Directories.TempFolder);
        });
    }
}