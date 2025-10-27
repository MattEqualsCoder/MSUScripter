using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class InstallDependenciesWindow : ScalableWindow
{
    private readonly InstallDependenciesWindowService? _service;
    private readonly InstallDependenciesWindowViewModel _viewModel;
    
    public InstallDependenciesWindow()
    {
        InitializeComponent();
        
        if (Design.IsDesignMode)
        {
            DataContext = _viewModel = (InstallDependenciesWindowViewModel)new InstallDependenciesWindowViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<InstallDependenciesWindowService>();
            DataContext = _viewModel = _service?.InitializeModel() ?? new InstallDependenciesWindowViewModel();
        }
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _viewModel.DontRemindMeAgain = _viewModel.InitialDontRemindMeAgain;
    }

    private void InstallMsuPcmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.InstallMsuPcm();
    }

    private void LinkControlOpenTagButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not LinkControl { Tag: string url })
        {
            return;
        }

        CrossPlatformTools.OpenUrl(url);
    }

    private void RetryMsuPcmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.RetryMsuPcm();
    }
    
    private void RevalidateMsuPcmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.RevalidateMsuPcm();
    }

    private void InstallFfmpegButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.InstallFfmpeg();
    }

    private void RetryFfmpegButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.RetryFfmpeg();
    }
    
    private void RevalidateFfmpegButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.RevalidateFfmpeg();
    }

    private void InstallPyAppButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.InstallPyApp();
    }
    
    private void RetryPyAppButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.RetryPyApp();
    }
    
    private void RevalidatePyAppButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.RevalidatePyApp();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveSettings();
        Close();
    }
}