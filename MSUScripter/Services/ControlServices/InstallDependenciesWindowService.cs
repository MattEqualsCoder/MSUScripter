using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class InstallDependenciesWindowService (MsuPcmService msuPcmService, PythonCompanionService pythonCompanionService, SettingsService settingsService) : ControlService
{
    private readonly InstallDependenciesWindowViewModel _viewModel = new();

    public InstallDependenciesWindowViewModel InitializeModel()
    {
        if (msuPcmService.IsValid)
        {
            _viewModel.MsuPcmState = InstallState.Valid;
        }
        else
        {
            _viewModel.MsuPcmState = InstallState.CanInstall;
        }
        
        if (pythonCompanionService.IsFfMpegValid)
        {
            _viewModel.FfmpegState = InstallState.Valid;
        }
        else
        {
            _viewModel.FfmpegState = InstallState.CanInstall;
        }
        
        if (pythonCompanionService.IsValid)
        {
            _viewModel.PyAppState = InstallState.Valid;
        }
        else
        {
            _viewModel.PyAppState = InstallState.CanInstall;
        }
        
        return _viewModel;
    }

    public async Task InstallMsuPcm()
    {
        _viewModel.MsuPcmState = InstallState.InProgress;
        _viewModel.MsuPcmInstallProgress = "Starting";
        var result = await msuPcmService.Install(progress =>
        {
            _viewModel.MsuPcmInstallProgress = progress;
        });
        if (result.Success)
        {
            _viewModel.MsuPcmState = InstallState.Valid;
        }
        else
        {
            _viewModel.MsuPcmState = InstallState.Error;
            _viewModel.MsuPcmErrorText = result.MissingSharedLibraries ? "Missing Libraries" : "Install Failed";
            _viewModel.MsuPcmErrorToolTip = result.MissingSharedLibraries
                ? "MsuPcm++ is missing some libraries that it is dependent on. Please click to view additional installation instructions."
                : "Failed to be able to download and run MsuPcm++. Please click to view additional installation instructions.";
        }
    }

    public async Task RetryMsuPcm()
    {
        if (msuPcmService.VerifyInstalled(out _))
        {
            _viewModel.MsuPcmState = InstallState.Valid;
            return;
        }
        await InstallMsuPcm();
    }
    
    public void RevalidateMsuPcm()
    {
        if (msuPcmService.VerifyInstalled(out _))
        {
            _viewModel.MsuPcmState = InstallState.Valid;
        }
    }

    public async Task InstallFfmpeg()
    {
        _viewModel.FfmpegState = InstallState.InProgress;
        _viewModel.FfmpegInstallProgress = "Starting";
        var result = await pythonCompanionService.InstallFfmpeg(progress =>
        {
            _viewModel.FfmpegInstallProgress = progress;
        });
        if (result)
        {
            _viewModel.FfmpegState = InstallState.Valid;
        }
        else
        {
            _viewModel.FfmpegState = InstallState.Error;
            _viewModel.MsuPcmErrorText = "Install Failed";
            _viewModel.MsuPcmErrorToolTip = "Failed to be able to download and run FFmpeg. Please click to view additional installation instructions.";
        }
    }
    
    public async Task InstallPyApp()
    {
        _viewModel.PyAppState = InstallState.InProgress;
        _viewModel.PyAppInstallProgress = "Starting";
        var result = await pythonCompanionService.InstallPyApp(progress =>
        {
            _viewModel.PyAppInstallProgress = progress;
        });
        if (result)
        {
            _viewModel.PyAppState = InstallState.Valid;
        }
        else
        {
            _viewModel.PyAppState = InstallState.Error;
            _viewModel.PyAppErrorText = "Install Failed";
            _viewModel.PyAppErrorToolTip = "Failed to be able to download and run Python Companion App. Please click to view additional installation instructions.";
        }
    }

    public void SaveSettings()
    {
        if (_viewModel.DontRemindMeAgain)
        {
            settingsService.Settings.IgnoreMissingDependencies = true;
            settingsService.TrySaveSettings();
        }
    }
}