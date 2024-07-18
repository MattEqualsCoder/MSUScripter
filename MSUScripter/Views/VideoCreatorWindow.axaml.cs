using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaControls;
using AvaloniaControls.Extensions;
using MSUScripter.Services;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;
using FileInputControlType = AvaloniaControls.FileInputControlType;

namespace MSUScripter.Views;

public partial class VideoCreatorWindow : Window
{
    private VideoCreatorWindowService? _service;
    private VideoCreatorWindowViewModel _model;
    
    public VideoCreatorWindow()
    {
        DataContext = _model = (VideoCreatorWindowViewModel)new VideoCreatorWindowViewModel().DesignerExample();
        InitializeComponent();
    }
    
    public VideoCreatorWindow(MsuProjectViewModel project)
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        InitializeComponent();
        _service = this.GetControlService<VideoCreatorWindowService>();
        DataContext = _model = _service?.InitializeModel(project) ?? new VideoCreatorWindowViewModel();
    }


    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_service?.CanCreateVideo != true) return;
        
        var topLevel = GetTopLevel(this);

        if (topLevel == null) return;

        IStorageFolder? previousFolder;
        if (!string.IsNullOrEmpty(_model.PreviousPath))
        {
            previousFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(_model.PreviousPath);    
        }
        else
        {
            previousFolder = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
        }

        var file = await CrossPlatformTools.OpenFileDialogAsync(this, FileInputControlType.SaveFile, "MP4 Video File:*.mp4",
            previousFolder?.Path.LocalPath, "Select mp4 file");
        
        if (!string.IsNullOrEmpty(file?.Path.LocalPath))
        {
            var path = file.Path.LocalPath;
            if (!path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                path += ".mp4";
            }

            _service?.CreateVideo(path);
        }
        else
        {
            Close();
        }
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _service?.Cancel();
    }

    private void GitHubLink_OnClick(object? sender, RoutedEventArgs e)
    {
        CrossPlatformTools.OpenUrl("https://github.com/MattEqualsCoder/MSUTestVideoCreator");
    }
}