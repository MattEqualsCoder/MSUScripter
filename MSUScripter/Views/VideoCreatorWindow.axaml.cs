using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaControls.Controls;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class VideoCreatorWindow : Window
{
    private VideoCreatorService? _videoCreatorService;

    public VideoCreatorWindow(): this(null)
    {
    }
    
    public VideoCreatorWindow(VideoCreatorService? videoCreatorService)
    {
        _videoCreatorService = videoCreatorService;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        InitializeComponent();

        if (_videoCreatorService != null)
        {
            _videoCreatorService.VideoCreationCompleted += VideoCreatorServiceOnVideoCreationCompleted;
        }
    }

    private void VideoCreatorServiceOnVideoCreationCompleted(object? sender, VideoCreatorServiceEventArgs e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (e.Successful)
            {
                this.Find<TextBlock>(nameof(MessageTextBlock))!.Text = "Video generation successful!";
            }
            else
            {
                this.Find<TextBlock>(nameof(MessageTextBlock))!.Text = $"Error: {e.Message}";
            }
            
            this.Find<Button>(nameof(OkButton))!.Content = "OK";
        });
    }

    public MsuProjectViewModel? Project { get; set; }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_videoCreatorService == null || Project == null)
        {
            return;
        }

        _ = GetFile();
    }

    private async Task GetFile()
    {
        var topLevel = GetTopLevel(this);

        if (topLevel == null) return;

        IStorageFolder? previousFolder;
        if (!string.IsNullOrEmpty(SettingsService.Instance.Settings.PreviousPath))
        {
            previousFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(SettingsService.Instance.Settings.PreviousPath);    
        }
        else
        {
            previousFolder = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
        }
        
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select mp4 file",
            FileTypeChoices = new List<FilePickerFileType>()
            {
                new("MP4 Video File")
                {
                    Patterns = new List<string>()
                    {
                        "*.mp4"
                    }
                }
            },
            ShowOverwritePrompt = true,
            SuggestedStartLocation = previousFolder,
        });

        if (!string.IsNullOrEmpty(file?.Path.LocalPath))
        {
            var path = file.Path.LocalPath;
            if (!path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                path += ".mp4";
            }
            RunVideoCreator(path);
        }
        else
        {
            Close();
        }
    }

    private void RunVideoCreator(string outputPath)
    {
        string message;
        if (_videoCreatorService!.CreateVideo(Project!, outputPath, out message, out var showGitHub))
        {
            this.Find<TextBlock>(nameof(MessageTextBlock))!.Text = "Creating video (this could take a while)";
        }
        else
        {
            this.Find<TextBlock>(nameof(MessageTextBlock))!.Text = $"Error: {message}";

            if (showGitHub)
            {
                this.Find<LinkControl>(nameof(GitHubLink))!.IsVisible = true;    
            }
        }
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_videoCreatorService?.IsRunning == true)
        {
            _videoCreatorService.Cancel();
        }
    }

    private void GitHubLink_OnClick(object? sender, RoutedEventArgs e)
    {
        var url = "https://github.com/MattEqualsCoder/MSUTestVideoCreator";
        
        try
        {
            Process.Start(url);
        }
        catch
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}