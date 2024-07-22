using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class PackageMsuWindow : Window
{
    private readonly PackageMsuWindowService? _service;
    
    public PackageMsuWindow()
    {
        InitializeComponent();
        DataContext = new PackageMsuWindowViewModel().DesignerExample();
    }

    public PackageMsuWindow(MsuProjectViewModel project)
    {
        InitializeComponent();
        _service = this.GetControlService<PackageMsuWindowService>();
        DataContext = _service?.InitializeModel(project);
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        
        var zipPath = await CrossPlatformTools.OpenFileDialogAsync(this, FileInputControlType.SaveFile, "Zip File:*.zip",
            _service.MsuDirectory, "Select Desired MSU Zip File");

        if (zipPath == null || string.IsNullOrEmpty(zipPath.Path.LocalPath))
        {
            Close();
            return;
        }
        
        _service.PackageProject(zipPath.Path.LocalPath);
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _service?.Cancel();
    }
}