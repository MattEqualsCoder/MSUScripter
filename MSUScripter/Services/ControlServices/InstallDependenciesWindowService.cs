using System;
using System.Threading.Tasks;
using AvaloniaControls.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

// ReSharper disable once ClassNeverInstantiated.Global
public class InstallDependenciesWindowService (MsuPcmService msuPcmService, PythonCompanionService pythonCompanionService, SettingsService settingsService) : ControlService
{
    private readonly InstallDependenciesWindowViewModel _viewModel = new();

    public InstallDependenciesWindowViewModel InitializeModel()
    {
        _viewModel.MsuPcmState = msuPcmService.IsValid ? InstallState.Valid : InstallState.CanInstall;
        _viewModel.FfmpegState = pythonCompanionService.IsFfMpegValid ? InstallState.Valid : InstallState.CanInstall;
        _viewModel.PyAppState = pythonCompanionService.IsValid ? InstallState.Valid : InstallState.CanInstall;
        _viewModel.InitialDontRemindMeAgain = settingsService.Settings.IgnoreMissingDependencies;
        return _viewModel;
    }

    public async Task InstallMsuPcm()
    {
        _viewModel.MsuPcmState = InstallState.InProgress;
        _viewModel.MsuPcmInstallProgress = "Starting";
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        var result = await msuPcmService.InstallAsync(progress =>
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
        if ((await msuPcmService.VerifyInstalledAsync()).Successful)
        {
            _viewModel.MsuPcmState = InstallState.Valid;
            return;
        }
        await InstallMsuPcm();
    }
    
    public async Task RevalidateMsuPcm()
    {
        if ((await msuPcmService.VerifyInstalledAsync()).Successful)
        {
            _viewModel.MsuPcmState = InstallState.Valid;
        }
    }

    public async Task InstallFfmpeg()
    {
        _viewModel.FfmpegState = InstallState.InProgress;
        _viewModel.FfmpegInstallProgress = "Starting";
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        var result = await pythonCompanionService.InstallFfmpegAsync(progress =>
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
    
    public async Task RetryFfmpeg()
    {
        if (await pythonCompanionService.VerifyFfMpegAsync())
        {
            _viewModel.FfmpegState = InstallState.Valid;
            return;
        }
        await InstallFfmpeg();
    }
    
    public async Task RevalidateFfmpeg()
    {
        if (await pythonCompanionService.VerifyFfMpegAsync())
        {
            _viewModel.FfmpegState = InstallState.Valid;
        }
    }
    
    public async Task InstallPyApp()
    {
        _viewModel.PyAppState = InstallState.InProgress;
        _viewModel.PyAppInstallProgress = "Starting";
        await Task.Delay(TimeSpan.FromMilliseconds(100));
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
    
    public async Task RetryPyApp()
    {
        if (await pythonCompanionService.VerifyInstalledAsync())
        {
            _viewModel.PyAppState = InstallState.Valid;
            return;
        }
        await InstallPyApp();
    }
    
    public async Task RevalidatePyApp()
    {
        if (await pythonCompanionService.VerifyInstalledAsync())
        {
            _viewModel.PyAppState = InstallState.Valid;
        }
    }

    public void SaveSettings()
    {
        settingsService.Settings.IgnoreMissingDependencies = _viewModel.DontRemindMeAgain;
        settingsService.TrySaveSettings();
    }
}