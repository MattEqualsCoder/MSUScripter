using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaControls;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using GitHubReleaseChecker;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MainWindowService(Settings settings, SettingsService settingsService, MsuPcmService msuPcmService, PyMusicLooperService pyMusicLooperService, ProjectService projectService, IGitHubReleaseCheckerService gitHubReleaseCheckerService, IMsuTypeService msuTypeService) : ControlService
{
    private readonly MainWindowViewModel _model = new();

    public MainWindowViewModel InitializeModel()
    {
        _ = CheckForNewRelease();
        _ = CleanUpFolders();
        OpenCommandlineArgsProject();
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
        _model.HasDoneFirstTimeSetup = settings.HasDoneFirstTimeSetup;
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
    
    public void OpenEditProjectPanel(MsuProject project)
    {
        _model.CurrentMsuProject = project;
        UpdateTitle();
    }

    public void CloseEditProjectPanel()
    {
        _model.CurrentMsuProject = null;
        UpdateTitle();
    }

    public void OpenGitHubReleasePage()
    {
        if (string.IsNullOrEmpty(_model.GitHubReleaseUrl)) return;
        CrossPlatformTools.OpenUrl(_model.GitHubReleaseUrl);
    }

    public void CloseNewReleaseBanner(bool permanently)
    {
        if (permanently)
        {
            settings.CheckForUpdates = false;
            settingsService.SaveSettings();
        }

        _model.GitHubReleaseUrl = "";
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
        catch (Exception e)
        {
            return (null, null, "Error opening project. Please contact MattEqualsCoder or post an issue on GitHub");
        }
    }

    public bool ValidateProjectPaths(MsuProject project)
    {
        return project.Tracks.SelectMany(x => x.Songs).All(x => x.MsuPcmInfo.AreFilesValid());
    }

    public bool ValidateMsuPcm(string msupcmPath)
    {
        return msuPcmService.ValidateMsuPcmPath(msupcmPath, out _);
    }

    public void UpdateHasDoneFirstTimeSetup(string? msupcmPath)
    {
        settings.HasDoneFirstTimeSetup = true;
        settings.MsuPcmPath = msupcmPath;
        settingsService.SaveSettings();
    }
    
    public void Shutdown()
    {
        settingsService.SaveSettings();
        msuPcmService.DeleteTempPcms();
        msuPcmService.DeleteTempJsonFiles();
    }

    public void UpdateTitle()
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
            return projectService.NewMsuProject(projectPath, msuType, msuPath, msuPcmJson, msuPcmWorkingDir);
        }
        catch (Exception exception)
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
    
    public bool IsEditPanelDisplayed => _model.DisplayEditPage;

    private void OpenCommandlineArgsProject()
    {
        if (string.IsNullOrEmpty(Program.StartingProject)) return;
        
        _model.InitProject = projectService.LoadMsuProject(Program.StartingProject, false);

        if (_model.InitProject == null)
        {
            _model.InitProjectError = true;
            return;
        }

        if (!string.IsNullOrEmpty(_model.InitProject.BackupFilePath))
        {
            _model.InitBackupProject = projectService.LoadMsuProject(_model.InitProject.BackupFilePath, true);
        }
    }
    
    private async Task CheckForNewRelease()
    {
        if (settings.CheckForUpdates == false) return;

        var newerGitHubRelease = await gitHubReleaseCheckerService.GetGitHubReleaseToUpdateToAsync("MattEqualsCoder",
            "MSUScripter", App.Version, settings.PromptOnPreRelease);

        if (newerGitHubRelease != null)
        {
            _model.GitHubReleaseUrl = newerGitHubRelease.Url;
        }
    }
    
    private async Task CleanUpFolders()
    {
        await ITaskService.Run(() =>
        {
            msuPcmService.DeleteTempPcms();
            msuPcmService.DeleteTempJsonFiles();
            msuPcmService.ClearCache();
            pyMusicLooperService.ClearCache();
        });
    }
    
    
}