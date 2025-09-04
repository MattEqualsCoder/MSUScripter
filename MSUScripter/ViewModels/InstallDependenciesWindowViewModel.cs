using AvaloniaControls.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class InstallDependenciesWindowViewModel : ViewModelBase
{
    [Reactive] public bool DontRemindMeAgain { get; set; } = true;
    public bool CanClickInstallButton => !ShowMsuPcmProgress && !ShowFfmpegProgress && !ShowPyAppProgress;
    
    
    [Reactive, ReactiveLinkedProperties(nameof(ShowInstallMsuPcmButton), nameof(ShowMsuPcmVerifiedText), nameof(ShowMsuPcmProgress), nameof(ShowMsuPcmError), nameof(CanClickInstallButton))] 
    public InstallState MsuPcmState { get; set; }
    public bool ShowInstallMsuPcmButton => MsuPcmState == InstallState.CanInstall;
    public bool ShowMsuPcmVerifiedText => MsuPcmState == InstallState.Valid;
    public bool ShowMsuPcmProgress => MsuPcmState == InstallState.InProgress;
    public bool ShowMsuPcmError => MsuPcmState == InstallState.Error;
    [Reactive] public string MsuPcmInstallProgress { get; set; } = string.Empty;
    [Reactive] public string MsuPcmErrorText { get; set; } = "Install Failed";
    [Reactive] public string MsuPcmErrorToolTip { get; set; } = "Install Failed";
    
    
    [Reactive, ReactiveLinkedProperties(nameof(ShowInstallFfmpegButton), nameof(ShowFfmpegVerifiedText), nameof(ShowFfmpegProgress), nameof(ShowFfmpegError), nameof(CanClickInstallButton))] 
    public InstallState FfmpegState { get; set; }
    public bool ShowInstallFfmpegButton => FfmpegState == InstallState.CanInstall;
    public bool ShowFfmpegVerifiedText => FfmpegState == InstallState.Valid;
    public bool ShowFfmpegProgress => FfmpegState == InstallState.InProgress;
    public bool ShowFfmpegError => FfmpegState == InstallState.Error;
    [Reactive] public string FfmpegInstallProgress { get; set; } = string.Empty;
    [Reactive] public string FfmpegErrorText { get; set; } = "Install Failed";
    [Reactive] public string FfmpegErrorToolTip { get; set; } = "Install Failed";
    
    
    [Reactive, ReactiveLinkedProperties(nameof(ShowInstallPyAppButton), nameof(ShowPyAppVerifiedText), nameof(ShowPyAppProgress), nameof(ShowPyAppError), nameof(CanClickInstallButton))] 
    public InstallState PyAppState { get; set; }
    public bool ShowInstallPyAppButton => PyAppState == InstallState.CanInstall;
    public bool ShowPyAppVerifiedText => PyAppState == InstallState.Valid;
    public bool ShowPyAppProgress => PyAppState == InstallState.InProgress;
    public bool ShowPyAppError => PyAppState == InstallState.Error;
    [Reactive] public string PyAppInstallProgress { get; set; } = string.Empty;
    [Reactive] public string PyAppErrorText { get; set; } = "Install Failed";
    [Reactive] public string PyAppErrorToolTip { get; set; } = "Install Failed";
        
    public override ViewModelBase DesignerExample()
    {
        return new InstallDependenciesWindowViewModel();
    }
}

public enum InstallState
{
    Valid,
    CanInstall,
    InProgress,
    Error
}