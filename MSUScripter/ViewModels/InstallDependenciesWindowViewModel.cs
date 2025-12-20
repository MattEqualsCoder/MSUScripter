using AvaloniaControls.Models;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public partial class InstallDependenciesWindowViewModel : ViewModelBase
{
    [Reactive] public partial bool DontRemindMeAgain { get; set; }
    public bool InitialDontRemindMeAgain { get; set; }
    public bool CanClickInstallButton => !ShowMsuPcmProgress && !ShowFfmpegProgress && !ShowPyAppProgress;
    
    
    [Reactive, ReactiveLinkedProperties(nameof(ShowInstallMsuPcmButton), nameof(ShowMsuPcmVerifiedText), nameof(ShowMsuPcmProgress), nameof(ShowMsuPcmError), nameof(CanClickInstallButton))] 
    public partial InstallState MsuPcmState { get; set; }
    public bool ShowInstallMsuPcmButton => MsuPcmState == InstallState.CanInstall;
    public bool ShowMsuPcmVerifiedText => MsuPcmState == InstallState.Valid;
    public bool ShowMsuPcmProgress => MsuPcmState == InstallState.InProgress;
    public bool ShowMsuPcmError => MsuPcmState == InstallState.Error;
    [Reactive] public partial string MsuPcmInstallProgress { get; set; }
    [Reactive] public partial string MsuPcmErrorText { get; set; }
    [Reactive] public partial string MsuPcmErrorToolTip { get; set; }
    
    
    [Reactive, ReactiveLinkedProperties(nameof(ShowInstallFfmpegButton), nameof(ShowFfmpegVerifiedText), nameof(ShowFfmpegProgress), nameof(ShowFfmpegError), nameof(CanClickInstallButton))] 
    public partial InstallState FfmpegState { get; set; }
    public bool ShowInstallFfmpegButton => FfmpegState == InstallState.CanInstall;
    public bool ShowFfmpegVerifiedText => FfmpegState == InstallState.Valid;
    public bool ShowFfmpegProgress => FfmpegState == InstallState.InProgress;
    public bool ShowFfmpegError => FfmpegState == InstallState.Error;
    [Reactive] public partial string FfmpegInstallProgress { get; set; }
    [Reactive] public partial string FfmpegErrorText { get; set; }
    [Reactive] public partial string FfmpegErrorToolTip { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(ShowInstallPyAppButton), nameof(ShowPyAppVerifiedText), nameof(ShowPyAppProgress), nameof(ShowPyAppError), nameof(CanClickInstallButton))] 
    public partial InstallState PyAppState { get; set; }
    public bool ShowInstallPyAppButton => PyAppState == InstallState.CanInstall;
    public bool ShowPyAppVerifiedText => PyAppState == InstallState.Valid;
    public bool ShowPyAppProgress => PyAppState == InstallState.InProgress;
    public bool ShowPyAppError => PyAppState == InstallState.Error;
    [Reactive] public partial string PyAppInstallProgress { get; set; }
    [Reactive] public partial string PyAppErrorText { get; set; }
    [Reactive] public partial string PyAppErrorToolTip { get; set; }

    public InstallDependenciesWindowViewModel()
    {
        DontRemindMeAgain = true;
        MsuPcmInstallProgress = string.Empty;
        MsuPcmErrorText = "Install Failed";
        MsuPcmErrorToolTip = "Install Failed";
        FfmpegInstallProgress = string.Empty;
        FfmpegErrorText = "Install Failed";
        FfmpegErrorToolTip = "Install Failed";
        PyAppInstallProgress = string.Empty;
        PyAppErrorText = "Install Failed";
        PyAppErrorToolTip = "Install Failed";
    }
        
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