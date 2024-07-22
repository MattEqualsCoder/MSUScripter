using System.IO;
using System.Threading.Tasks;
using AvaloniaControls;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using GitHubReleaseChecker;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class MainWindowService(Settings settings, SettingsService settingsService, MsuPcmService msuPcmService, PyMusicLooperService pyMusicLooperService, ProjectService projectService, IGitHubReleaseCheckerService gitHubReleaseCheckerService) : ControlService
{
    private MainWindowViewModel _model = new();

    public MainWindowViewModel InitializeModel()
    {
        _ = CheckForNewRelease();
        _ = CleanUpFolders();
        OpenCommandlineArgsProject();
        _model.AppVersion = $" v{App.Version}";
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
            settings.PromptOnUpdate = false;
            settingsService.SaveSettings();
        }

        _model.GitHubReleaseUrl = "";
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
        if (settings.PromptOnUpdate == false) return;

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