using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using MSUScripter.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class MusicLooperWindow : Window
{
    private PyMusicLooperService? _pyMusicLooperService;
    
    public MusicLooperWindow() : this(null)
    {
    }
    
    public MusicLooperWindow(PyMusicLooperService? pyMusicLooperService)
    {
        _pyMusicLooperService = pyMusicLooperService;
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
    
    public bool? Result { get; set; }
    
    public MsuSongMsuPcmInfoViewModel? Model { get; set; }
    
    public async Task<bool?> ShowDialog()
    {
        if (App._mainWindow == null) return null;
        return await ShowDialog(App._mainWindow);
    }
    
    public new async Task<bool?> ShowDialog(Window window)
    {
        if (Model == null) return null;
        await base.ShowDialog(window);
        return Result;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_pyMusicLooperService == null || Model == null) return;
        
        var file = Model.File;

        if (string.IsNullOrEmpty(file) || !File.Exists(file))
        {
            DisplayMessage("No file specified for this song.", true);
            return;
        }
        
        if (!_pyMusicLooperService.TestService(out var validationMessage))
        {
            DisplayGetPyMusicLooper(validationMessage);
            return;
        }
        
        DisplayMessage("Running PyMusicLooper", false);
        
        await Task.Run(() =>
        {
            var successful = _pyMusicLooperService.GetLoopPoints(file, out var message, out var loopStart, out var loopEnd);

            if (!successful)
            {
                DisplayMessage($"Error with PyMusicLooper:\n{message}", true);
            }
            else if (Result == null)
            {
                Result = true;
                Model.TrimEnd = loopEnd;
                Model.Loop = loopStart;
            }

            return Task.CompletedTask;
        });

        if (Result == true)
        {
            Close();
        }
    }

    private void DisplayGetPyMusicLooper(string validationMessage)
    {
        DisplayMessage(
            $"Error with PyMusicLooper: {validationMessage}.",
            true);
        this.Find<LinkControl>(nameof(GitHubLink))!.IsVisible = true;
    }

    private void DisplayMessage(string message, bool isError)
    {
        if (!CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => DisplayMessage(message, isError));
            return;
        }
        
        this.Find<TextBlock>(nameof(MessageTextBlock))!.Text = message;
        
        if (isError)
        {
            this.Find<StackPanel>(nameof(MessageStackPanel))!.HorizontalAlignment = HorizontalAlignment.Left;
            this.Find<Border>(nameof(IconBorder))!.IsVisible = true;
        }
    }

    private void GitHubLink_OnClick(object? sender, RoutedEventArgs e)
    {
        var startInfo = new ProcessStartInfo
        {
            Arguments = "https://github.com/arkrow/PyMusicLooper", 
            FileName = "explorer.exe"
        };
                
        Process.Start(startInfo);
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Result = false;
    }
}